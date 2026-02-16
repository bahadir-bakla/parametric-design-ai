using System;
using System.Collections.Generic;
using RoofAI.Models;
using Rhino.Geometry;

namespace RoofAI.Geometry.Generators
{
    /// <summary>
    /// İç mekan geometri üreticisi
    /// </summary>
    public class InteriorGenerator : IGeometryGenerator
    {
        public string ObjectType => "interior";
        
        public bool CanGenerate(ParametricObject parameters)
        {
            return parameters is InteriorParameters;
        }
        
        public List<GeometryBase> Generate(ParametricObject parameters)
        {
            var interior = parameters as InteriorParameters;
            if (interior == null)
                throw new ArgumentException("Parameters must be InteriorParameters");
            
            switch (interior.WallCount)
            {
                case 3:
                    return GenerateTriangularRoom(interior);
                case 4:
                    return GenerateRectangularRoom(interior);
                case 5:
                    return GeneratePentagonalRoom(interior);
                case 6:
                    return GenerateHexagonalRoom(interior);
                default:
                    return GenerateRectangularRoom(interior); // Default
            }
        }
        
        public List<GeometryBase> Update(ParametricObject parameters, List<Guid> existingGeometryIds)
        {
            return Generate(parameters);
        }
        
        private List<GeometryBase> GenerateRectangularRoom(InteriorParameters p)
        {
            var geometries = new List<GeometryBase>();
            
            double halfLength = p.RoomLength / 2.0;
            double halfWidth = p.RoomWidth / 2.0;
            double height = p.RoomHeight;
            
            // 4 duvar
            var wall1 = new Line(
                new Point3d(-halfLength, -halfWidth, 0),
                new Point3d(-halfLength, -halfWidth, height)
            );
            var wall2 = new Line(
                new Point3d(halfLength, -halfWidth, 0),
                new Point3d(halfLength, -halfWidth, height)
            );
            var wall3 = new Line(
                new Point3d(halfLength, halfWidth, 0),
                new Point3d(halfLength, halfWidth, height)
            );
            var wall4 = new Line(
                new Point3d(-halfLength, halfWidth, 0),
                new Point3d(-halfLength, halfWidth, height)
            );
            
            geometries.Add(wall1.ToNurbsCurve());
            geometries.Add(wall2.ToNurbsCurve());
            geometries.Add(wall3.ToNurbsCurve());
            geometries.Add(wall4.ToNurbsCurve());
            
            // Üst bağlantı (tavan outline)
            var ceilingPoints = new Point3d[]
            {
                new Point3d(-halfLength, -halfWidth, height),
                new Point3d(halfLength, -halfWidth, height),
                new Point3d(halfLength, halfWidth, height),
                new Point3d(-halfLength, halfWidth, height),
                new Point3d(-halfLength, -halfWidth, height)
            };
            geometries.Add(new Polyline(ceilingPoints).ToNurbsCurve());
            
            // Zemin
            if (p.CreateFloor)
            {
                var floorPoints = new Point3d[]
                {
                    new Point3d(-halfLength, -halfWidth, 0),
                    new Point3d(halfLength, -halfWidth, 0),
                    new Point3d(halfLength, halfWidth, 0),
                    new Point3d(-halfLength, halfWidth, 0),
                    new Point3d(-halfLength, -halfWidth, 0)
                };
                geometries.Add(new Polyline(floorPoints).ToNurbsCurve());
            }
            
            // Kapılar
            foreach (var door in p.Doors)
            {
                var doorGeometry = GenerateDoor(door, p);
                geometries.AddRange(doorGeometry);
            }
            
            // Pencereler
            foreach (var window in p.Windows)
            {
                var windowGeometry = GenerateWindow(window, p);
                geometries.AddRange(windowGeometry);
            }
            
            return geometries;
        }
        
        private List<GeometryBase> GenerateTriangularRoom(InteriorParameters p)
        {
            var geometries = new List<GeometryBase>();
            
            double radius = Math.Max(p.RoomLength, p.RoomWidth) / 2.0;
            double height = p.RoomHeight;
            
            // Üçgen köşeler (ekilateral)
            var corners = new Point3d[]
            {
                new Point3d(0, radius, 0),
                new Point3d(-radius * 0.866, -radius * 0.5, 0),
                new Point3d(radius * 0.866, -radius * 0.5, 0)
            };
            
            // 3 duvar
            for (int i = 0; i < 3; i++)
            {
                var start = corners[i];
                var end = corners[(i + 1) % 3];
                
                var wallBottom = new Line(start, end);
                var wallTop = new Line(
                    new Point3d(start.X, start.Y, height),
                    new Point3d(end.X, end.Y, height)
                );
                
                geometries.Add(wallBottom.ToNurbsCurve());
                geometries.Add(wallTop.ToNurbsCurve());
            }
            
            // Dikey köşe çizgileri
            foreach (var corner in corners)
            {
                var vertical = new Line(
                    corner,
                    new Point3d(corner.X, corner.Y, height)
                );
                geometries.Add(vertical.ToNurbsCurve());
            }
            
            return geometries;
        }
        
