using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace RoofAI.Core
{
    /// <summary>
    /// Konuşma geçmişini ve durumunu yönetir
    /// </summary>
    public class ConversationManager
    {
        private readonly List<ConversationMessage> _messages;
        private readonly int _maxHistorySize;
        
        public ConversationManager(int maxHistorySize = 5)
        {
            _messages = new List<ConversationMessage>();
            _maxHistorySize = maxHistorySize;
        }
        
        /// <summary>
        /// Yeni mesaj ekle
        /// </summary>
        public void AddMessage(MessageType type, string content, string command = null, 
                              bool success = true, string errorMessage = null)
        {
            var message = new ConversationMessage
            {
                Id = Guid.NewGuid(),
                Type = type,
                Content = content,
                Command = command,
                Timestamp = DateTime.Now,
                Success = success,
                ErrorMessage = errorMessage
            };
            
            _messages.Add(message);
            
            // History limitini koru
            if (_messages.Count > _maxHistorySize)
            {
                _messages.RemoveAt(0);
            }
        }
        
        /// <summary>
        /// Son mesajları getir (API için)
        /// </summary>
        public List<ConversationMessage> GetRecentMessages(int count)
        {
            return _messages.TakeLast(count).ToList();
        }
        
        /// <summary>
        /// Tüm mesajları getir (UI için)
        /// </summary>
        public IReadOnlyList<ConversationMessage> GetAllMessages()
        {
            return _messages.AsReadOnly();
        }
        
        /// <summary>
        /// Son kullanıcı mesajını getir
        /// </summary>
        public ConversationMessage GetLastUserMessage()
        {
            return _messages.LastOrDefault(m => m.Type == MessageType.User);
        }
        
        /// <summary>
        /// Son AI yanıtını getir
        /// </summary>
        public ConversationMessage GetLastAIResponse()
        {
            return _messages.LastOrDefault(m => m.Type == MessageType.AI);
        }
        
        /// <summary>
        /// Geçmişi temizle
        /// </summary>
        public void Clear()
        {
            _messages.Clear();
        }
        
        /// <summary>
        /// Belirli bir mesajı sil
        /// </summary>
        public bool RemoveMessage(Guid messageId)
        {
            var message = _messages.FirstOrDefault(m => m.Id == messageId);
            if (message != null)
            {
                return _messages.Remove(message);
            }
            return false;
        }
        
        /// <summary>
        /// Konuşmayı JSON olarak dışa aktar
        /// </summary>
        public string ExportToJson()
        {
            return JsonConvert.SerializeObject(_messages, Formatting.Indented);
        }
        
        /// <summary>
        /// JSON'dan konuşma yükle
        /// </summary>
        public void ImportFromJson(string json)
        {
            var messages = JsonConvert.DeserializeObject<List<ConversationMessage>>(json);
            if (messages != null)
            {
                _messages.Clear();
                _messages.AddRange(messages.TakeLast(_maxHistorySize));
            }
        }
        
        /// <summary>
        /// Belirli bir obje ile ilgili mesajları bul
        /// </summary>
        public List<ConversationMessage> GetMessagesForObject(Guid objectId)
        {
            return _messages.Where(m => m.ObjectId == objectId).ToList();
        }
        
        /// <summary>
        /// Undo için son komutu al
        /// </summary>
        public ConversationMessage GetLastCommand()
        {
            return _messages.LastOrDefault(m => !string.IsNullOrEmpty(m.Command));
        }
        
        /// <summary>
        /// Mesaj sayısı
        /// </summary>
        public int Count => _messages.Count;
        
        /// <summary>
        /// Boş mu?
        /// </summary>
        public bool IsEmpty => _messages.Count == 0;
        
        /// <summary>
        /// Event: Yeni mesaj eklendiğinde
        /// </summary>
        public event EventHandler<ConversationMessage> MessageAdded;
        
        /// <summary>
        /// Event: Geçmiş temizlendiğinde
        /// </summary>
        public event EventHandler HistoryCleared;
        
        private void OnMessageAdded(ConversationMessage message)
        {
            MessageAdded?.Invoke(this, message);
        }
    }
    
    /// <summary>
    /// Konuşma mesajı
    /// </summary>
    public class ConversationMessage
    {
        public Guid Id { get; set; }
        public MessageType Type { get; set; }
        public string Content { get; set; }
        public string Command { get; set; } // Orijinal komut
        public DateTime Timestamp { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public Guid? ObjectId { get; set; } // İlgili Rhino objesi
        public string ObjectType { get; set; } // roof, facade, vb.
        
        public bool IsUser => Type == MessageType.User;
        public bool IsAI => Type == MessageType.AI;
        public bool IsSystem => Type == MessageType.System;
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
        
        public string DisplayText
        {
            get
            {
                if (Type == MessageType.User)
                    return $"👤 {Content}";
                else if (Type == MessageType.AI)
                    return $"🤖 {Content}";
                else
                    return $"ℹ️ {Content}";
            }
        }
    }
    
    public enum MessageType
    {
        User,
        AI,
        System,
        Error
    }
}
