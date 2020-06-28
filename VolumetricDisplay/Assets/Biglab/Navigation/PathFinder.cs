using Biglab.Utility;
using Priority_Queue;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Stopwatch = System.Diagnostics.Stopwatch;

namespace Biglab.Navigation
{
    /// <summary>
    /// Implementation of Lazy Theta Star <para/>
    /// Algorithm taken from: http://aigamedev.com/open/tutorial/lazy-theta-star/ <para/>
    /// Implementation coded by Dylan Fafard and made more generic by Chris Chamberlain.
    /// </summary>
    public class PathFinder<T>
    {
        public delegate bool CheckLineOfSight(T a, T b);
        public delegate float CostFunction(T a, T b);

        public delegate IEnumerable<T> GetSucessor(T a);

        // private IPriorityQueue<Vertex> _open;
        private SimplePriorityQueue<Vertex> _open;
        private Dictionary<T, Vertex> _vertices;
        private HashSet<Vertex> _closed;

        private readonly GetSucessor _getSucessor;           // Get neighbors
        private readonly CheckLineOfSight _checkLineOfSight; // Line of sight check function
        private readonly CostFunction _getEstimateCost;      // Heuristic function
        private readonly CostFunction _getActualCost;        // True cost function

        public event Action<IEnumerable<T>> ComputedPath;

        private static int _idCounter = 0;
        private int _id = _idCounter++;

        private static int _concurrentSearches = 0;

        public PathFinder(
            GetSucessor getSucessor,
            CheckLineOfSight lineOfSight,
            CostFunction getActualCost,
            CostFunction getEstimateCost)
        {
            _getSucessor = getSucessor;
            _checkLineOfSight = lineOfSight;
            _getActualCost = getActualCost;
            _getEstimateCost = getEstimateCost;
        }

        private Vertex GetVertex(T node)
        {
            if (_vertices.ContainsKey(node) == false)
            {
                _vertices[node] = new Vertex(node, null, 0);
            }

            return _vertices[node];
        }

        public Coroutine FindPath(T start, T goal)
        {
            return Scheduler.StartCoroutine(FindPathRoutine(start, goal));
        }

        private IEnumerator FindPathRoutine(T start, T goal)
        {
            var sw = Stopwatch.StartNew();

            _vertices = new Dictionary<T, Vertex>();
            _open = new SimplePriorityQueue<Vertex>();
            _closed = new HashSet<Vertex>();

            // TODO: Can break problem across frames to remove "spikes" on the longer more complicated paths.

            _concurrentSearches++;

            // Self looping start ( for ComputePath termination )
            var startVertex = GetVertex(start);
            startVertex.Parent = startVertex;

            // 
            _open.Enqueue(startVertex, startVertex.Cost + _getEstimateCost(start, start));

            var foundPath = false;

            while (_open.Count > 0)
            {
                var vCurrent = _open.Dequeue();

                SetVertex(vCurrent);

                if (vCurrent.Item.Equals(goal)) // Found a path. ^_^
                {
                    foundPath = true;

                    var sVertex = GetVertex(start);
                    var gVertex = GetVertex(goal);
                    ComputePath(sVertex, gVertex);

                    break;
                }

                _closed.Add(vCurrent);

                foreach (T nNeighbor in _getSucessor(vCurrent.Item))
                {
                    var vNeighbor = GetVertex(nNeighbor);

                    if (!_closed.Contains(vNeighbor))
                    {
                        if (!_open.Contains(vNeighbor))
                        {
                            vNeighbor.Cost = float.MaxValue;
                            vNeighbor.Parent = default(Vertex);
                        }

                        UpdateVertex(vCurrent, vNeighbor);
                    }
                }

                var frameTime = Time.smoothDeltaTime * 1000F;

                // Have we took too long?
                var timeSlice = Mathf.Max(1F, (8F / _concurrentSearches) - frameTime);
                if (sw.ElapsedMilliseconds > timeSlice)
                {
                    // Debug.Log($"<b>Time Slice</b>: {sw.ElapsedMilliseconds} exceeds {timeSlice} ({frameTime})");

                    // Go back to doing other unity work.
                    yield return null;
                    sw.Restart();
                }
            }

            // If we didn't find a path
            if (!foundPath)
            {
                // Invoke with null, to signal no path
                ComputedPath?.Invoke(null);
            }

            // Search complete
            _concurrentSearches--;
        }

        private void UpdateVertex(Vertex vStart, Vertex vTarget)
        {
            var oldCost = vTarget.Cost;

            ComputeCost(vStart, vTarget);

            if (vTarget.Cost < oldCost)
            {
                var newCost = vTarget.Cost + _getEstimateCost(vStart.Item, vTarget.Item);

                if (_open.Contains(vTarget))
                {
                    _open.UpdatePriority(vTarget, newCost);
                }
                else
                {
                    _open.Enqueue(vTarget, newCost);
                }
            }
        }

        private void ComputeCost(Vertex vStart, Vertex vTarget)
        {
            /* Path 2 */
            var cost = vStart.Parent.Cost + _getActualCost(vStart.Parent.Item, vTarget.Item);
            if (cost < vTarget.Cost)
            {
                vTarget.Parent = vStart.Parent;
                vTarget.Cost = cost;
            }
        }

        private void SetVertex(Vertex vertex)
        {
            if (!_checkLineOfSight(vertex.Parent.Item, vertex.Item))
            {
                /* Path 1 */
                var minimumParent = default(Vertex);
                var minimumCost = float.MaxValue;

                foreach (T n in _getSucessor(vertex.Item))
                {
                    var t = GetVertex(n);

                    if (_closed.Contains(t))
                    {
                        var cost = t.Cost + _getActualCost(t.Item, vertex.Item);
                        if (cost < minimumCost)
                        {
                            minimumCost = cost;
                            minimumParent = t;
                        }
                    }
                }

                vertex.Parent = minimumParent;
                vertex.Cost = minimumCost;
            }
        }

        private void ComputePath(Vertex vStart, Vertex vGoal)
        {
            var path = new List<Vertex> { vGoal };

            // Shortcut, already at goal
            if (vStart.Equals(vGoal) == false)
            {
                var cur = vGoal;

                // Walk up parent tree
                while (cur.Equals(vStart) == false)
                {
                    cur = cur.Parent;
                    path.Add(cur);
                }

                // Reverse ( goal -> start ) becomes ( start -> goal )
                path.Reverse();
            }

            // 
            ComputedPath?.Invoke(path.Select(v => v.Item));
        }

        private class Vertex : IEquatable<Vertex>
        {
            public T Item;
            public Vertex Parent;
            public float Cost;

            public Vertex(T item, Vertex parent, float cost)
            {
                Item = item;
                Parent = parent;
                Cost = cost;
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as Vertex);
            }

            public bool Equals(Vertex other)
            {
                return other != null &&
                       EqualityComparer<T>.Default.Equals(Item, other.Item);
            }

            public override int GetHashCode()
            {
                return -979861770 + EqualityComparer<T>.Default.GetHashCode(Item);
            }

            public static bool operator ==(Vertex vertex1, Vertex vertex2) => EqualityComparer<Vertex>.Default.Equals(vertex1, vertex2);
            public static bool operator !=(Vertex vertex1, Vertex vertex2) => !(vertex1 == vertex2);
        }
    }
}