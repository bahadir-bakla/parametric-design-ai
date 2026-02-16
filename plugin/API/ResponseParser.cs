using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using RoofAI.Models;

namespace RoofAI.API
{
    /// <summary>
    /// AI yanıtlarını parse eden ve parametrik objelere dönüştüren sınıf
    /// </summary>
    public class ResponseParser
    {
        /// <summary>
        /// JSON yanıtı parse et ve doğru parametrik objeye dönüştür
        /// </summary>
        public ParseResult ParseResponse(JObject json, string originalCommand)
        {
            try
            {
                // Action tipini belirle
                string action = json["action"]?.ToString()?.ToLower() ?? "create";
                
                // Obj tipini belirle (roof, facade, vb.)
                string objectType = DetectObjectType(json);
                
                ParametricObject paramObj = null;
                
                switch (objectType)
                {
                    case "roof":
                        paramObj = new RoofParameters();
                        paramObj.FromJson(json);
                        break;
                        
                    case "facade":
                        paramObj = new FacadeParameters();
                        paramObj.FromJson(json);
                        break;
                        
                    case "furniture":
                        paramObj = new FurnitureParameters();
                        paramObj.FromJson(json);
                        break;
                        
                    case "interior":
                        paramObj = new InteriorParameters();
                        paramObj.FromJson(json);
                        break;
                        
                    default:
                        return new ParseResult
                        {
                            Success = false,
                            ErrorMessage = $"Desteklenmeyen obje tipi: {objectType}"
                        };
                }
                
                // Validasyon
                if (!paramObj.Validate(out string error))
                {
                    return new ParseResult
                    {
                        Success = false,
                        ErrorMessage = $"Validasyon hatası: {error}"
                    };
                }
                
                // Metadata
                paramObj.SourceCommand = originalCommand;
                paramObj.UpdatedAt = DateTime.Now;
                
                return new ParseResult
                {
                    Success = true,
                    Action = action,
                    ObjectType = objectType,
                    Parameters = paramObj
                };
            }
            catch (Exception ex)
            {
                return new ParseResult
                {
                    Success = false,
                    ErrorMessage = $"Parse hatası: {ex.Message}"
                };
            }
        }
        
        /// <summary>
        /// Güncelleme komutunu parse et (sadece parametre değişikliği)
        /// </summary>
        public ParseResult ParseUpdateCommand(JObject json, ParametricObject existingObject)
        {
            try
            {
                var parameters = json["parameters"] as JObject;
                if (parameters == null)
                {
                    return new ParseResult
                    {
                        Success = false,
                        ErrorMessage = "Güncelleme için 'parameters' alanı gerekli"
                    };
                }
                
                // Mevcut objeyi klonla (undo için)
                var updatedObject = existingObject; // Basit implementasyon
                
                // Her parametreyi güncelle
                foreach (var prop in parameters.Properties())
                {
                    updatedObject.UpdateParameter(prop.Name, prop.Value.ToObject<object>());
                }
                
                // Validasyon
                if (!updatedObject.Validate(out string error))
                {
                    return new ParseResult
                    {
                        Success = false,
                        ErrorMessage = $"Güncelleme validasyon hatası: {error}"
                    };
                }
                
                return new ParseResult
                {
                    Success = true,
                    Action = "update",
                    ObjectType = updatedObject.ObjectType,
                    Parameters = updatedObject
                };
            }
            catch (Exception ex)
            {
                return new ParseResult
                {
                    Success = false,
                    ErrorMessage = $"Güncelleme parse hatası: {ex.Message}"
                };
            }
        }
        
        /// <summary>
        /// Hata/Clarification yanıtını işle
        /// </summary>
        public ParseResult ParseClarification(JObject json)
        {
            string message = json["message"]?.ToString() ?? "Daha fazla bilgiye ihtiyacım var";
            
            return new ParseResult
            {
                Success = true,
                Action = "clarify",
                ErrorMessage = message,
                IsClarification = true
            };
        }
        
        /// <summary>
        /// JSON'dan obje tipini tespit et
        /// </summary>
        private string DetectObjectType(JObject json)
        {
            // Direkt object_type varsa kullan
            string explicitType = json["object_type"]?.ToString()?.ToLower();
            if (!string.IsNullOrEmpty(explicitType))
                return explicitType;
            
            // roof_type varsa => roof
            if (json["roof_type"] != null)
                return "roof";
            
            // pattern/panels varsa => facade
            if (json["pattern"] != null || json["panels_h"] != null)
                return "facade";
            
            // furniture-specific fields
            if (json["furniture_type"] != null || json["seat_count"] != null)
                return "furniture";
            
            // interior-specific fields
            if (json["room_type"] != null || json["wall_count"] != null)
                return "interior";
            
            // Varsayılan
            return "roof";
        }
        
        /// <summary>
        /// Auto-fix common JSON errors
        /// </summary>
        public string AutoFixJson(string rawContent)
        {
            string fixedJson = rawContent;
            
            // Trailing commas
            fixedJson = System.Text.RegularExpressions.Regex.Replace(fixedJson, ",\\s*}", "}");
            fixedJson = System.Text.RegularExpressions.Regex.Replace(fixedJson, ",\\s*]", "]");
            
            // Single quotes to double quotes
            fixedJson = fixedJson.Replace("'", "\"");
            
            // Fix Turkish characters in property names (normalize)
            fixedJson = fixedJson.Replace("\"egim\"", "\"pitch_angle\"");
            fixedJson = fixedJson.Replace("\"sacak\"", "\"eave_overhang\"");
            fixedJson = fixedJson.Replace("\"boyut\"", "\"size\"");
            fixedJson = fixedJson.Replace("\"genislik\"", "\"width\"");
            fixedJson = fixedJson.Replace("\"uzunluk\"", "\"length\"");
            
            return fixedJson;
        }
    }
    
    /// <summary>
    /// Parse sonucu
    /// </summary>
    public class ParseResult
    {
        public bool Success { get; set; }
        public string Action { get; set; } // create, update, analyze_light, optimize_skylights, clarify
        public string ObjectType { get; set; }
        public ParametricObject Parameters { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsClarification { get; set; }
        
        public bool ShouldCreateGeometry => Success && Action == "create";
        public bool ShouldUpdateGeometry => Success && Action == "update";
    }
}
