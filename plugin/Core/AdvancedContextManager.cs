using System;
using System.Collections.Generic;
using System.Linq;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace RoofAI.Core
{
    /// <summary>
    /// Gelişmiş context yönetimi - seçili objeleri daha akıllı analiz eder
    /// </summary>
    public class AdvancedContextManager : ContextManager
    {
        /// <summary>
        /// Seçili objelerin türünü analiz et
        /// </summary>
        public SelectionAnalysis AnalyzeSelection()
        {
            var selected = GetSelectedObjects();
            var analysis = new SelectionAnalysis
            {
                SelectedObjects = selected,
                Count = selected.Count,
                HasSelection = selected.Count > 0
            };
            
            if (!analysis.HasSelection)
                return analysis;
            
            // Objelerin türlerini grupla
            var typeGroups = selected.GroupBy(s => s.ObjectType).ToList();
            analysis.DominantType = typeGroups.OrderByDescending(g => g.Count()).First().Key;
            analysis.TypeDistribution = typeGroups.ToDictionary(g => g.Key, g => g.Count());
            
            // Homojen seçim mi? (hepsi aynı türden)
            analysis.IsHomogeneousSelection = typeGroups.Count == 1;
            
            // Çoklu seçim mi?
            analysis.IsMultipleSelection = selected.Count > 1;
            
            // Bounding box hesapla
            if (selected.Count > 0 && selected[0].Geometry != null)
            {
                var bbox = selected[0].Geometry.GetBoundingBox(true);
                for (int i = 1; i < selected.Count; i++)
                {
                    if (selected[i].Geometry != null)
                    {
                        bbox = BoundingBox.Union(bbox, selected[i].Geometry.GetBoundingBox(true));
                    }
                }
                analysis.BoundingBox = bbox;
                analysis.TotalArea = bbox.Area;
                analysis.CenterPoint = bbox.Center;
            }
            
            // RoofAI objelerini tespit et
            analysis.RoofAIObjectIds = selected
                .Where(s => IsRoofAIObject(s.Id))
                .Select(s => s.Id)
                .ToList();
            
            analysis.HasRoofAIObjects = analysis.RoofAIObjectIds.Count > 0;
            
            return analysis;
        }
        
        /// <summary>
        /// Komutun bağlamını belirle (daha akıllı)
        /// </summary>
        public CommandContext DetermineCommandContext(string userMessage, SelectionAnalysis analysis)
        {
            var context = new CommandContext();
            var lowerMessage = userMessage.ToLower();
            
            // 1. Aksiyon tipini belirle
            if (IsUpdateCommand(lowerMessage))
            {
                context.Action = CommandType.Update;
            }
            else if (IsDeleteCommand(lowerMessage))
            {
                context.Action = CommandType.Delete;
            }
            else if (IsAnalyzeCommand(lowerMessage))
            {
                context.Action = CommandType.Analyze;
            }
            else
            {
                context.Action = CommandType.Create;
            }
            
            // 2. Hedef obje tipini belirle
            context.TargetType = DetermineTargetType(lowerMessage, analysis);
            
            // 3. Mod belirle
            if (analysis.HasSelection && context.Action == CommandType.Update)
            {
                // Seçili obje var ve güncelleme isteniyor
                if (analysis.IsHomogeneousSelection && analysis.HasRoofAIObjects)
                {
                    context.Mode = ContextMode.UpdateExisting;
                    context.TargetObjectIds = analysis.RoofAIObjectIds;
                }
                else if (analysis.HasRoofAIObjects)
                {
                    // Karışık seçim ama içinde RoofAI objesi var
                    context.Mode = ContextMode.UpdateMixed;
                    context.TargetObjectIds = analysis.RoofAIObjectIds;
                }
                else
                {
                    // Seçili obje var ama RoofAI objesi değil
                    context.Mode = ContextMode.CreateNew; // Yeni obje oluştur
                }
            }
            else if (analysis.HasSelection && context.Action == CommandType.Create)
            {
                // Seçili obje var ama create deniyor - muhtemelen yeni obje
                context.Mode = ContextMode.CreateNearSelection;
                context.ReferencePoint = analysis.CenterPoint;
            }
            else
            {
                // Seçim yok
                context.Mode = ContextMode.CreateNew;
            }
            
            // 4. Parametre çıkarımı
            context.InferredParameters = InferParameters(lowerMessage, analysis);
            
            return context;
        }
        
        /// <summary>
        /// Mesajdan parametre çıkar
        /// </summary>
        private Dictionary<string, object> InferParameters(string message, SelectionAnalysis analysis)
        {
            var parameters = new Dictionary<string, object>();
            var lowerMessage = message.ToLower();
            
            // "büyüt", "küçült" gibi komutlardan oran çıkar
            if (lowerMessage.Contains("büyüt") || lowerMessage.Contains("büyüt"))
            {
                double ratio = 1.2; // Default %20 büyüt
                
                // Yüzde varsa
                if (message.Contains("%"))
                {
                    var percentMatch = System.Text.RegularExpressions.Regex.Match(message, @"(\d+)%");
                    if (percentMatch.Success)
                    {
                        ratio = 1.0 + (double.Parse(percentMatch.Groups[1].Value) / 100.0);
                    }
                }
                else if (System.Text.RegularExpressions.Regex.IsMatch(message, @"\d+"))
                {
                    // Direkt sayı varsa (metre vb.)
                    var numberMatch = System.Text.RegularExpressions.Regex.Match(message, @"(\d+\.?\d*)");
                    if (numberMatch.Success)
                    {
                        parameters["absolute_size"] = double.Parse(numberMatch.Groups[1].Value);
                    }
                }
                
                parameters["scale_ratio"] = ratio;
            }
            else if (lowerMessage.Contains("küçült") || lowerMessage.Contains("azalt"))
            {
                double ratio = 0.8; // Default %20 küçült
                
                if (message.Contains("%"))
                {
                    var percentMatch = System.Text.RegularExpressions.Regex.Match(message, @"(\d+)%");
                    if (percentMatch.Success)
                    {
                        ratio = 1.0 - (double.Parse(percentMatch.Groups[1].Value) / 100.0);
                    }
                }
                
                parameters["scale_ratio"] = ratio;
            }
            
            // Yön belirleme
            if (lowerMessage.Contains("sağa"))
                parameters["direction"] = "right";
            else if (lowerMessage.Contains("sola"))
                parameters["direction"] = "left";
            else if (lowerMessage.Contains("yukarı"))
                parameters["direction"] = "up";
            else if (lowerMessage.Contains("aşağı"))
                parameters["direction"] = "down";
            
            return parameters;
        }
        
        private bool IsUpdateCommand(string message)
        {
            var keywords = new[] { "değiştir", "güncelle", "arttır", "azalt", "büyüt", "küçült", 
                                 "update", "change", "modify", "increase", "decrease", "scale", 
                                 "rotate", "move", "translate" };
            return keywords.Any(k => message.Contains(k));
        }
        
        private bool IsDeleteCommand(string message)
        {
            var keywords = new[] { "sil", "kaldır", "delete", "remove", "clear" };
            return keywords.Any(k => message.Contains(k));
        }
        
        private bool IsAnalyzeCommand(string message)
        {
            var keywords = new[] { "analiz", "hesapla", "ölç", "analyze", "calculate", "measure" };
            return keywords.Any(k => message.Contains(k));
        }
        
        private string DetermineTargetType(string message, SelectionAnalysis analysis)
        {
            // Mesajdan tip çıkar
            if (message.Contains("çatı") || message.Contains("roof"))
                return "roof";
            if (message.Contains("cephe") || message.Contains("facade"))
                return "facade";
            if (message.Contains("mobilya") || message.Contains("furniture"))
                return "furniture";
            if (message.Contains("oda") || message.Contains("room") || message.Contains("interior"))
                return "interior";
            
            // Seçimden tip çıkar
            if (analysis.IsHomogeneousSelection)
                return analysis.DominantType.ToLower();
            
            return "roof"; // Default
        }
    }
    
    /// <summary>
    /// Seçim analizi sonucu
    /// </summary>
    public class SelectionAnalysis
    {
        public List<SelectedObjectInfo> SelectedObjects { get; set; }
        public int Count { get; set; }
        public bool HasSelection { get; set; }
        public bool IsMultipleSelection { get; set; }
        public bool IsHomogeneousSelection { get; set; }
        public string DominantType { get; set; }
        public Dictionary<string, int> TypeDistribution { get; set; }
        public BoundingBox BoundingBox { get; set; }
        public double TotalArea { get; set; }
        public Point3d CenterPoint { get; set; }
        public List<Guid> RoofAIObjectIds { get; set; }
        public bool HasRoofAIObjects { get; set; }
    }
    
    /// <summary>
    /// Komut bağlamı
    /// </summary>
    public class CommandContext
    {
        public CommandType Action { get; set; }
        public string TargetType { get; set; }
        public ContextMode Mode { get; set; }
        public List<Guid> TargetObjectIds { get; set; } = new List<Guid>();
        public Point3d ReferencePoint { get; set; }
        public Dictionary<string, object> InferredParameters { get; set; } = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Context modu
    /// </summary>
    public enum ContextMode
    {
        CreateNew,          // Sıfırdan yeni obje
        CreateNearSelection, // Seçimin yanına yeni obje
        UpdateExisting,     // Mevcut RoofAI objesini güncelle
        UpdateMixed,        // Karışık seçimden RoofAI objelerini güncelle
        CloneAndModify      // Kopyala ve değiştir
    }
}
