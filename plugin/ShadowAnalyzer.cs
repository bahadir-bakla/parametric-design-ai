using System;
using System.Collections.Generic;
using Rhino.Geometry;

namespace RoofAI
{
    public class ShadowResult
    {
        public List<Brep> ShadowBreps { get; set; } = new List<Brep>();
        public double ShadowArea { get; set; }
        public double ShadedPercentage { get; set; }
        public SunPosition SunPosition { get; set; }
    }

    public static class ShadowAnalyzer
    {
        public static ShadowResult CalculateShadow(List<Brep> roofBreps, SunPosition sunPosition,
            Plane groundPlane = default)
        {
            var result = new ShadowResult { SunPosition = sunPosition };

            if (!sunPosition.IsAboveHorizon)
                return result;

            if (groundPlane == default)
                groundPlane = Plane.WorldXY;

            Vector3d sunDir = sunPosition.SunVector;

            foreach (var brep in roofBreps)
            {
                var shadowBrep = ProjectBrepToGround(brep, sunDir, groundPlane);
                if (shadowBrep != null)
                    result.ShadowBreps.Add(shadowBrep);
            }

            double shadowArea = 0;
            foreach (var sb in result.ShadowBreps)
            {
                var area = AreaMassProperties.Compute(sb);
                if (area != null)
                    shadowArea += area.Area;
            }
            result.ShadowArea = shadowArea;

            double roofFootprint = CalculateFootprint(roofBreps, groundPlane);
            if (roofFootprint > 0)
                result.ShadedPercentage = (shadowArea / roofFootprint) * 100.0;

            return result;
        }

        public static List<ShadowResult> CalculateDailyShadows(List<Brep> roofBreps,
            double latitude, double longitude, int year, int month, int day, int timezone,
            double startHour = 8, double endHour = 18, double step = 1.0)
        {
            var results = new List<ShadowResult>();

            for (double hour = startHour; hour <= endHour; hour += step)
            {
                var sunPos = SunPathCalculator.CalculateSunPosition(
                    latitude, longitude, year, month, day, hour, timezone);

                if (sunPos.IsAboveHorizon)
                {
                    var shadow = CalculateShadow(roofBreps, sunPos);
                    results.Add(shadow);
                }
            }

            return results;
        }

        private static Brep ProjectBrepToGround(Brep brep, Vector3d sunDirection, Plane groundPlane)
        {
            try
            {
                var mesh = Mesh.CreateFromBrep(brep, MeshingParameters.Default);
                if (mesh == null || mesh.Length == 0) return null;

                var projectedPoints = new List<Point3d>();

                foreach (var m in mesh)
                {
                    foreach (var vertex in m.Vertices)
                    {
                        Point3d pt = new Point3d(vertex.X, vertex.Y, vertex.Z);
                        Point3d projected = ProjectPointToPlane(pt, sunDirection, groundPlane);
                        projectedPoints.Add(projected);
                    }
                }

                if (projectedPoints.Count < 3) return null;

                var hull = CreateConvexHull2D(projectedPoints, groundPlane);
                if (hull == null || hull.Count < 3) return null;

                var polyline = new Polyline(hull);
                polyline.Add(hull[0]);
                var curve = polyline.ToNurbsCurve();

                var breps = Brep.CreatePlanarBreps(curve, 0.01);
                if (breps != null && breps.Length > 0)
                    return breps[0];
            }
            catch { }

            return null;
        }

        private static Point3d ProjectPointToPlane(Point3d point, Vector3d direction, Plane plane)
        {
            double denom = Vector3d.Multiply(direction, plane.ZAxis);
            if (Math.Abs(denom) < 1e-10)
                return plane.ClosestPoint(point);

            double t = Vector3d.Multiply(plane.Origin - point, plane.ZAxis) / denom;
            return point + t * direction;
        }

        private static List<Point3d> CreateConvexHull2D(List<Point3d> points, Plane plane)
        {
            if (points.Count < 3) return points;

            var points2D = new List<double[]>();
            foreach (var pt in points)
            {
                double u, v;
                plane.ClosestParameter(pt, out u, out v);
                points2D.Add(new[] { u, v });
            }

            points2D.Sort((a, b) => a[0] != b[0] ? a[0].CompareTo(b[0]) : a[1].CompareTo(b[1]));

            int n = points2D.Count;
            var hull = new List<int>();

            for (int i = 0; i < n; i++)
            {
                while (hull.Count >= 2 && Cross(points2D[hull[hull.Count - 2]], points2D[hull[hull.Count - 1]], points2D[i]) <= 0)
                    hull.RemoveAt(hull.Count - 1);
                hull.Add(i);
            }

            int lowerSize = hull.Count + 1;
            for (int i = n - 2; i >= 0; i--)
            {
                while (hull.Count >= lowerSize && Cross(points2D[hull[hull.Count - 2]], points2D[hull[hull.Count - 1]], points2D[i]) <= 0)
                    hull.RemoveAt(hull.Count - 1);
                hull.Add(i);
            }

            hull.RemoveAt(hull.Count - 1);

            var result = new List<Point3d>();
            foreach (int idx in hull)
            {
                result.Add(plane.PointAt(points2D[idx][0], points2D[idx][1]));
            }

            return result;
        }

        private static double Cross(double[] o, double[] a, double[] b)
        {
            return (a[0] - o[0]) * (b[1] - o[1]) - (a[1] - o[1]) * (b[0] - o[0]);
        }

        private static double CalculateFootprint(List<Brep> breps, Plane groundPlane)
        {
            var footprintPoints = new List<Point3d>();

            foreach (var brep in breps)
            {
                var bb = brep.GetBoundingBox(false);
                var corners = bb.GetCorners();
                foreach (var c in corners)
                {
                    footprintPoints.Add(groundPlane.ClosestPoint(c));
                }
            }

            if (footprintPoints.Count < 3) return 0;

            var hull = CreateConvexHull2D(footprintPoints, groundPlane);
            if (hull == null || hull.Count < 3) return 0;

            var polyline = new Polyline(hull);
            polyline.Add(hull[0]);
            var curve = polyline.ToNurbsCurve();
            var footBreps = Brep.CreatePlanarBreps(curve, 0.01);

            if (footBreps != null && footBreps.Length > 0)
            {
                var area = AreaMassProperties.Compute(footBreps[0]);
                if (area != null) return area.Area;
            }

            return 0;
        }
    }
}
