using System;
using System.Collections.Generic;
using Rhino.Geometry;

namespace RoofAI
{
    public static class GeometryEngine
    {
        public static List<Brep> GenerateRoof(string roofType, double length, double width,
            double pitch, double overhang, double orientation)
        {
            List<Brep> surfaces;

            switch (roofType.ToLower())
            {
                case "gable":
                    surfaces = CreateGableRoof(length, width, pitch, overhang);
                    break;
                case "hip":
                    surfaces = CreateHipRoof(length, width, pitch, overhang);
                    break;
                case "gambrel":
                    surfaces = CreateGambrelRoof(length, width, pitch, overhang);
                    break;
                case "shed":
                    surfaces = CreateShedRoof(length, width, pitch, overhang);
                    break;
                case "flat":
                    surfaces = CreateFlatRoof(length, width, overhang);
                    break;
                default:
                    throw new ArgumentException($"Desteklenmeyen cati tipi: {roofType}");
            }

            if (Math.Abs(orientation) > 0.01)
            {
                var transform = Transform.Rotation(
                    orientation * Math.PI / 180.0,
                    Vector3d.ZAxis,
                    Point3d.Origin);

                for (int i = 0; i < surfaces.Count; i++)
                {
                    surfaces[i].Transform(transform);
                }
            }

            return surfaces;
        }

        public static List<Brep> CreateGableRoof(double length, double width, double pitch, double overhang)
        {
            var surfaces = new List<Brep>();

            double totalLength = length + 2 * overhang;
            double totalWidth = width + 2 * overhang;
            double ridgeHeight = (width / 2.0) * Math.Tan(pitch * Math.PI / 180.0);

            double hL = totalLength / 2.0;
            double hW = totalWidth / 2.0;

            Point3d sw = new Point3d(-hL, -hW, 0);
            Point3d se = new Point3d(hL, -hW, 0);
            Point3d ne = new Point3d(hL, hW, 0);
            Point3d nw = new Point3d(-hL, hW, 0);
            Point3d ridgeW = new Point3d(-hL, 0, ridgeHeight);
            Point3d ridgeE = new Point3d(hL, 0, ridgeHeight);

            var southSlope = CreateQuadBrep(sw, se, ridgeE, ridgeW);
            if (southSlope != null) surfaces.Add(southSlope);

            var northSlope = CreateQuadBrep(nw, ridgeW, ridgeE, ne);
            if (northSlope != null) surfaces.Add(northSlope);

            var westGable = Brep.CreateFromCornerPoints(sw, nw, ridgeW, 0.01);
            if (westGable != null) surfaces.Add(westGable);

            var eastGable = Brep.CreateFromCornerPoints(se, ridgeE, ne, 0.01);
            if (eastGable != null) surfaces.Add(eastGable);

            return surfaces;
        }

        public static List<Brep> CreateHipRoof(double length, double width, double pitch, double overhang)
        {
            var surfaces = new List<Brep>();

            double totalLength = length + 2 * overhang;
            double totalWidth = width + 2 * overhang;
            double ridgeHeight = (width / 2.0) * Math.Tan(pitch * Math.PI / 180.0);

            double hL = totalLength / 2.0;
            double hW = totalWidth / 2.0;

            double ridgeInset = hW;
            double ridgeHalfLen = hL - ridgeInset;
            if (ridgeHalfLen < 0) ridgeHalfLen = 0;

            Point3d sw = new Point3d(-hL, -hW, 0);
            Point3d se = new Point3d(hL, -hW, 0);
            Point3d ne = new Point3d(hL, hW, 0);
            Point3d nw = new Point3d(-hL, hW, 0);

            Point3d ridgeW, ridgeE;

            if (ridgeHalfLen > 0.01)
            {
                ridgeW = new Point3d(-ridgeHalfLen, 0, ridgeHeight);
                ridgeE = new Point3d(ridgeHalfLen, 0, ridgeHeight);

                var southSlope = CreateQuadBrep(sw, se, ridgeE, ridgeW);
                if (southSlope != null) surfaces.Add(southSlope);

                var northSlope = CreateQuadBrep(nw, ridgeW, ridgeE, ne);
                if (northSlope != null) surfaces.Add(northSlope);

                var westHip = Brep.CreateFromCornerPoints(sw, nw, ridgeW, 0.01);
                if (westHip != null) surfaces.Add(westHip);

                var eastHip = Brep.CreateFromCornerPoints(se, ridgeE, ne, 0.01);
                if (eastHip != null) surfaces.Add(eastHip);
            }
            else
            {
                Point3d apex = new Point3d(0, 0, ridgeHeight);

                var south = Brep.CreateFromCornerPoints(sw, se, apex, 0.01);
                if (south != null) surfaces.Add(south);

                var east = Brep.CreateFromCornerPoints(se, ne, apex, 0.01);
                if (east != null) surfaces.Add(east);

                var north = Brep.CreateFromCornerPoints(ne, nw, apex, 0.01);
                if (north != null) surfaces.Add(north);

                var west = Brep.CreateFromCornerPoints(nw, sw, apex, 0.01);
                if (west != null) surfaces.Add(west);
            }

            return surfaces;
        }

