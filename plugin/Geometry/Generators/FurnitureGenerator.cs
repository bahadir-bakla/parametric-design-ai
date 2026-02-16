using System;
using System.Collections.Generic;
using RoofAI.Models;
using Rhino.Geometry;

namespace RoofAI.Geometry.Generators
{
    /// <summary>
    /// Mobilya geometri üreticisi
    /// </summary>
    public class FurnitureGenerator : IGeometryGenerator
    {
        public string ObjectType => "furniture";
        
        public bool CanGenerate(ParametricObject parameters)
        {
            return parameters is FurnitureParameters;
        }
        
        public List<GeometryBase> Generate(ParametricObject parameters)
        {
            var furniture = parameters as FurnitureParameters;
            if (furniture == null)
                throw new ArgumentException("Parameters must be FurnitureParameters");
            
            switch (furniture.FurnitureType.ToLower())
            {
                case "table":
                case "masa":
                    return GenerateTable(furniture);
                    
                case "chair":
                case "sandalye":
                    return GenerateChair(furniture);
                    
                case "shelf":
                case "raf":
                    return GenerateShelf(furniture);
                    
                case "sofa":
                case "kanepe":
                    return GenerateSofa(furniture);
                    
                case "bed":
                case "yatak":
                    return GenerateBed(furniture);
                    
                default:
                    return GenerateTable(furniture); // Default
            }
        }
        
        public List<GeometryBase> Update(ParametricObject parameters, List<Guid> existingGeometryIds)
        {
            return Generate(parameters);
        }
        
        private List<GeometryBase> GenerateTable(FurnitureParameters p)
        {
            var geometries = new List<GeometryBase>();
            
            double halfLength = p.Length / 2.0;
            double halfWidth = p.Width / 2.0;
            double legHeight = p.Height * 0.95;
            double topThickness = p.Height * 0.05;
            
            // Masa tablası (dikdörtgen)
            var tableTopPoints = new Point3d[]
            {
                new Point3d(-halfLength, -halfWidth, legHeight),
                new Point3d(halfLength, -halfWidth, legHeight),
                new Point3d(halfLength, halfWidth, legHeight),
                new Point3d(-halfLength, halfWidth, legHeight),
                new Point3d(-halfLength, -halfWidth, legHeight)
            };
            var tableTop = new Polyline(tableTopPoints);
            geometries.Add(tableTop.ToNurbsCurve());
            
            // 4 ayak
            double legOffsetX = halfLength * 0.8;
            double legOffsetY = halfWidth * 0.8;
            double legSize = 0.05; // 5cm leg thickness
            
            var legPositions = new (double x, double y)[]
            {
                (-legOffsetX, -legOffsetY),
                (legOffsetX, -legOffsetY),
                (legOffsetX, legOffsetY),
                (-legOffsetX, legOffsetY)
            };
            
            foreach (var pos in legPositions)
            {
                var legLine = new Line(
                    new Point3d(pos.x, pos.y, 0),
                    new Point3d(pos.x, pos.y, legHeight)
                );
                geometries.Add(legLine.ToNurbsCurve());
            }
            
            return geometries;
        }
        
