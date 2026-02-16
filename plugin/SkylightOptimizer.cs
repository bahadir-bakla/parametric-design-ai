using System;
using System.Collections.Generic;
using Rhino.Geometry;

namespace RoofAI
{
    public class SkylightInfo
    {
        public Point3d Center { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public Brep Geometry { get; set; }
        public double DaylightFactor { get; set; }
        public int FaceIndex { get; set; }
    }

    public class SkylightResult
    {
        public List<SkylightInfo> Skylights { get; set; } = new List<SkylightInfo>();
        public double TotalGlazingArea { get; set; }
        public double GlazingToFloorRatio { get; set; }
        public double AverageDaylightFactor { get; set; }
    }

    public static class SkylightOptimizer
    {
        private static readonly Dictionary<string, RoomRequirement> RoomRequirements = new Dictionary<string, RoomRequirement>
        {
            ["salon"] = new RoomRequirement { MinLux = 300, TargetLux = 500, GlazingRatio = 0.15 },
            ["mutfak"] = new RoomRequirement { MinLux = 300, TargetLux = 500, GlazingRatio = 0.12 },
            ["yatak odasi"] = new RoomRequirement { MinLux = 150, TargetLux = 300, GlazingRatio = 0.10 },
            ["calisma odasi"] = new RoomRequirement { MinLux = 500, TargetLux = 750, GlazingRatio = 0.18 },
            ["banyo"] = new RoomRequirement { MinLux = 150, TargetLux = 250, GlazingRatio = 0.08 },
        };

        public static SkylightResult OptimizeSkylights(List<Brep> roofBreps, string roomType,
            int count, string optimizationGoal, double latitude = 41.0)
        {
            var result = new SkylightResult();

            if (!RoomRequirements.TryGetValue(roomType.ToLower(), out var requirement))
                requirement = RoomRequirements["salon"];

            double roofArea = GeometryEngine.CalculateRoofArea(roofBreps);
            double targetGlazingArea = roofArea * requirement.GlazingRatio;
            double perWindowArea = targetGlazingArea / count;
            double windowSize = Math.Sqrt(perWindowArea);
            double windowWidth = Math.Max(0.6, Math.Min(windowSize, 1.4));
            double windowHeight = Math.Max(0.6, Math.Min(perWindowArea / windowWidth, 2.0));

            var suitableFaces = FindSuitableFaces(roofBreps, latitude, optimizationGoal);

            var placements = DistributeWindows(suitableFaces, count, windowWidth, windowHeight);

            foreach (var placement in placements)
            {
                var skylight = CreateSkylight(placement.Point, placement.Normal,
                    placement.UDir, windowWidth, windowHeight);

                if (skylight != null)
                {
                    double df = EstimateDaylightFactor(placement.Point, windowWidth, windowHeight,
                        placement.Normal, latitude);

                    result.Skylights.Add(new SkylightInfo
                    {
                        Center = placement.Point,
                        Width = windowWidth,
                        Height = windowHeight,
                        Geometry = skylight,
                        DaylightFactor = df,
                        FaceIndex = placement.FaceIndex
                    });
                }
            }

            result.TotalGlazingArea = windowWidth * windowHeight * result.Skylights.Count;
            result.GlazingToFloorRatio = roofArea > 0 ? result.TotalGlazingArea / roofArea : 0;

            double totalDf = 0;
            foreach (var s in result.Skylights)
                totalDf += s.DaylightFactor;
            result.AverageDaylightFactor = result.Skylights.Count > 0
                ? totalDf / result.Skylights.Count : 0;

            return result;
        }

        private static List<FacePlacement> FindSuitableFaces(List<Brep> breps, double latitude, string goal)
        {
            var faces = new List<FacePlacement>();

            double preferredAzimuth = latitude > 0 ? 180 : 0;

            for (int bIdx = 0; bIdx < breps.Count; bIdx++)
            {
                var brep = breps[bIdx];
                for (int fIdx = 0; fIdx < brep.Faces.Count; fIdx++)
                {
                    var face = brep.Faces[fIdx];
                    var frameMid = face.FrameAt(face.Domain(0).Mid, face.Domain(1).Mid, out Plane frame);

                    if (!frameMid) continue;

                    Vector3d normal = frame.ZAxis;
                    if (normal.Z < 0.1) continue;

                    double slopeAngle = Math.Acos(Math.Abs(normal.Z)) * 180.0 / Math.PI;
                    if (slopeAngle > 75) continue;

                    double azimuth = Math.Atan2(normal.X, normal.Y) * 180.0 / Math.PI;
                    if (azimuth < 0) azimuth += 360;

                    double score = CalculateFaceScore(slopeAngle, azimuth, preferredAzimuth, goal);

                    var area = AreaMassProperties.Compute(face);
                    double faceArea = area?.Area ?? 0;

                    faces.Add(new FacePlacement
                    {
                        BrepIndex = bIdx,
                        FaceIndex = fIdx,
                        Point = frame.Origin,
                        Normal = normal,
                        UDir = frame.XAxis,
                        Score = score,
                        Area = faceArea
                    });
                }
            }

            faces.Sort((a, b) => b.Score.CompareTo(a.Score));
            return faces;
        }

