using System;
using System.Collections.Generic;

using Biglab.Extensions;
using Biglab.Math;
using Biglab.Utility;

using UnityEngine;

namespace Biglab.Navigation
{
    [PreferBinarySerialization]
    [CreateAssetMenu(menuName = "Navigation/Volume")]
    public class NavVolume : ScriptableObject, ISerializationCallbackReceiver
    {
        public Bounds Bounds;

        public VisibilityOptions VisibilityParameters;

        public float CellSize = 0.33F;

        public List<Vector3> VisibilityPositions;

        public Dictionary<Coordinate, Node> Graph;

        [SerializeField]
        private NodePairs[] _serializedGraph;

        public int GridWidth;

        public int GridHeight;

        public int GridDepth;

        /// <summary>
        /// Gets all coordinates in the volume.
        /// </summary>
        public IEnumerable<Coordinate> Coordinates
        {
            get
            {
                foreach (var kv in Graph)
                {
                    yield return kv.Key;
                }
            }
        }

        /// <summary>
        /// Gets all positions within the volume.
        /// </summary>
        public IEnumerable<Vector3> Positions
        {
            get
            {
                foreach (var co in Coordinates)
                {
                    yield return GetPosition(co);
                }
            }
        }

        /// <summary>
        /// Gets the world position of the given coordinate.
        /// </summary>
        public Vector3 GetPosition(Coordinate coordinate)
        {
            var xf = coordinate.X.Rescale(0, GridWidth, Bounds.min.x, Bounds.max.x);
            var yf = coordinate.Y.Rescale(0, GridHeight, Bounds.min.y, Bounds.max.y);
            var zf = coordinate.Z.Rescale(0, GridDepth, Bounds.min.z, Bounds.max.z);

            return new Vector3(xf, yf, zf);
        }

        /// <summary>
        /// Gets the nearest coordinate ( may be invalid ) to the given world position.
        /// </summary>
        public Coordinate GetCoordinate(Vector3 position)
        {
            var xi = Mathf.RoundToInt(position.x.Rescale(Bounds.min.x, Bounds.max.x, 0, GridWidth));
            var yi = Mathf.RoundToInt(position.y.Rescale(Bounds.min.y, Bounds.max.y, 0, GridHeight));
            var zi = Mathf.RoundToInt(position.z.Rescale(Bounds.min.z, Bounds.max.z, 0, GridDepth));

            return new Coordinate(xi, yi, zi);
        }

        /// <summary>
        /// Gets the nearst valid coordinate to the given world position.
        /// </summary>
        public Coordinate GetNearestCoordinate(Vector3 position)
        {
            var co = GetCoordinate(position);

            // Simple coordinate, if valid, hurray
            if (Graph.ContainsKey(co))
            {
                return co;
            }
            else
            {
                // Try immediate neighbor field
                foreach (var nCo in GetNeighborField(co))
                {
                    if (Graph.ContainsKey(nCo))
                    { return nCo; }
                }

                // If reaching here, none of the neighbors are good, now we need to try iterating the entire volume 
                // The best approach I can think of is iterating around a spiral outward from the current position,
                // this way its hopefully less than iterating the entire volume but its definitely more complicated.
                // Overall that is tedious... and this works in the edge cases so far,
                // so return the fast estimated coordinate, but its a garbage value here.
                return co;
            }
        }

        /// <summary>
        /// Gets a subset of coordinates within the volume.
        /// </summary>
        public IEnumerable<Coordinate> GetCoordinates(Func<Node, bool> filter)
        {
            foreach (var kv in Graph)
            {
                if (filter(kv.Value))
                {
                    yield return kv.Key;
                }
            }
        }

        /// <summary>
        /// Gets a subset of positions within the volume.
        /// </summary>
        public IEnumerable<Vector3> GetPositions(Func<Node, bool> filter)
        {
            foreach (var co in GetCoordinates(filter))
            {
                yield return GetPosition(co);
            }
        }

