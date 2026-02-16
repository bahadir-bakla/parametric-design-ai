using System;
using System.Collections.Generic;
using RoofAI.Models;
using Rhino.Geometry;

namespace RoofAI.Geometry.Generators
{
    /// <summary>
    /// Cephe geometri üreticisi
    /// </summary>
    public class FacadeGenerator : IGeometryGenerator
    {
        public string ObjectType => "facade";
        
        public bool CanGenerate(ParametricObject parameters)
        {
            return parameters is FacadeParameters;
        }
        
        public List<GeometryBase> Generate(ParametricObject parameters)
        {
            var facade = parameters as FacadeParameters;
            if (facade == null)
                throw new ArgumentException("Parameters must be FacadeParameters");
            
            switch (facade.PatternType.ToLower())
            {
                case "grid":
                    return GenerateGridFacade(facade);
                case "diamond":
                    return GenerateDiamondFacade(facade);
                default:
                    return GenerateGridFacade(facade);
            }
        }
        
        public List<GeometryBase> Update(ParametricObject parameters, List<Guid> existingGeometryIds)
        {
            return Generate(parameters);
        }
        
        private List<GeometryBase> GenerateGridFacade(FacadeParameters p)
        {
            var geometries = new List<GeometryBase>();
            
            double panelWidth = p.Width / p.PanelCountHorizontal;
            double panelHeight = p.Height / p.PanelCountVertical;
            
            // Grid çizgileri
            for (int i = 0; i <= p.PanelCountHorizontal; i++)
            {
                double x = i * panelWidth - p.Width / 2.0;
                var line = new Line(
                    new Point3d(x, 0, 0),
                    new Point3d(x, 0, p.Height)
                );
                geometries.Add(line.ToNurbsCurve());
            }
            
            for (int j = 0; j <= p.PanelCountVertical; j++)
            {
                double z = j * panelHeight;
                var line = new Line(
                    new Point3d(-p.Width / 2.0, 0, z),
                    new Point3d(p.Width / 2.0, 0, z)
                );
                geometries.Add(line.ToNurbsCurve());
            }
            
            // Pencere açıklıkları
            double windowWidth = panelWidth * p.WindowRatio;
            double windowHeight = panelHeight * p.WindowRatio;
            
            for (int i = 0; i < p.PanelCountHorizontal; i++)
            {
                for (int j = 0; j < p.PanelCountVertical; j++)
                {
                    double x = i * panelWidth - p.Width / 2.0 + panelWidth / 2.0;
                    double z = j * panelHeight + panelHeight / 2.0;
                    
                    var windowRect = new Rectangle3d(
                        Plane.WorldZX,
                        new Interval(x - windowWidth / 2, x + windowWidth / 2),
                        new Interval(z - windowHeight / 2, z + windowHeight / 2)
                    );
                    
                    geometries.Add(windowRect.ToNurbsCurve());
                }
            }
            
            return geometries;
        }
        
        private List<GeometryBase> GenerateDiamondFacade(FacadeParameters p)
        {
            var geometries = new List<GeometryBase>();
            
            double panelWidth = p.Width / p.PanelCountHorizontal;
            double panelHeight = p.Height / p.PanelCountVertical;
            
            // Elmas pattern
            for (int i = 0; i < p.PanelCountHorizontal; i++)
            {
                for (int j = 0; j < p.PanelCountVertical; j++)
                {
                    double x = i * panelWidth - p.Width / 2.0 + panelWidth / 2.0;
                    double z = j * panelHeight + panelHeight / 2.0;
                    
                    var diamondPoints = new Point3d[]
                    {
                        new Point3d(x, 0, z + panelHeight / 2.0),
                        new Point3d(x + panelWidth / 2.0, 0, z),
                        new Point3d(x, 0, z - panelHeight / 2.0),
                        new Point3d(x - panelWidth / 2.0, 0, z),
                        new Point3d(x, 0, z + panelHeight / 2.0)
                    };
                    
                    var diamond = new Polyline(diamondPoints);
                    geometries.Add(diamond.ToNurbsCurve());
                }
            }
            
            return geometries;
        }
    }
}