        private List<GeometryBase> GenerateChair(FurnitureParameters p)
        {
            var geometries = new List<GeometryBase>();
            
            double halfWidth = p.Width / 2.0;
            double halfDepth = p.Length / 2.0;
            double seatHeight = p.Height * 0.45;
            double backrestHeight = p.Height;
            
            // Oturma yeri
            var seatPoints = new Point3d[]
            {
                new Point3d(-halfDepth, -halfWidth, seatHeight),
                new Point3d(halfDepth, -halfWidth, seatHeight),
                new Point3d(halfDepth, halfWidth, seatHeight),
                new Point3d(-halfDepth, halfWidth, seatHeight),
                new Point3d(-halfDepth, -halfWidth, seatHeight)
            };
            var seat = new Polyline(seatPoints);
            geometries.Add(seat.ToNurbsCurve());
            
            // 4 ayak
            double legOffset = 0.9; // %90 offset from center
            var legPositions = new (double x, double y)[]
            {
                (-halfDepth * legOffset, -halfWidth * legOffset),
                (halfDepth * legOffset, -halfWidth * legOffset),
                (halfDepth * legOffset, halfWidth * legOffset),
                (-halfDepth * legOffset, halfWidth * legOffset)
            };
            
            foreach (var pos in legPositions)
            {
                var legLine = new Line(
                    new Point3d(pos.x, pos.y, 0),
                    new Point3d(pos.x, pos.y, seatHeight)
                );
                geometries.Add(legLine.ToNurbsCurve());
            }
            
            // Sırtlık (backrest)
            if (p.HasBackrest)
            {
                var backrestLeft = new Line(
                    new Point3d(-halfDepth, -halfWidth, seatHeight),
                    new Point3d(-halfDepth, -halfWidth, backrestHeight)
                );
                var backrestRight = new Line(
                    new Point3d(halfDepth, -halfWidth, seatHeight),
                    new Point3d(halfDepth, -halfWidth, backrestHeight)
                );
                var backrestTop = new Line(
                    new Point3d(-halfDepth, -halfWidth, backrestHeight),
                    new Point3d(halfDepth, -halfWidth, backrestHeight)
                );
                
                geometries.Add(backrestLeft.ToNurbsCurve());
                geometries.Add(backrestRight.ToNurbsCurve());
                geometries.Add(backrestTop.ToNurbsCurve());
            }
            
            // Kolçaklar (armrests)
            if (p.HasArmrest)
            {
                double armrestHeight = seatHeight * 1.2;
                var leftArmrest = new Line(
                    new Point3d(-halfDepth, halfWidth, seatHeight),
                    new Point3d(-halfDepth, halfWidth, armrestHeight)
                );
                var rightArmrest = new Line(
                    new Point3d(halfDepth, halfWidth, seatHeight),
                    new Point3d(halfDepth, halfWidth, armrestHeight)
                );
                
                geometries.Add(leftArmrest.ToNurbsCurve());
                geometries.Add(rightArmrest.ToNurbsCurve());
            }
            
            return geometries;
        }
        
        private List<GeometryBase> GenerateShelf(FurnitureParameters p)
        {
            var geometries = new List<GeometryBase>();
            
            double halfWidth = p.Width / 2.0;
            double halfDepth = p.Length / 2.0;
            double shelfSpacing = p.Height / (p.ShelfCount + 1);
            double thickness = 0.02; // 2cm thickness
            
            // Yan paneller
            var leftPanel = new Point3d[]
            {
                new Point3d(-halfDepth, -halfWidth, 0),
                new Point3d(-halfDepth, -halfWidth, p.Height),
                new Point3d(-halfDepth, halfWidth, p.Height),
                new Point3d(-halfDepth, halfWidth, 0),
                new Point3d(-halfDepth, -halfWidth, 0)
            };
            var rightPanel = new Point3d[]
            {
                new Point3d(halfDepth, -halfWidth, 0),
                new Point3d(halfDepth, -halfWidth, p.Height),
                new Point3d(halfDepth, halfWidth, p.Height),
                new Point3d(halfDepth, halfWidth, 0),
                new Point3d(halfDepth, -halfWidth, 0)
            };
            
            geometries.Add(new Polyline(leftPanel).ToNurbsCurve());
            geometries.Add(new Polyline(rightPanel).ToNurbsCurve());
            
            // Raflar
            for (int i = 1; i <= p.ShelfCount; i++)
            {
                double z = i * shelfSpacing;
                var shelf = new Point3d[]
                {
                    new Point3d(-halfDepth, -halfWidth, z),
                    new Point3d(halfDepth, -halfWidth, z),
                    new Point3d(halfDepth, halfWidth, z),
                    new Point3d(-halfDepth, halfWidth, z),
                    new Point3d(-halfDepth, -halfWidth, z)
                };
                geometries.Add(new Polyline(shelf).ToNurbsCurve());
            }
            
            return geometries;
        }
        