        public static List<Brep> CreateGambrelRoof(double length, double width, double pitch, double overhang)
        {
            var surfaces = new List<Brep>();

            double totalLength = length + 2 * overhang;
            double totalWidth = width + 2 * overhang;

            double hL = totalLength / 2.0;
            double hW = totalWidth / 2.0;

            double lowerPitch = Math.Min(pitch + 25, 70);
            double upperPitch = Math.Max(pitch - 5, 15);

            double breakPointY = hW * 0.5;
            double lowerHeight = breakPointY * Math.Tan(lowerPitch * Math.PI / 180.0);
            double upperRise = (hW - breakPointY) * Math.Tan(upperPitch * Math.PI / 180.0);
            double totalHeight = lowerHeight + upperRise;

            Point3d sw = new Point3d(-hL, -hW, 0);
            Point3d se = new Point3d(hL, -hW, 0);
            Point3d ne = new Point3d(hL, hW, 0);
            Point3d nw = new Point3d(-hL, hW, 0);

            Point3d sBreakW = new Point3d(-hL, -breakPointY, lowerHeight);
            Point3d sBreakE = new Point3d(hL, -breakPointY, lowerHeight);
            Point3d nBreakW = new Point3d(-hL, breakPointY, lowerHeight);
            Point3d nBreakE = new Point3d(hL, breakPointY, lowerHeight);

            Point3d ridgeW = new Point3d(-hL, 0, totalHeight);
            Point3d ridgeE = new Point3d(hL, 0, totalHeight);

            var southLower = CreateQuadBrep(sw, se, sBreakE, sBreakW);
            if (southLower != null) surfaces.Add(southLower);

            var southUpper = CreateQuadBrep(sBreakW, sBreakE, ridgeE, ridgeW);
            if (southUpper != null) surfaces.Add(southUpper);

            var northLower = CreateQuadBrep(nw, nBreakW, nBreakE, ne);
            if (northLower != null) surfaces.Add(northLower);

            var northUpper = CreateQuadBrep(nBreakW, ridgeW, ridgeE, nBreakE);
            if (northUpper != null) surfaces.Add(northUpper);

            var westPts = new List<Point3d> { sw, nw, nBreakW, ridgeW, sBreakW };
            var westGable = CreatePolygonBrep(westPts);
            if (westGable != null) surfaces.Add(westGable);

            var eastPts = new List<Point3d> { se, sBreakE, ridgeE, nBreakE, ne };
            var eastGable = CreatePolygonBrep(eastPts);
            if (eastGable != null) surfaces.Add(eastGable);

            return surfaces;
        }

