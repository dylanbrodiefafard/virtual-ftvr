using System;
using System.Collections.Generic;
using System.Linq;
//using MathNet.Numerics.LinearAlgebra.Single;
using UnityEngine;

namespace Biglab.Math
{
    public static class MathB
    {
        #region Spherical Cooridinates

        /// <summary>
        /// Spherical coordinates to cartesian coordinates.
        /// </summary>
        /// <param name="spherical">The spherical coordinates as (radius, azimuthal, polar).</param>
        /// <returns>Cartesian coordinates where Y is up.</returns>
        public static Vector3 SphericalToCartesian(Vector3 spherical)
            => SphericalToCartesian(spherical.x, spherical.y, spherical.z);

        /// <summary>
        /// Spherical coordinates to cartesian coordinates.
        /// </summary>
        /// <param name="radius">radius of the coordinate</param>
        /// <param name="theta">azimuthal coordinate (around the vertical axis).</param>
        /// <param name="phi">polar coordinate (measure from the vertical axis down).</param>
        /// <returns>Cartesian coordinates where Y is up.</returns>
        public static Vector3 SphericalToCartesian(float radius, float theta, float phi)
            => new Vector3
            {
                x = Mathf.Cos(theta) * Mathf.Sin(phi),
                y = Mathf.Cos(phi),
                z = Mathf.Sin(theta) * Mathf.Sin(phi)
            } * radius;

        /// <summary>
        /// Cartesian coordinates to spherical coordinates.
        /// </summary>
        /// <param name="cartesian">The cartesian coordinates as (x, y, z).</param>
        /// <returns>The spherical coordinates as (radius, azimuthal, polar).</returns>
        public static Vector3 CartesianToSpherical(Vector3 cartesian)
        {
            var radius = cartesian.magnitude;
            var theta = Mathf.Atan2(cartesian.z, cartesian.x);
            var phi = Mathf.Acos(cartesian.y / radius);

            return new Vector3
            {
                x = radius,
                y = theta,
                z = phi
            };
        }

        #endregion

        #region Plane Intersection

        public static bool DoesLineIntersectPlane(Vector3 l, Vector3 n)
            => Vector3.Dot(l, n) <= float.Epsilon;

        // See: https://en.wikipedia.org/wiki/Line%E2%80%93plane_intersection
        public static Vector3 IntersectionOfLinePlane(Vector3 l, Vector3 l0, Vector3 n, Vector3 p0)
        {
            if (!DoesLineIntersectPlane(l, n))
            {
                throw new InvalidOperationException();
            }

            var d = Vector3.Dot(p0 - l0, n) / Vector3.Dot(l, n);
            return d * l + l0;
        }

        #endregion

        #region Circle Intersection

        /// <summary>
        /// Rotates a point about a fixed point at some angle 'a'
        /// </summary>
        /// <param name="fp">Fixed point</param>
        /// <param name="pt">Point to rotate</param>
        /// <param name="a">The angle amount</param>
        /// <returns>The rotated point</returns>
        public static Vector2 RotatePoint(Vector2 fp, Vector2 pt, float a)
        {
            var x = pt.x - fp.x;
            var y = pt.y - fp.y;
            var xRot = x * Mathf.Cos(a) + y * Mathf.Sin(a);
            var yRot = y * Mathf.Cos(a) - x * Mathf.Sin(a);
            return new Vector2(fp.x + xRot, fp.y + yRot);
        }

        // Given two circles this method finds the intersection
        // point(s) of the two circles (if any exists)
        public static int CircleCircleIntersectionPoints(Vector2 c1Origin, float c1Radius, Vector2 c2Origin, float c2Radius, out Vector2 p1, out Vector2 p2)
        {
            float radius1, radius2;
            Vector2 c1, c2;

            if (c1Radius < c2Radius)
            {
                radius1 = c1Radius;
                radius2 = c2Radius;
                c1 = c1Origin;
                c2 = c2Origin;
            }
            else
            {
                radius1 = c2Radius;
                radius2 = c1Radius;
                c2 = c1Origin;
                c1 = c2Origin;
            }

            // Find the distance between two points.
            var distance = Vector2.Distance(c1, c2);

            // There are an infinite number of solutions
            // Seems appropriate to return one of them
            // OR
            // No intersection (circles centered at the 
            // same place with different size)
            // OR 
            // No intersection. Either the small circle contained within 
            // big circle or circles are simply disjoint.
            if (distance < float.Epsilon || distance + radius1 < radius2 || radius2 + radius1 < distance)
            {
                if (Mathf.Abs(radius2 - radius1) < float.Epsilon)
                {
                    p1 = new Vector2(c1.x + radius1, c1.y);
                    p2 = new Vector2(c1.x - radius1, c1.y);

                    return int.MaxValue;
                }

                p1 = Vector2.positiveInfinity;
                p2 = Vector2.positiveInfinity;

                return 0;
            }

            // Compute the vector <dx, dy>
            var dx = c1.x - c2.x;
            var dy = c1.y - c2.y;
            var x = (dx / distance) * radius2 + c2.x;
            var y = (dy / distance) * radius2 + c2.y;
            var p = new Vector2(x, y);

            // Single intersection (kissing circles)
            if (Mathf.Abs(radius2 + radius1 - distance) < float.Epsilon || Mathf.Abs(radius2 - (radius1 + distance)) < float.Epsilon)
            {
                p1 = p;
                p2 = Vector2.positiveInfinity;

                return 1;
            }

            var angle = Mathf.Acos((radius1 * radius1 - distance * distance - radius2 * radius2) / (-2.0f * distance * radius2));
            p1 = RotatePoint(c2, p, +angle);
            p2 = RotatePoint(c2, p, -angle);

            return 2;
        }