        private List<GeometryBase> GeneratePentagonalRoom(InteriorParameters p)
        {
            var geometries = new List<GeometryBase>();
            
            double radius = Math.Max(p.RoomLength, p.RoomWidth) / 2.0;
            double height = p.RoomHeight;
            
            // Beşgen köşeler
            var corners = new List<Point3d>();
            for (int i = 0; i < 5; i++)
            {
                double angle = (i * 2.0 * Math.PI / 5.0) - (Math.PI / 2.0);
                corners.Add(new Point3d(
                    radius * Math.Cos(angle),
                    radius * Math.Sin(angle),
                    0
                ));
            }
            
            // 5 duvar
            for (int i = 0; i < 5; i++)
            {
                var start = corners[i];
                var end = corners[(i + 1) % 5];
                
                var wallBottom = new Line(start, end);
                var wallTop = new Line(
                    new Point3d(start.X, start.Y, height),
                    new Point3d(end.X, end.Y, height)
                );
                
                geometries.Add(wallBottom.ToNurbsCurve());
                geometries.Add(wallTop.ToNurbsCurve());
                
                // Dikey köşe
                var vertical = new Line(
                    start,
                    new Point3d(start.X, start.Y, height)
                );
                geometries.Add(vertical.ToNurbsCurve());
            }
            
            return geometries;
        }
        
        private List<GeometryBase> GenerateHexagonalRoom(InteriorParameters p)
        {
            var geometries = new List<GeometryBase>();
            
            double radius = Math.Max(p.RoomLength, p.RoomWidth) / 2.0;
            double height = p.RoomHeight;
            
            // Altıgen köşeler
            var corners = new List<Point3d>();
            for (int i = 0; i < 6; i++)
            {
                double angle = i * 2.0 * Math.PI / 6.0;
                corners.Add(new Point3d(
                    radius * Math.Cos(angle),
                    radius * Math.Sin(angle),
                    0
                ));
            }
            
            // 6 duvar
            for (int i = 0; i < 6; i++)
            {
                var start = corners[i];
                var end = corners[(i + 1) % 6];
                
                var wallBottom = new Line(start, end);
                geometries.Add(wallBottom.ToNurbsCurve());
                
                // Dikey köşe
                var vertical = new Line(
                    start,
                    new Point3d(start.X, start.Y, height)
                );
                geometries.Add(vertical.ToNurbsCurve());
            }
            
            // Tavan outline
            var ceilingPoints = new List<Point3d>();
            foreach (var corner in corners)
            {
                ceilingPoints.Add(new Point3d(corner.X, corner.Y, height));
            }
            ceilingPoints.Add(ceilingPoints[0]); // Close loop
            geometries.Add(new Polyline(ceilingPoints).ToNurbsCurve());
            
            return geometries;
        }
        
        private List<GeometryBase> GenerateDoor(DoorInfo door, InteriorParameters room)
        {
            var geometries = new List<GeometryBase>();
            
            // Basit kapı gösterimi (açıklık)
            double halfWidth = door.Width / 2.0;
            
            // Kapı çerçevesi
            var doorFrame = new Point3d[]
            {
                new Point3d(-halfWidth, 0, 0),
                new Point3d(-halfWidth, 0, door.Height),
                new Point3d(halfWidth, 0, door.Height),
                new Point3d(halfWidth, 0, 0),
                new Point3d(-halfWidth, 0, 0)
            };
            
            geometries.Add(new Polyline(doorFrame).ToNurbsCurve());
            
            // Kapı kanadı (açık durumda)
            var doorLeaf = new Line(
                new Point3d(-halfWidth, 0, 0),
                new Point3d(-halfWidth - door.Width * 0.8, door.Width * 0.8, 0)
            );
            geometries.Add(doorLeaf.ToNurbsCurve());
            
            return geometries;
        }
        
        private List<GeometryBase> GenerateWindow(WindowInfo window, InteriorParameters room)
        {
            var geometries = new List<GeometryBase>();
            
            double halfWidth = window.Width / 2.0;
            double bottomZ = window.SillHeight;
            double topZ = window.SillHeight + window.Height;
            
            // Pencere çerçevesi
            var windowFrame = new Point3d[]
            {
                new Point3d(-halfWidth, 0, bottomZ),
                new Point3d(-halfWidth, 0, topZ),
                new Point3d(halfWidth, 0, topZ),
                new Point3d(halfWidth, 0, bottomZ),
                new Point3d(-halfWidth, 0, bottomZ)
            };
            
            geometries.Add(new Polyline(windowFrame).ToNurbsCurve());
            
            // Pencere bölmesi (orta)
            var mullion = new Line(
                new Point3d(0, 0, bottomZ),
                new Point3d(0, 0, topZ)
            );
            geometries.Add(mullion.ToNurbsCurve());
            
            return geometries;
        }
    }
}