        private List<GeometryBase> GenerateSofa(FurnitureParameters p)
        {
            var geometries = new List<GeometryBase>();
            
            double halfWidth = p.Width / 2.0;
            double halfDepth = p.Length / 2.0;
            double seatHeight = p.Height * 0.4;
            double backrestHeight = p.Height;
            double armrestWidth = p.Width * 0.15;
            
            // Oturma yeri
            var seatPoints = new Point3d[]
            {
                new Point3d(-halfDepth, -halfWidth + armrestWidth, seatHeight),
                new Point3d(halfDepth, -halfWidth + armrestWidth, seatHeight),
                new Point3d(halfDepth, halfWidth - armrestWidth, seatHeight),
                new Point3d(-halfDepth, halfWidth - armrestWidth, seatHeight),
                new Point3d(-halfDepth, -halfWidth + armrestWidth, seatHeight)
            };
            geometries.Add(new Polyline(seatPoints).ToNurbsCurve());
            
            // Sırtlık
            var backrestPoints = new Point3d[]
            {
                new Point3d(-halfDepth, -halfWidth, seatHeight),
                new Point3d(-halfDepth, -halfWidth, backrestHeight),
                new Point3d(halfDepth, -halfWidth, backrestHeight),
                new Point3d(halfDepth, -halfWidth, seatHeight),
                new Point3d(-halfDepth, -halfWidth, seatHeight)
            };
            geometries.Add(new Polyline(backrestPoints).ToNurbsCurve());
            
            // Kolçaklar
            var leftArmrest = new Point3d[]
            {
                new Point3d(-halfDepth, halfWidth, 0),
                new Point3d(-halfDepth, halfWidth, seatHeight),
                new Point3d(halfDepth, halfWidth, seatHeight),
                new Point3d(halfDepth, halfWidth, 0),
                new Point3d(-halfDepth, halfWidth, 0)
            };
            var rightArmrest = new Point3d[]
            {
                new Point3d(-halfDepth, -halfWidth, 0),
                new Point3d(-halfDepth, -halfWidth, seatHeight),
                new Point3d(halfDepth, -halfWidth, seatHeight),
                new Point3d(halfDepth, -halfWidth, 0),
                new Point3d(-halfDepth, -halfWidth, 0)
            };
            geometries.Add(new Polyline(leftArmrest).ToNurbsCurve());
            geometries.Add(new Polyline(rightArmrest).ToNurbsCurve());
            
            return geometries;
        }
        
        private List<GeometryBase> GenerateBed(FurnitureParameters p)
        {
            var geometries = new List<GeometryBase>();
            
            double halfWidth = p.Width / 2.0;
            double halfLength = p.Length / 2.0;
            double baseHeight = p.Height * 0.3;
            double mattressHeight = p.Height * 0.7;
            double headboardHeight = p.Height * 1.5;
            
            // Yatak tabanı
            var basePoints = new Point3d[]
            {
                new Point3d(-halfLength, -halfWidth, 0),
                new Point3d(halfLength, -halfWidth, 0),
                new Point3d(halfLength, halfWidth, 0),
                new Point3d(-halfLength, halfWidth, 0),
                new Point3d(-halfLength, -halfWidth, 0)
            };
            geometries.Add(new Polyline(basePoints).ToNurbsCurve());
            
            // Yatak yüzeyi (mattress)
            var mattressPoints = new Point3d[]
            {
                new Point3d(-halfLength, -halfWidth, baseHeight),
                new Point3d(halfLength, -halfWidth, baseHeight),
                new Point3d(halfLength, halfWidth, baseHeight),
                new Point3d(-halfLength, halfWidth, baseHeight),
                new Point3d(-halfLength, -halfWidth, baseHeight)
            };
            geometries.Add(new Polyline(mattressPoints).ToNurbsCurve());
            
            // Başlık (headboard)
            var headboardPoints = new Point3d[]
            {
                new Point3d(-halfLength, -halfWidth, baseHeight),
                new Point3d(-halfLength, -halfWidth, headboardHeight),
                new Point3d(-halfLength, halfWidth, headboardHeight),
                new Point3d(-halfLength, halfWidth, baseHeight),
                new Point3d(-halfLength, -halfWidth, baseHeight)
            };
            geometries.Add(new Polyline(headboardPoints).ToNurbsCurve());
            
            return geometries;
        }
    }
}