        /// <summary>
        /// Finds a path from start to goal, returning zero elements if a path could not be found.
        /// </summary>
        public void FindPath(Vector3 start, Vector3 goal, Action<IEnumerable<Coordinate>> onComputedPath)
        {
            var cStart = GetNearestCoordinate(start);
            var cGoal = GetNearestCoordinate(goal);

            // Does the graph contain these coordinates?
            if (Graph.ContainsKey(cStart) && Graph.ContainsKey(cGoal))
            {
                // Create path finding instance
                var finder = new PathFinder<Coordinate>(GetNeighbors, CheckLineOfSight, GetActualCost, GetHeuristicCost);

                // When the path is computed
                Coroutine coroutine = null;
                finder.ComputedPath += path =>
                {
                    // If the coroutine was set, stop it.
                    if (coroutine != null)
                    { Scheduler.StopCoroutine(coroutine); }

                    onComputedPath(path);
                };

                // Find path
                coroutine = finder.FindPath(cStart, cGoal);
            }
            else
            {
                var hasStart = Graph.ContainsKey(cStart);
                var hasGoal = Graph.ContainsKey(cGoal);

                if (!hasStart && !hasGoal)
                {
                    Debug.LogWarning($"No path possible from {start}/{cStart} to {goal}/{cGoal}. Either or one of the end-points is out of bounds.");
                }
                else if (!hasStart)
                {
                    Debug.LogWarning($"No path possible from {start}/{cStart} to {goal}/{cGoal}. Starting coordinate {cGoal} is out of bounds.");
                }
                else
                {
                    Debug.LogWarning($"No path possible from {start}/{cStart} to {goal}/{cGoal}. Goal coordinate {cGoal} is out of bounds.");
                }
            }
        }

        /// <summary>
        /// Creates an instance of a navigation volume with the given parameters.
        /// </summary>
        public static NavVolume CreateNavigationVolume(Bounds bounds, float cellSize, VisibilityOptions visibilityOptions)
        {
            var vol = CreateInstance<NavVolume>();

            vol.Bounds = bounds;
            vol.CellSize = cellSize;

            vol.GridWidth = Mathf.FloorToInt(bounds.size.x * (1F / cellSize));
            vol.GridHeight = Mathf.FloorToInt(bounds.size.y * (1F / cellSize));
            vol.GridDepth = Mathf.FloorToInt(bounds.size.z * (1F / cellSize));

            vol.Generate(visibilityOptions);

            return vol;
        }

        private void Generate(VisibilityOptions visibilityOptions)
        {
            // Compute visibility positions
            VisibilityPositions = GenerateVisiblityPositions(visibilityOptions);
            VisibilityParameters = visibilityOptions;

            // Create storage data structure
            Graph = new Dictionary<Coordinate, Node>();

            // 
            var maxVisibility = float.MinValue;
            var minVisibility = float.MaxValue;
            var avgVisibility = 0F;
            var avgCount = 0;

            var totalCells = GridWidth * GridHeight * GridDepth;

            // Iterate over volume
            for (var z = 0; z < GridDepth; z++)
            {
                for (var y = 0; y < GridHeight; y++)
                {
                    for (var x = 0; x < GridWidth; x++)
                    {
                        // Gets the world position of the given cell
                        var coordinate = new Coordinate(x, y, z);
                        var center = GetPosition(coordinate);

                        // Display voxelization
                        var cellIndex = x + (y * GridWidth) + (z * GridWidth * GridHeight);
                        Bizmos.DisplayEditorProgress("Generating", $"Voxelizing Volume ({cellIndex}/{totalCells})", cellIndex / (float)totalCells);

                        // Does a box overlap the world somewhere?
                        if (!Physics.CheckBox(center, Vector3.one * CellSize * 0.75F))
                        {
                            var score = 0F;

                            // Check visibility of this with the sky-bound visibility positions
                            foreach (var visibilityPosition in VisibilityPositions)
                            {
                                RaycastHit hit;
                                if (!Physics.Linecast(visibilityPosition, center, out hit))
                                {
                                    score++;
                                }
                            }

                            // If can see
                            if (score > 0)
                            {
                                // Tracking min and max visibility numbers
                                maxVisibility = Mathf.Max(maxVisibility, score);
                                minVisibility = Mathf.Min(minVisibility, score);
                                avgVisibility += score;
                                avgCount++;

                                // Store the node here
                                Graph[coordinate] = new Node(coordinate, center, score);
                            }
                        }
                    }
                }
            }

            // Display visibility stats
            avgVisibility /= avgCount;

            // Map neighbors
            var i = 0;
            foreach (var node in Graph.Values)
            {
                // Remap visibility to a normalized range
                // node.Visibility = node.Visibility.Between(0, VisibilityPositions.Count);
                node.Visibility = node.Visibility.Between(minVisibility, maxVisibility);

                // Display progress
                Bizmos.DisplayEditorProgress("Generating", $"Normalizing Visibility Score ({i++}/{Graph.Count})", i / (float)Graph.Count);
            }
        }

