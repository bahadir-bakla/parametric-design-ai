using System;
using System.Collections.Generic;
using RoofAI.Models;
using Rhino.Geometry;

namespace RoofAI.Geometry.Generators
{
    /// <summary>
    /// Çatı geometri üreticisi
    /// </summary>
    public class RoofGenerator : IGeometryGenerator
    {
        public string ObjectType => "roof";
        
        public bool CanGenerate(ParametricObject parameters)
        {
            return parameters is RoofParameters;
        }
        
        public List<GeometryBase> Generate(ParametricObject parameters)
        {
            var roof = parameters as RoofParameters;
            if (roof == null)
                throw new ArgumentException("Parameters must be RoofParameters");
            
            switch (roof.RoofType.ToLower())
            {
                case "gable":
                case "besik":
                    return GenerateGableRoof(roof);
                    
                case "hip":
                case "dort_egim":
                    return GenerateHipRoof(roof);
                    
                case "gambrel":
                case "kirma":
                    return GenerateGambrelRoof(roof);
                    
                case "shed":
                case "tek_egim":
                    return GenerateShedRoof(roof);
                    
                case "flat":
                case "duz":
                    return GenerateFlatRoof(roof);
                    
                default:
                    return GenerateGableRoof(roof); // Default
            }
        }
        
        public List<GeometryBase> Update(ParametricObject parameters, List<Guid> existingGeometryIds)
        {
            // Mevcut geometriyi sil ve yeniden üret
            // (Rhino API'si ile mevcut objeleri silme kodu buraya gelecek)
            return Generate(parameters);
        }
        
        /// <summary>
        /// Beşik çatı (Gable) üret
        /// </summary>
        private List<GeometryBase> GenerateGableRoof(RoofParameters p)
        {
            var geometries = new List<GeometryBase>();
            
            double halfWidth = p.Width / 2.0;
            double ridgeHeight = p.RidgeHeight;
            double halfLength = p.Length / 2.0;
            double overhang = p.EaveOverhang;
            
            // Temel dikdörtgen (taban)
            var basePoints = new Point3d[]
            {
                new Point3d(-halfLength - overhang, -halfWidth - overhang, 0),
                new Point3d(halfLength + overhang, -halfWidth - overhang, 0),
                new Point3d(halfLength + overhang, halfWidth + overhang, 0),
                new Point3d(-halfLength - overhang, halfWidth + overhang, 0),
                new Point3d(-halfLength - overhang, -halfWidth - overhang, 0)
            };
            
            // Çatı yüzeyleri
            // Ön yüzey (x ekseninde)
            var frontPoints = new Point3d[]
            {
                new Point3d(-halfLength - overhang, -halfWidth - overhang, 0),
                new Point3d(-halfLength - overhang, 0, ridgeHeight),
                new Point3d(-halfLength - overhang, halfWidth + overhang, 0),
                new Point3d(-halfLength - overhang, -halfWidth - overhang, 0)
            };
            
            // Arka yüzey
            var backPoints = new Point3d[]
            {
                new Point3d(halfLength + overhang, -halfWidth - overhang, 0),
                new Point3d(halfLength + overhang, 0, ridgeHeight),
                new Point3d(halfLength + overhang, halfWidth + overhang, 0),
                new Point3d(halfLength + overhang, -halfWidth - overhang, 0)
            };
            
            // Yan yüzeyler (eğimli)
            var leftPoints = new Point3d[]
            {
                new Point3d(-halfLength - overhang, -halfWidth - overhang, 0),
                new Point3d(-halfLength - overhang, 0, ridgeHeight),
                new Point3d(halfLength + overhang, 0, ridgeHeight),
                new Point3d(halfLength + overhang, -halfWidth - overhang, 0),
                new Point3d(-halfLength - overhang, -halfWidth - overhang, 0)
            };
            
            var rightPoints = new Point3d[]
            {
                new Point3d(-halfLength - overhang, halfWidth + overhang, 0),
                new Point3d(-halfLength - overhang, 0, ridgeHeight),
                new Point3d(halfLength + overhang, 0, ridgeHeight),
                new Point3d(halfLength + overhang, halfWidth + overhang, 0),
                new Point3d(-halfLength - overhang, halfWidth + overhang, 0)
            };
            
            // Polysurface oluştur
            var frontPoly = new Polyline(frontPoints);
            var backPoly = new Polyline(backPoints);
            var leftPoly = new Polyline(leftPoints);
            var rightPoly = new Polyline(rightPoints);
            
            geometries.Add(frontPoly.ToNurbsCurve());
            geometries.Add(backPoly.ToNurbsCurve());
            geometries.Add(leftPoly.ToNurbsCurve());
            geometries.Add(rightPoly.ToNurbsCurve());
            
            // Mahya çizgisi (ridge)
            var ridgeLine = new Line(
                new Point3d(-halfLength - overhang, 0, ridgeHeight),
                new Point3d(halfLength + overhang, 0, ridgeHeight)
            );
            geometries.Add(ridgeLine.ToNurbsCurve());
            
            // Orientasyon uygula
            if (p.Orientation != 0)
            {
                double angleRad = p.Orientation * Math.PI / 180.0;
                var rotation = Transform.Rotation(angleRad, new Vector3d(0, 0, 1), Point3d.Origin);
                
                foreach (var geom in geometries)
                {
                    geom.Transform(rotation);
                }
            }
            
            return geometries;
        }
        
