﻿namespace Billapong.GameConsole.Game
{
    using System;
    using System.Windows;

    /// <summary>
    /// Provides some helper methods for calculations
    /// </summary>
    public static class CalculationHelpers
    {
        /// <summary>
        /// Calculates the distance between the current and the target point in pixel
        /// </summary>
        /// <param name="currentPoint">The current point.</param>
        /// <param name="targetPoint">The target point.</param>
        /// <returns>The calculated distance</returns>
        public static double DistanceTo(this Point currentPoint, Point targetPoint)
        {
            var a = targetPoint.X - currentPoint.X;
            var b = targetPoint.Y - currentPoint.Y;

            return Math.Sqrt(a * a + b * b);
        }

        /// <summary>
        /// Checks two lines for intersection
        /// </summary>
        /// <param name="firstLineStart">The first line start.</param>
        /// <param name="firstLineEnd">The first line end.</param>
        /// <param name="secondLineStart">The second line start.</param>
        /// <param name="secondLineEnd">The second line end.</param>
        /// <returns>The intersection check result</returns>
        public static Vector? GetLineIntersection(Vector firstLineStart, Vector firstLineEnd, Vector secondLineStart, Vector secondLineEnd)
        {
            var b = firstLineEnd - firstLineStart;
            var d = secondLineEnd - secondLineStart;
            var delta = (b.X * d.Y) - (b.Y * d.X);

            // check for parallel lines (inifite intersection point)
            if (Math.Abs(delta) < 0) return null;

            var c = secondLineStart - firstLineStart;
            var t = ((c.X * d.Y) - (c.Y * d.X)) / delta;
            if (t < 0 || t > 1)
            {
                return null;
            }

            var u = ((c.X * b.Y) - (c.Y * b.X)) / delta;
            if (u < 0 || u > 1)
            {
                return null;
            }

            return firstLineStart + (t * b);
        }

        /// <summary>
        /// Calculates the intersection between a line and a sphere.
        /// </summary>
        /// <param name="sphereCenter">The center point of the sphere</param>
        /// <param name="radius">The radius of the sphere.</param>
        /// <param name="lineStart">The line start point.</param>
        /// <param name="lineEnd">The line end point.</param>
        /// <param name="firstIntersection">The first intersection.</param>
        /// <param name="secondIntersection">The second intersection.</param>
        /// <returns>The intersection. 0 = no intersection, 1 = one intersection, 3 = two intersections</returns>
        public static int CalculateLineSphereIntersection(Point sphereCenter, double radius, Point lineStart, Point lineEnd, out Point firstIntersection, out Point secondIntersection)
        {
            double t;
            var dx = lineEnd.X - lineStart.X;
            var dy = lineEnd.Y - lineStart.Y;

            var a = dx * dx + dy * dy;
            var b = 2 * (dx * (lineStart.X - sphereCenter.X) + dy * (lineStart.Y - sphereCenter.Y));
            var c = (lineStart.X - sphereCenter.X) * (lineStart.X - sphereCenter.X) + (lineStart.Y - sphereCenter.Y) * (lineStart.Y - sphereCenter.Y) - radius * radius;

            var det = b * b - 4 * a * c;
            if ((Math.Abs(a) < 0.001) || (det < 0))
            {
                // No intersection
                firstIntersection = default(Point);
                secondIntersection = default(Point);
                return 0;
            }

            if (Math.Abs(det) < 0.001)
            {
                // One intersection
                t = -b / (2 * a);
                firstIntersection = new Point(lineStart.X + t * dx, lineStart.Y + t * dy);
                secondIntersection = default(Point);
                return 1;
            }

            // Two intersections
            t = ((-b + Math.Sqrt(det)) / (2 * a));
            firstIntersection = new Point(lineStart.X + t * dx, lineStart.Y + t * dy);
            t = (float)((-b - Math.Sqrt(det)) / (2 * a));
            secondIntersection = new Point(lineStart.X + t * dx, lineStart.Y + t * dy);
            return 2;
        }
    }
}
