using System;
using System.Collections.Generic;
using RoofAI.Models;
using Rhino.Geometry;

namespace RoofAI.Geometry
{
    /// <summary>
    /// Geometry generator interface
    /// </summary>
    public interface IGeometryGenerator
    {
        string ObjectType { get; }
        bool CanGenerate(ParametricObject parameters);
        List<GeometryBase> Generate(ParametricObject parameters);
        List<GeometryBase> Update(ParametricObject parameters, List<Guid> existingGeometryIds);
    }
    
    /// <summary>
    /// Factory pattern ile geometry üretim yönetimi
    /// </summary>
    public class GeometryEngine
    {
        private readonly Dictionary<string, IGeometryGenerator> _generators = 
            new Dictionary<string, IGeometryGenerator>();
        
        public GeometryEngine()
        {
            // Generator'ları kaydet
            RegisterGenerator(new RoofGenerator());
            RegisterGenerator(new FacadeGenerator());
            RegisterGenerator(new FurnitureGenerator());
            RegisterGenerator(new InteriorGenerator());
        }
        
        public void RegisterGenerator(IGeometryGenerator generator)
        {
            _generators[generator.ObjectType] = generator;
        }
        
        /// <summary>
        /// Parametrik objeden geometri üret
        /// </summary>
        public GenerationResult Generate(ParametricObject parameters)
        {
            try
            {
                if (!_generators.TryGetValue(parameters.ObjectType, out var generator))
                {
                    return new GenerationResult
                    {
                        Success = false,
                        ErrorMessage = $"Generator bulunamadı: {parameters.ObjectType}"
                    };
                }
                
                if (!generator.CanGenerate(parameters))
                {
                    return new GenerationResult
                    {
                        Success = false,
                        ErrorMessage = "Parametreler üretim için uygun değil"
                    };
                }
                
                var geometries = generator.Generate(parameters);
                
                return new GenerationResult
                {
                    Success = true,
                    Geometries = geometries,
                    Parameters = parameters
                };
            }
            catch (Exception ex)
            {
                return new GenerationResult
                {
                    Success = false,
                    ErrorMessage = $"Üretim hatası: {ex.Message}"
                };
            }
        }
        
        /// <summary>
        /// Mevcut geometriyi güncelle
        /// </summary>
        public GenerationResult Update(ParametricObject parameters, List<Guid> existingIds)
        {
            try
            {
                if (!_generators.TryGetValue(parameters.ObjectType, out var generator))
                {
                    return new GenerationResult
                    {
                        Success = false,
                        ErrorMessage = $"Generator bulunamadı: {parameters.ObjectType}"
                    };
                }
                
                var geometries = generator.Update(parameters, existingIds);
                
                return new GenerationResult
                {
                    Success = true,
                    Geometries = geometries,
                    Parameters = parameters,
                    IsUpdate = true
                };
            }
            catch (Exception ex)
            {
                return new GenerationResult
                {
                    Success = false,
                    ErrorMessage = $"Güncelleme hatası: {ex.Message}"
                };
            }
        }
    }
    
    /// <summary>
    /// Üretim sonucu
    /// </summary>
    public class GenerationResult
    {
        public bool Success { get; set; }
        public List<GeometryBase> Geometries { get; set; } = new List<GeometryBase>();
        public ParametricObject Parameters { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsUpdate { get; set; }
    }
}