        public static List<Brep> CreateShedRoof(double length, double width, double pitch, double overhang)
        {
            var surfaces = new List<Brep>();

            double totalLength = length + 2 * overhang;
            double totalWidth = width + 2 * overhang;
            double roofHeight = width * Math.Tan(pitch * Math.PI / 180.0);

            double hL = totalLength / 2.0;
            double hW = totalWidth / 2.0;

            Point3d swLow = new Point3d(-hL, -hW, 0);
            Point3d seLow = new Point3d(hL, -hW, 0);
            Point3d neHigh = new Point3d(hL, hW, roofHeight);
            Point3d nwHigh = new Point3d(-hL, hW, roofHeight);

            var roofSurface = CreateQuadBrep(swLow, seLow, neHigh, nwHigh);
            if (roofSurface != null) surfaces.Add(roofSurface);

            Point3d nwGround = new Point3d(-hL, hW, 0);
            Point3d neGround = new Point3d(hL, hW, 0);

            var westWall = Brep.CreateFromCornerPoints(swLow, nwGround, nwHigh, 0.01);
            if (westWall != null) surfaces.Add(westWall);

            var eastWall = Brep.CreateFromCornerPoints(seLow, neHigh, neGround, 0.01);
            if (eastWall != null) surfaces.Add(eastWall);

            var backWall = CreateQuadBrep(nwGround, neGround, neHigh, nwHigh);
            if (backWall != null) surfaces.Add(backWall);

            return surfaces;
        }

        public static List<Brep> CreateFlatRoof(double length, double width, double overhang)
        {
            var surfaces = new List<Brep>();

            double totalLength = length + 2 * overhang;
            double totalWidth = width + 2 * overhang;

            double minSlope = 0.02;
            double slopeHeight = totalWidth * minSlope;

            Point3d sw = new Point3d(-totalLength / 2, -totalWidth / 2, 0);
            Point3d se = new Point3d(totalLength / 2, -totalWidth / 2, 0);
            Point3d ne = new Point3d(totalLength / 2, totalWidth / 2, slopeHeight);
            Point3d nw = new Point3d(-totalLength / 2, totalWidth / 2, slopeHeight);

            var roof = CreateQuadBrep(sw, se, ne, nw);
            if (roof != null) surfaces.Add(roof);

            return surfaces;
        }

        private static Brep CreateQuadBrep(Point3d p0, Point3d p1, Point3d p2, Point3d p3)
        {
            var tri1 = Brep.CreateFromCornerPoints(p0, p1, p2, 0.01);
            var tri2 = Brep.CreateFromCornerPoints(p0, p2, p3, 0.01);

            if (tri1 != null && tri2 != null)
            {
                var joined = Brep.JoinBreps(new[] { tri1, tri2 }, 0.01);
                if (joined != null && joined.Length > 0)
                    return joined[0];
            }

            return tri1 ?? tri2;
        }

        private static Brep CreatePolygonBrep(List<Point3d> points)
        {
            if (points.Count < 3) return null;

            var polyline = new Polyline(points);
            polyline.Add(points[0]);

            var curve = polyline.ToNurbsCurve();
            var breps = Brep.CreatePlanarBreps(curve, 0.01);

            if (breps != null && breps.Length > 0)
                return breps[0];

            return null;
        }

        public static Mesh CreateRoofMesh(List<Brep> breps)
        {
            var meshParams = MeshingParameters.Default;
            meshParams.MinimumEdgeLength = 0.1;
            meshParams.MaximumEdgeLength = 2.0;

            var combined = new Mesh();
            foreach (var brep in breps)
            {
                var meshes = Mesh.CreateFromBrep(brep, meshParams);
                if (meshes != null)
                {
                    foreach (var m in meshes)
                        combined.Append(m);
                }
            }
            return combined;
        }

        public static double CalculateRoofArea(List<Brep> breps)
        {
            double totalArea = 0;
            foreach (var brep in breps)
            {
                var area = AreaMassProperties.Compute(brep);
                if (area != null)
                    totalArea += area.Area;
            }
            return totalArea;
        }

        public static BoundingBox GetRoofBounds(List<Brep> breps)
        {
            var bbox = BoundingBox.Empty;
            foreach (var brep in breps)
            {
                bbox.Union(brep.GetBoundingBox(false));
            }
            return bbox;
        }
    }
}