        private IEnumerable<Coordinate> GetNeighborField(Coordinate co)
        {
            // Check neighbors
            for (var ix = -1; ix <= 1; ix++)
            {
                for (var iy = -1; iy <= 1; iy++)
                {
                    for (var iz = -1; iz <= 1; iz++)
                    {
                        // Skip self
                        if (ix == 0 && iy == 0 && iz == 0)
                        {
                            continue;
                        }

                        yield return new Coordinate(co.X + ix, co.Y + iy, co.Z + iz);
                    }
                }
            }
        }

        private List<Vector3> GenerateVisiblityPositions(VisibilityOptions opt)
        {
            var positions = new List<Vector3>();

            // For each radial shell
            for (var rIdx = 0; rIdx < opt.RadiusIterations; rIdx++)
            {
                // For each azimuth ( ground circle )
                for (var aIdx = 0; aIdx < opt.AzimuthIterations; aIdx++)
                {
                    // For each polar ( sky to horizon )
                    for (var pIdx = 0; pIdx < opt.PolarIterations; pIdx++)
                    {
                        var radius = rIdx.Rescale(0, opt.RadiusIterations, opt.Radius.Min, opt.Radius.Max);
                        var polar = pIdx.Rescale(0, opt.PolarIterations, opt.Polar.Min * Mathf.Deg2Rad, opt.Polar.Max * Mathf.Deg2Rad);
                        var azimuth = aIdx.Rescale(0, opt.AzimuthIterations, opt.Azimuth.Min * Mathf.Deg2Rad, opt.Azimuth.Max * Mathf.Deg2Rad);

                        // Compute position from the center of an arbitrary sphere.
                        var position = MathB.SphericalToCartesian(radius, azimuth, polar);

                        // Scale points by the largest component in the bound size.
                        // This should ensure that a min-size of 1 always is "outside" the volume.
                        positions.Add(position * Bounds.size.MaxElement());
                    }
                }
            }

            return positions;
        }

        private float GetHeuristicCost(Coordinate a, Coordinate b)
        {
            if (Graph.ContainsKey(a) && Graph.ContainsKey(b))
            {
                var n1 = Graph[a];
                var n2 = Graph[b];

                // Manhattan Distance
                var dx = Mathf.Abs(n1.Position.x - n2.Position.x);
                var dy = Mathf.Abs(n1.Position.y - n2.Position.y);
                var dz = Mathf.Abs(n1.Position.z - n2.Position.z);
                return (dx + dy + dz);// * 3; // Assumes worst case visibility
            }
            else
            {
                // One of the coordinates doesn't exist,
                // so it can't possibly be in the path, so its the maximum weight.
                return float.MaxValue;
            }
        }

        private float GetActualCost(Coordinate a, Coordinate b)
        {
            if (Graph.ContainsKey(a) && Graph.ContainsKey(b))
            {
                var n1 = Graph[a];
                var n2 = Graph[b];

                // Euclidean Distance
                return Vector3.Distance(n1.Position, n2.Position);// * (1 + n1.Visibility + n2.Visibility);
            }
            else
            {
                // One of the coordinates doesn't exist,
                // so it can't possibly be in the path, so its the maximum weight.
                return float.MaxValue;
            }
        }

