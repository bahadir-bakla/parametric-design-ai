using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RoofAI.Models
{
    /// <summary>
    /// Tüm parametrik objelerin base sınıfı
    /// </summary>
    public abstract class ParametricObject
    {
        public Guid Id { get; set; }
        public string ObjectType { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        /// <summary>
        /// Orijinal AI komutu
        /// </summary>
        public string SourceCommand { get; set; }
        
        /// <summary>
        /// Rhino/Grasshopper obje referansları
        /// </summary>
        public List<Guid> GeometryIds { get; set; } = new List<Guid>();
        
        /// <summary>
        /// JSON'dan parametreleri parse et
        /// </summary>
        public abstract void FromJson(JObject json);
        
        /// <summary>
        /// Parametreleri JSON'a çevir
        /// </summary>
        public abstract JObject ToJson();
        
        /// <summary>
        /// Validasyon
        /// </summary>
        public abstract bool Validate(out string errorMessage);
        
        /// <summary>
        /// Parametre değerini güncelle
        /// </summary>
        public abstract void UpdateParameter(string paramName, object value);
    }
    
    /// <summary>
    /// Çatı parametreleri
    /// </summary>
    public class RoofParameters : ParametricObject
    {
        public string RoofType { get; set; } // gable, hip, gambrel, shed, flat
        public double Length { get; set; }
        public double Width { get; set; }
        public double PitchAngle { get; set; }
        public double EaveOverhang { get; set; }
        public double RidgeHeight { get; set; }
        public double Orientation { get; set; }
        public string Material { get; set; }
        
        public RoofParameters()
        {
            ObjectType = "roof";
            CreatedAt = DateTime.Now;
            Id = Guid.NewGuid();
        }
        
        public override void FromJson(JObject json)
        {
            RoofType = json["roof_type"]?.ToString() ?? "gable";
            Length = json["length"]?.Value<double>() ?? 10.0;
            Width = json["width"]?.Value<double>() ?? 8.0;
            PitchAngle = json["pitch_angle"]?.Value<double>() ?? 30.0;
            EaveOverhang = json["eave_overhang"]?.Value<double>() ?? 0.5;
            
            if (json["ridge_height"]?.ToString() == "auto")
                RidgeHeight = CalculateAutoRidgeHeight();
            else
                RidgeHeight = json["ridge_height"]?.Value<double>() ?? CalculateAutoRidgeHeight();
                
            Orientation = json["orientation"]?.Value<double>() ?? 0.0;
            Material = json["material"]?.ToString() ?? "kiremit";
            
            UpdatedAt = DateTime.Now;
        }
        
        public override JObject ToJson()
        {
            return new JObject
            {
                ["action"] = "create",
                ["roof_type"] = RoofType,
                ["length"] = Length,
                ["width"] = Width,
                ["pitch_angle"] = PitchAngle,
                ["eave_overhang"] = EaveOverhang,
                ["ridge_height"] = RidgeHeight,
                ["orientation"] = Orientation,
                ["material"] = Material
            };
        }
        
        public override bool Validate(out string errorMessage)
        {
            errorMessage = "";
            
            if (Length <= 0 || Length > 100)
                errorMessage = "Length must be between 0.1 and 100m";
            else if (Width <= 0 || Width > 100)
                errorMessage = "Width must be between 0.1 and 100m";
            else if (PitchAngle < 0 || PitchAngle > 60)
                errorMessage = "Pitch angle must be between 0 and 60 degrees";
            else if (EaveOverhang < 0 || EaveOverhang > 3)
                errorMessage = "Eave overhang must be between 0 and 3m";
            
            return string.IsNullOrEmpty(errorMessage);
        }
        
        public override void UpdateParameter(string paramName, object value)
        {
            switch (paramName.ToLower())
            {
                case "length":
                    Length = Convert.ToDouble(value);
                    break;
                case "width":
                    Width = Convert.ToDouble(value);
                    break;
                case "pitch_angle":
                case "egim":
                    PitchAngle = Convert.ToDouble(value);
                    break;
                case "eave_overhang":
                case "sacak":
                    EaveOverhang = Convert.ToDouble(value);
                    break;
                case "ridge_height":
                case "mahya_yuksekligi":
                    RidgeHeight = Convert.ToDouble(value);
                    break;
                case "orientation":
                case "yon":
                    Orientation = Convert.ToDouble(value);
                    break;
                case "material":
                    Material = value.ToString();
                    break;
            }
            UpdatedAt = DateTime.Now;
        }
        
        private double CalculateAutoRidgeHeight()
        {
            // Basit hesaplama: width/2 * tan(pitch)
            double pitchRad = PitchAngle * Math.PI / 180.0;
            return (Width / 2.0) * Math.Tan(pitchRad);
        }
    }
    
    /// <summary>
    /// Cephe parametreleri
    /// </summary>
    public class FacadeParameters : ParametricObject
    {
        public double Height { get; set; }
        public double Width { get; set; }
        public string PatternType { get; set; } // grid, diamond, random
        public int PanelCountHorizontal { get; set; }
        public int PanelCountVertical { get; set; }
        public double WindowRatio { get; set; }
        
        public FacadeParameters()
        {
            ObjectType = "facade";
            CreatedAt = DateTime.Now;
            Id = Guid.NewGuid();
        }
        
        public override void FromJson(JObject json)
        {
            Height = json["height"]?.Value<double>() ?? 3.0;
            Width = json["width"]?.Value<double>() ?? 10.0;
            PatternType = json["pattern"]?.ToString() ?? "grid";
            PanelCountHorizontal = json["panels_h"]?.Value<int>() ?? 5;
            PanelCountVertical = json["panels_v"]?.Value<int>() ?? 3;
            WindowRatio = json["window_ratio"]?.Value<double>() ?? 0.3;
            UpdatedAt = DateTime.Now;
        }
        
        public override JObject ToJson()
        {
            return new JObject
            {
                ["action"] = "create",
                ["object_type"] = "facade",
                ["height"] = Height,
                ["width"] = Width,
                ["pattern"] = PatternType,
                ["panels_h"] = PanelCountHorizontal,
                ["panels_v"] = PanelCountVertical,
                ["window_ratio"] = WindowRatio
            };
        }
        
        public override bool Validate(out string errorMessage)
        {
            errorMessage = "";
            if (Height <= 0) errorMessage = "Height must be positive";
            else if (Width <= 0) errorMessage = "Width must be positive";
            else if (WindowRatio < 0 || WindowRatio > 1) errorMessage = "Window ratio must be between 0 and 1";
            return string.IsNullOrEmpty(errorMessage);
        }
        
        public override void UpdateParameter(string paramName, object value)
        {
            // Implementation...
            UpdatedAt = DateTime.Now;
        }
    }
}