        #endregion

        /// <summary>
        /// Computes the geodesic distance between two rotations according to the o3 metric from DOI 10.1007/s10851-009-0161-2
        /// </summary>
        /// <param name="q1">The first rotation.</param>
        /// <param name="q2">The second rotation.</param>
        /// <returns>Geodesic distnace between rotations in radians.</returns>
        public static float GeodesicDistanceBetweenRotations(Quaternion q1, Quaternion q2)
        {
            // Added Mathf.Clamp. For some reason Mathf.Acos was return NaN when y was 1.00000000000000000 etc... 
            var y = Mathf.Clamp(Mathf.Abs(Quaternion.Dot(q1, q2)), 0, 1);
            var x = Mathf.Acos(y) % (Mathf.PI / 2.0f);
            return 2.0f * x;
        }

        public static Vector3 GetMean(IEnumerable<Vector3> vectors)
        {
            var enumerable = vectors as Vector3[] ?? vectors.ToArray();
            return new Vector3
            {
                x = enumerable.Select(elem => elem.x).Average(),
                y = enumerable.Select(elem => elem.y).Average(),
                z = enumerable.Select(elem => elem.z).Average(),
            };
        }

        /// <summary>
        /// Computes the mean weighted rotation. The arguments must be the same length.
        /// </summary>
        /// <param name="rotations">The rotations to average.</param>
        /// <param name="weights">The weights for each element in rotations.</param>
        /// <returns>The mean weighted rotation.</returns>
        /// /// <exception cref="ArgumentNullException">One of the arguments is null.</exception>
        public static Quaternion ComputeMeanWeightedRotation(List<Quaternion> rotations,
            List<float> weights = null)
        {
            if (rotations == null)
            {
                throw new ArgumentNullException(nameof(rotations));
            }

            if (weights == null)
            {
                weights = new List<float>(rotations.Count);
                foreach (var _ in rotations)
                {
                    weights.Add(1.0f / rotations.Count);
                }
            }

            // Handle the special cases
            if (rotations.Count == 1)
            {
                return rotations[0];
            }

            if (rotations.Count == 2)
            {
                return Quaternion.Slerp(rotations[0], rotations[1], weights[1]);
            }

            var wSum = 0F;
            MathNet.Numerics.LinearAlgebra.Matrix<float> m = new MathNet.Numerics.LinearAlgebra.Single.DenseMatrix(4, 4);
            // Add all of the outerproducts together
            using (var weightEnumerator = weights.GetEnumerator())
            using (var rotationEnumerator = rotations.GetEnumerator())
            {
                while (weightEnumerator.MoveNext() && rotationEnumerator.MoveNext())
                {
                    var quaternion = rotationEnumerator.Current;
                    var vector = new MathNet.Numerics.LinearAlgebra.Single.DenseVector(new[] { quaternion.y, quaternion.z, quaternion.w, quaternion.x });
                    var outerProduct = vector.OuterProduct(vector);
                    var weight = weightEnumerator.Current;
                    m = weight * outerProduct + m;
                    wSum += weight;
                }
            }

            // Scale
            m = 1F / wSum * m;

            // Perform an eigenvalue decomposition
            var evd = m.Evd();

            //Find the maximum eigenvector which is our average rotation
            var maxEigenvector = evd.EigenVectors.Column(3);

            return new Quaternion(maxEigenvector[3], maxEigenvector[0], maxEigenvector[1], maxEigenvector[2]);
        }

        /// <summary>
        /// Gets an arbitrary orthogonal direction to a given direction.
        /// </summary>
        /// <param name="direction">a unit vector</param>
        /// <returns>a unit vector that is orthogonal to the given direction.</returns>
        public static Vector3 GetArbitraryOrthogonalDirection(Vector3 direction)
        {
            var xMag = Mathf.Abs(direction.x);
            var yMag = Mathf.Abs(direction.y);
            var zMag = Mathf.Abs(direction.z);

            if (xMag < yMag && xMag < zMag)
            {
                // x is smallest, use right
                return Vector3.Cross(direction, Vector3.right);
            }

            if (yMag < zMag && yMag < xMag)
            {
                // y is smallest, use up
                return Vector3.Cross(direction, Vector3.up);
            }

            // z is smallest, use forward
            return Vector3.Cross(direction, Vector3.forward);
        }

        // Source: http://rajputyh.blogspot.com/2014/09/the-ultimate-rounding-function.html
        /// <summary>
        /// Rounds a given value to the nearest given rouding unity with fairness.
        /// </summary>
        /// <param name="amountToRound">input amount</param>
        /// <param name="nearestOf">.25 if round to quater, 0.01 for rounding to 1 cent, 1 for rounding to $1</param>
        /// <param name="fairness">btween 0 to 0.9999999___.
        ///            0 means floor and 0.99999... means ceiling. But for ceiling, I would recommend, Math.Ceiling
        ///            0.5 = Standard Rounding function. It will round up the border case. i.e. 1.5 to 2 and not 1.
        ///            0.4999999... non-standard rounding function. Where border case is rounded down. i.e. 1.5 to 1 and not 2.
        ///            0.75 means first 75% values will be rounded down, rest 25% value will be rounded up.</param>
        /// <returns>The amount rounded to the nearest nearest of with given fairness.</returns>
        public static float RoundToNearest(float amountToRound, float nearestOf, float fairness = 0.5f)
            => Mathf.Floor(amountToRound / nearestOf + fairness) * nearestOf;

        /// <summary>
        /// Computes the factorial of i
        /// </summary>
        /// <param name="i">The number to compute the factorial for</param>
        /// <returns>i!</returns>
        public static int Factorial(int i)
            => i <= 1 ? 1 : i * Factorial(i - 1);
    }
}