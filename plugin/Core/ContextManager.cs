using System;
using System.Collections.Generic;
using System.Linq;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace RoofAI.Core
{
    /// <summary>
    /// Rhino viewport context'ini yönetir (seçili objeler, vb.)
    /// </summary>
    public class ContextManager
    {
        /// <summary>
        /// Seçili objeleri tespit et
        /// </summary>
        public List<SelectedObjectInfo> GetSelectedObjects()
        {
            var selected = new List<SelectedObjectInfo>();
            
            var rhinoDoc = RhinoDoc.ActiveDoc;
            if (rhinoDoc == null) return selected;
            
            var objectEnumerator = rhinoDoc.Objects.GetSelectedObjects(false, false);
            
            foreach (var obj in objectEnumerator)
            {
                selected.Add(new SelectedObjectInfo
                {
                    Id = obj.Id,
                    ObjectType = GetObjectType(obj),
                    Geometry = obj.Geometry,
                    Name = obj.Name ?? "Unnamed",
                    UserData = GetUserData(obj)
                });
            }
            
            return selected;
        }
        
        /// <summary>
        /// Seçili obje var mı?
        /// </summary>
        public bool HasSelection()
        {
            var rhinoDoc = RhinoDoc.ActiveDoc;
            return rhinoDoc != null && rhinoDoc.Objects.GetSelectedObjects(false, false).Any();
        }
        
        /// <summary>
        /// Komut tipini belirle (create vs update)
        /// </summary>
        public CommandType DetermineCommandType(string userMessage, List<SelectedObjectInfo> selectedObjects)
        {
            var lowerMessage = userMessage.ToLower();
            
            // Update anahtar kelimeleri
            var updateKeywords = new[] { "değiştir", "güncelle", "arttır", "azalt", "büyüt", "küçült", 
                                         "update", "change", "modify", "increase", "decrease" };
            
            // Create anahtar kelimeleri
            var createKeywords = new[] { "yap", "oluştur", "çiz", "create", "make", "draw", "generate" };
            
            // Önce seçim kontrolü
            if (selectedObjects.Any() && updateKeywords.Any(k => lowerMessage.Contains(k)))
            {
                return CommandType.Update;
            }
            
            // Açık create komutu
            if (createKeywords.Any(k => lowerMessage.Contains(k)))
            {
                return CommandType.Create;
            }
            
            // Varsayılan: seçili obje varsa update, yoksa create
            return selectedObjects.Any() ? CommandType.Update : CommandType.Create;
        }
        
        /// <summary>
        /// Obje tipini tespit et ( RoofAI objesi mi? )
        /// </summary>
        public bool IsRoofAIObject(Guid objectId)
        {
            var rhinoDoc = RhinoDoc.ActiveDoc;
            if (rhinoDoc == null) return false;
            
            var obj = rhinoDoc.Objects.FindId(objectId);
            if (obj == null) return false;
            
            // User data kontrolü
            var userData = obj.Attributes.UserData.Find(typeof(RoofAIObjectData)) as RoofAIObjectData;
            return userData != null;
        }
        
        /// <summary>
        /// Objeye metadata ekle
        /// </summary>
        public void AttachMetadata(Guid objectId, string objectType, string parameters)
        {
            var rhinoDoc = RhinoDoc.ActiveDoc;
            if (rhinoDoc == null) return;
            
            var obj = rhinoDoc.Objects.FindId(objectId);
            if (obj == null) return;
            
            var metadata = new RoofAIObjectData
            {
                ObjectType = objectType,
                Parameters = parameters,
                CreatedAt = DateTime.Now
            };
            
            obj.Attributes.UserData.Add(metadata);
            obj.CommitChanges();
        }
        
        /// <summary>
        /// Objeden metadata al
        /// </summary>
        public RoofAIObjectData GetMetadata(Guid objectId)
        {
            var rhinoDoc = RhinoDoc.ActiveDoc;
            if (rhinoDoc == null) return null;
            
            var obj = rhinoDoc.Objects.FindId(objectId);
            if (obj == null) return null;
            
            return obj.Attributes.UserData.Find(typeof(RoofAIObjectData)) as RoofAIObjectData;
        }
        
        private string GetObjectType(RhinoObject obj)
        {
            if (obj.Geometry is Brep) return "Brep";
            if (obj.Geometry is Mesh) return "Mesh";
            if (obj.Geometry is Curve) return "Curve";
            if (obj.Geometry is Surface) return "Surface";
            return "Unknown";
        }
        
        private string GetUserData(RhinoObject obj)
        {
            var metadata = obj.Attributes.UserData.Find(typeof(RoofAIObjectData)) as RoofAIObjectData;
            return metadata?.ToJson() ?? "{}";
        }
    }
    
    /// <summary>
    /// Seçili obje bilgisi
    /// </summary>
    public class SelectedObjectInfo
    {
        public Guid Id { get; set; }
        public string ObjectType { get; set; }
        public GeometryBase Geometry { get; set; }
        public string Name { get; set; }
        public string UserData { get; set; }
    }
    
    /// <summary>
    /// Komut tipi
    /// </summary>
    public enum CommandType
    {
        Create,
        Update,
        Delete,
        Analyze,
        Unknown
    }
    
    /// <summary>
    /// RoofAI obje metadata'sı
    /// </summary>
    [Serializable]
    public class RoofAIObjectData : Rhino.Collections.ArchivableDictionary
    {
        public string ObjectType { get; set; }
        public string Parameters { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        public string ToJson()
        {
            return $"{{\"type\":\"{ObjectType}\",\"params\":{Parameters},\"created\":\"{CreatedAt}\"}}";
        }
    }
}
