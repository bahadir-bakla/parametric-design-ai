using System;
using System.Collections.Generic;
using Rhino.Geometry;

namespace RoofAI.Models
{
    /// <summary>
    /// Mobilya parametreleri
    /// </summary>
    public class FurnitureParameters : ParametricObject
    {
        public string FurnitureType { get; set; } // table, chair, shelf, sofa, bed
        public double Length { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public string Material { get; set; }
        public string Style { get; set; } // modern, classic, minimalist
        public int SeatCount { get; set; } // For chairs/sofas
        public int ShelfCount { get; set; } // For shelves
        public bool HasBackrest { get; set; }
        public bool HasArmrest { get; set; }
        
        public FurnitureParameters()
        {
            ObjectType = "furniture";
            CreatedAt = DateTime.Now;
            Id = Guid.NewGuid();
            Material = "wood";
            Style = "modern";
        }
        
        public override void FromJson(Newtonsoft.Json.Linq.JObject json)
        {
            FurnitureType = json["furniture_type"]?.ToString() ?? "table";
            Length = json["length"]?.Value<double>() ?? 1.2;
            Width = json["width"]?.Value<double>() ?? 0.8;
            Height = json["height"]?.Value<double>() ?? 0.75;
            Material = json["material"]?.ToString() ?? "wood";
            Style = json["style"]?.ToString() ?? "modern";
            SeatCount = json["seat_count"]?.Value<int>() ?? 1;
            ShelfCount = json["shelf_count"]?.Value<int>() ?? 3;
            HasBackrest = json["has_backrest"]?.Value<bool>() ?? true;
            HasArmrest = json["has_armrest"]?.Value<bool>() ?? false;
            
            UpdatedAt = DateTime.Now;
        }
        
        public override Newtonsoft.Json.Linq.JObject ToJson()
        {
            return new Newtonsoft.Json.Linq.JObject
            {
                ["action"] = "create",
                ["object_type"] = "furniture",
                ["furniture_type"] = FurnitureType,
                ["length"] = Length,
                ["width"] = Width,
                ["height"] = Height,
                ["material"] = Material,
                ["style"] = Style,
                ["seat_count"] = SeatCount,
                ["shelf_count"] = ShelfCount,
                ["has_backrest"] = HasBackrest,
                ["has_armrest"] = HasArmrest
            };
        }
        
        public override bool Validate(out string errorMessage)
        {
            errorMessage = "";
            if (Length <= 0) errorMessage = "Length must be positive";
            else if (Width <= 0) errorMessage = "Width must be positive";
            else if (Height <= 0) errorMessage = "Height must be positive";
            return string.IsNullOrEmpty(errorMessage);
        }
        
        public override void UpdateParameter(string paramName, object value)
        {
            switch (paramName.ToLower())
            {
                case "length":
                case "uzunluk":
                    Length = Convert.ToDouble(value);
                    break;
                case "width":
                case "genislik":
                    Width = Convert.ToDouble(value);
                    break;
                case "height":
                case "yukseklik":
                    Height = Convert.ToDouble(value);
                    break;
                case "material":
                case "malzeme":
                    Material = value.ToString();
                    break;
                case "style":
                case "stil":
                    Style = value.ToString();
                    break;
            }
            UpdatedAt = DateTime.Now;
        }
    }
    
    /// <summary>
    /// İç mekan parametreleri
    /// </summary>
    public class InteriorParameters : ParametricObject
    {
        public string RoomType { get; set; } // living_room, bedroom, kitchen, office
        public double RoomLength { get; set; }
        public double RoomWidth { get; set; }
        public double RoomHeight { get; set; }
        public int WallCount { get; set; }
        public List<DoorInfo> Doors { get; set; } = new List<DoorInfo>();
        public List<WindowInfo> Windows { get; set; } = new List<WindowInfo>();
        public bool CreateFloor { get; set; }
        public bool CreateCeiling { get; set; }
        
        public InteriorParameters()
        {
            ObjectType = "interior";
            CreatedAt = DateTime.Now;
            Id = Guid.NewGuid();
            RoomHeight = 2.8;
            WallCount = 4;
            CreateFloor = true;
            CreateCeiling = false;
        }
        
        public override void FromJson(Newtonsoft.Json.Linq.JObject json)
        {
            RoomType = json["room_type"]?.ToString() ?? "living_room";
            RoomLength = json["room_length"]?.Value<double>() ?? 5.0;
            RoomWidth = json["room_width"]?.Value<double>() ?? 4.0;
            RoomHeight = json["room_height"]?.Value<double>() ?? 2.8;
            WallCount = json["wall_count"]?.Value<int>() ?? 4;
            CreateFloor = json["create_floor"]?.Value<bool>() ?? true;
            CreateCeiling = json["create_ceiling"]?.Value<bool>() ?? false;
            
            // Parse doors and windows if present
            var doorsArray = json["doors"] as Newtonsoft.Json.Linq.JArray;
            if (doorsArray != null)
            {
                foreach (var door in doorsArray)
                {
                    Doors.Add(new DoorInfo
                    {
                        Width = door["width"]?.Value<double>() ?? 0.9,
                        Height = door["height"]?.Value<double>() ?? 2.1,
                        Position = door["position"]?.Value<double>() ?? 0.5,
                        WallIndex = door["wall_index"]?.Value<int>() ?? 0
                    });
                }
            }
            
            UpdatedAt = DateTime.Now;
        }
        
        public override Newtonsoft.Json.Linq.JObject ToJson()
        {
            var json = new Newtonsoft.Json.Linq.JObject
            {
                ["action"] = "create",
                ["object_type"] = "interior",
                ["room_type"] = RoomType,
                ["room_length"] = RoomLength,
                ["room_width"] = RoomWidth,
                ["room_height"] = RoomHeight,
                ["wall_count"] = WallCount,
                ["create_floor"] = CreateFloor,
                ["create_ceiling"] = CreateCeiling
            };
            
            return json;
        }
        
        public override bool Validate(out string errorMessage)
        {
            errorMessage = "";
            if (RoomLength <= 0) errorMessage = "Room length must be positive";
            else if (RoomWidth <= 0) errorMessage = "Room width must be positive";
            else if (RoomHeight <= 0) errorMessage = "Room height must be positive";
            else if (WallCount < 3 || WallCount > 8) errorMessage = "Wall count must be between 3 and 8";
            return string.IsNullOrEmpty(errorMessage);
        }
        
        public override void UpdateParameter(string paramName, object value)
        {
            switch (paramName.ToLower())
            {
                case "room_length":
                case "oda_uzunluk":
                    RoomLength = Convert.ToDouble(value);
                    break;
                case "room_width":
                case "oda_genislik":
                    RoomWidth = Convert.ToDouble(value);
                    break;
                case "room_height":
                case "oda_yukseklik":
                    RoomHeight = Convert.ToDouble(value);
                    break;
            }
            UpdatedAt = DateTime.Now;
        }
    }
    
    public class DoorInfo
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public double Position { get; set; } // 0-1 along wall
        public int WallIndex { get; set; }
    }
    
    public class WindowInfo
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public double Position { get; set; } // 0-1 along wall
        public double SillHeight { get; set; }
        public int WallIndex { get; set; }
    }
}