        private static double CalculateFaceScore(double slopeAngle, double azimuth,
            double preferredAzimuth, string goal)
        {
            double azDiff = Math.Abs(azimuth - preferredAzimuth);
            if (azDiff > 180) azDiff = 360 - azDiff;
            double azScore = 1.0 - azDiff / 180.0;

            double slopeScore = 1.0 - Math.Abs(slopeAngle - 30) / 60.0;
            slopeScore = Math.Max(0, slopeScore);

            switch (goal?.ToLower())
            {
                case "maximize_daylight":
                    return azScore * 0.7 + slopeScore * 0.3;
                case "minimize_glare":
                    return (1 - azScore) * 0.5 + slopeScore * 0.5;
                default:
                    return azScore * 0.5 + slopeScore * 0.5;
            }
        }

        private static List<FacePlacement> DistributeWindows(List<FacePlacement> faces,
            int count, double windowWidth, double windowHeight)
        {
            var placements = new List<FacePlacement>();
            if (faces.Count == 0) return placements;

            double windowArea = windowWidth * windowHeight;
            double spacing = Math.Max(windowWidth, windowHeight) * 1.5;

            foreach (var face in faces)
            {
                if (placements.Count >= count) break;

                int windowsOnFace = Math.Max(1, (int)(face.Area / (windowArea * 3)));
                windowsOnFace = Math.Min(windowsOnFace, count - placements.Count);

                for (int i = 0; i < windowsOnFace; i++)
                {
                    double offset = (i - (windowsOnFace - 1) / 2.0) * spacing;
                    Point3d pos = face.Point + face.UDir * offset;

                    placements.Add(new FacePlacement
                    {
                        BrepIndex = face.BrepIndex,
                        FaceIndex = face.FaceIndex,
                        Point = pos,
                        Normal = face.Normal,
                        UDir = face.UDir,
                        Score = face.Score,
                        Area = face.Area
                    });
                }
            }

            return placements;
        }

        private static Brep CreateSkylight(Point3d center, Vector3d normal, Vector3d uDir,
            double width, double height)
        {
            Vector3d vDir = Vector3d.CrossProduct(normal, uDir);
            vDir.Unitize();
            uDir.Unitize();

            Point3d p0 = center - uDir * width / 2 - vDir * height / 2;
            Point3d p1 = center + uDir * width / 2 - vDir * height / 2;
            Point3d p2 = center + uDir * width / 2 + vDir * height / 2;
            Point3d p3 = center - uDir * width / 2 + vDir * height / 2;

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

        private static double EstimateDaylightFactor(Point3d position, double width, double height,
            Vector3d normal, double latitude)
        {
            double area = width * height;
            double slopeAngle = Math.Acos(Math.Abs(normal.Z)) * 180.0 / Math.PI;

            double baseDf = area * 3.0;

            double slopeFactor = Math.Cos(slopeAngle * Math.PI / 180.0);
            baseDf *= (0.5 + 0.5 * slopeFactor);

            double latFactor = 1.0 - Math.Abs(latitude) / 90.0 * 0.3;
            baseDf *= latFactor;

            return Math.Min(baseDf, 15.0);
        }

        private class FacePlacement
        {
            public int BrepIndex { get; set; }
            public int FaceIndex { get; set; }
            public Point3d Point { get; set; }
            public Vector3d Normal { get; set; }
            public Vector3d UDir { get; set; }
            public double Score { get; set; }
            public double Area { get; set; }
        }

        private class RoomRequirement
        {
            public double MinLux { get; set; }
            public double TargetLux { get; set; }
            public double GlazingRatio { get; set; }
        }
    }
}