        /// <summary>
        /// Dört eğimli çatı (Hip) üret
        /// </summary>
        private List<GeometryBase> GenerateHipRoof(RoofParameters p)
        {
            var geometries = new List<GeometryBase>();
            
            double halfWidth = p.Width / 2.0;
            double halfLength = p.Length / 2.0;
            double overhang = p.EaveOverhang;
            double ridgeHeight = p.RidgeHeight;
            
            // Piramit tarzı çatı
            var corners = new Point3d[]
            {
                new Point3d(-halfLength - overhang, -halfWidth - overhang, 0),
                new Point3d(halfLength + overhang, -halfWidth - overhang, 0),
                new Point3d(halfLength + overhang, halfWidth + overhang, 0),
                new Point3d(-halfLength - overhang, halfWidth + overhang, 0)
            };
            
            // Mahya noktası (merkez)
            var ridgePoint = new Point3d(0, 0, ridgeHeight);
            
            // Her köşeden mahyaya çizgiler
            foreach (var corner in corners)
            {
                var hipLine = new Line(corner, ridgePoint);
                geometries.Add(hipLine.ToNurbsCurve());
            }
            
            // Taban çerçevesi
            var basePoly = new Polyline(corners);
            basePoly.Add(corners[0]); // Kapat
            geometries.Add(basePoly.ToNurbsCurve());
            
            // Orientasyon
            if (p.Orientation != 0)
            {
                double angleRad = p.Orientation * Math.PI / 180.0;
                var rotation = Transform.Rotation(angleRad, new Vector3d(0, 0, 1), Point3d.Origin);
                
                foreach (var geom in geometries)
                {
                    geom.Transform(rotation);
                }
            }
            
            return geometries;
        }
        
        /// <summary>
        /// Kırma çatı (Gambrel) üret
        /// </summary>
        private List<GeometryBase> GenerateGambrelRoof(RoofParameters p)
        {
            // Mansart tarzı - basitleştirilmiş
            return GenerateGableRoof(p); // Şimdilik gable olarak
        }
        
        /// <summary>
        /// Tek eğimli çatı (Shed) üret
        /// </summary>
        private List<GeometryBase> GenerateShedRoof(RoofParameters p)
        {
            var geometries = new List<GeometryBase>();
            
            double halfWidth = p.Width / 2.0;
            double halfLength = p.Length / 2.0;
            double overhang = p.EaveOverhang;
            double ridgeHeight = p.RidgeHeight;
            
            // Tek eğimli yüzey
            var shedPoints = new Point3d[]
            {
                new Point3d(-halfLength - overhang, -halfWidth - overhang, 0),
                new Point3d(-halfLength - overhang, halfWidth + overhang, 0),
                new Point3d(halfLength + overhang, halfWidth + overhang, ridgeHeight),
                new Point3d(halfLength + overhang, -halfWidth - overhang, ridgeHeight),
                new Point3d(-halfLength - overhang, -halfWidth - overhang, 0)
            };
            
            var shedPoly = new Polyline(shedPoints);
            geometries.Add(shedPoly.ToNurbsCurve());
            
            // Orientasyon
            if (p.Orientation != 0)
            {
                double angleRad = p.Orientation * Math.PI / 180.0;
                var rotation = Transform.Rotation(angleRad, new Vector3d(0, 0, 1), Point3d.Origin);
                
                foreach (var geom in geometries)
                {
                    geom.Transform(rotation);
                }
            }
            
            return geometries;
        }
        
        /// <summary>
        /// Düz çatı (Flat) üret
        /// </summary>
        private List<GeometryBase> GenerateFlatRoof(RoofParameters p)
        {
            var geometries = new List<GeometryBase>();
            
            double halfWidth = p.Width / 2.0;
            double halfLength = p.Length / 2.0;
            double overhang = p.EaveOverhang;
            double height = 0.2; // Düz çatı kalınlığı
            
            // Dikdörtgen prism
            var basePoints = new Point3d[]
            {
                new Point3d(-halfLength - overhang, -halfWidth - overhang, 0),
                new Point3d(halfLength + overhang, -halfWidth - overhang, 0),
                new Point3d(halfLength + overhang, halfWidth + overhang, 0),
                new Point3d(-halfLength - overhang, halfWidth + overhang, 0),
                new Point3d(-halfLength - overhang, -halfWidth - overhang, 0)
            };
            
            var flatPoly = new Polyline(basePoints);
            geometries.Add(flatPoly.ToNurbsCurve());
            
            // Orientasyon
            if (p.Orientation != 0)
            {
                double angleRad = p.Orientation * Math.PI / 180.0;
                var rotation = Transform.Rotation(angleRad, new Vector3d(0, 0, 1), Point3d.Origin);
                
                foreach (var geom in geometries)
                {
                    geom.Transform(rotation);
                }
            }
            
            return geometries;
        }
    }
}