        private IEnumerable<Coordinate> GetNeighbors(Coordinate co)
        {
            if (Graph.ContainsKey(co))
            {
                foreach (var n in GetNeighborField(co))
                {
                    if (Graph.ContainsKey(n))
                    {
                        yield return n;
                    }
                }
            }
        }

        private bool CheckLineOfSight(Coordinate a, Coordinate b)
        {
            if (Graph.ContainsKey(a) && Graph.ContainsKey(b))
            {
                var p1 = Graph[a].Position;
                var p2 = Graph[b].Position;

                return !Physics.Linecast(p1, p2);

                //var dir = (p2 - p1).normalized;
                //var dist = Vector3.Distance(p1, p2);
                //return !Physics.SphereCast(new Ray(p1, dir), 0.5F, dist);
            }
            else
            {
                // One of the coordinates doesn't exist,
                // so it can't possibly be in the line of sight.
                return false;
            }
        }

        #region Nested Data Types

        [Serializable]
        public class Node
        {
            public Coordinate Coordinate;

            public Vector3 Position;

            public float Visibility;

            public Node(Coordinate coordinate, Vector3 position, float visibility)
            {
                Coordinate = coordinate;
                Position = position;
                Visibility = visibility;
            }
        }

        [Serializable]
        public class VisibilityOptions
        {
            public int RadiusIterations = 3;
            public MinMaxRange Radius = new MinMaxRange(1, Mathf.Sqrt(2));

            public int PolarIterations = 6;
            public MinMaxRange Polar = new MinMaxRange(20, 120);

            public int AzimuthIterations = 12;
            public MinMaxRange Azimuth = new MinMaxRange(0, 360);
        }

        [Serializable]
        private class NodePairs
        {
            public Coordinate Coordinate;
            public Node Node;
        }

        #endregion

        public void DrawGizmos(Func<Node, bool> visibilityThreshold, bool showVisibilitySources = false, bool showNeighbors = true)
        {
            Gizmos.DrawWireCube(Bounds.center, Bounds.size);

            if (Graph != null)
            {
                var cellColor = Color.cyan;
                cellColor.a = 0.2F;

                var neighborColor = Color.black;
                neighborColor.a = 0.1F;

                if (showVisibilitySources)
                {
                    Gizmos.color = Color.yellow;
                    foreach (var pos in VisibilityPositions)
                    {
                        Gizmos.DrawSphere(pos, CellSize * 0.33F);
                    }
                }

                foreach (var node in Graph.Values)
                {
                    if (node != null)
                    {
                        if (visibilityThreshold(node))
                        {
                            // Draw visibility center
                            Gizmos.color = Color.HSVToRGB(node.Visibility * 0.5F, 1F, 1F);
                            Gizmos.DrawCube(node.Position, Vector3.one * CellSize * 0.2F);

                            if (showNeighbors)
                            {
                                // Draw neighbors
                                Gizmos.color = neighborColor;
                                foreach (var n in GetNeighbors(node.Coordinate))
                                {
                                    var neighbor = Graph[n];
                                    if (visibilityThreshold(neighbor))
                                    {
                                        Gizmos.DrawLine(node.Position, neighbor.Position);
                                    }
                                }
                            }
                        }
                    }
                };
            }
        }

        public void OnBeforeSerialize()
        {
            _serializedGraph = new NodePairs[Graph.Count];

            var i = 0;
            foreach (var kv in Graph)
            {
                _serializedGraph[i++] = new NodePairs
                {
                    Coordinate = kv.Key,
                    Node = kv.Value
                };
            }
        }

        public void OnAfterDeserialize()
        {
            Graph = new Dictionary<Coordinate, Node>();

            foreach (var pair in _serializedGraph)
            {
                Graph[pair.Coordinate] = pair.Node;
            }
        }
    }
}