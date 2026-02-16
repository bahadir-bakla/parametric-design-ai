using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RoofAI.API
{
    /// <summary>
    /// Ollama API ile iletişim kuran client
    /// </summary>
    public class OllamaClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _modelName;
        private readonly int _timeoutSeconds;
        
        public OllamaClient(string baseUrl = "http://localhost:11434", 
                           string modelName = "roof-ai",
                           int timeoutSeconds = 30)
        {
            _baseUrl = baseUrl;
            _modelName = modelName;
            _timeoutSeconds = timeoutSeconds;
            
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(timeoutSeconds)
            };
        }
        
        /// <summary>
        /// AI'a mesaj gönder ve yanıt al
        /// </summary>
        public async Task<OllamaResponse> SendMessageAsync(string userMessage, 
                                                          ConversationHistory history = null)
        {
            try
            {
                var requestBody = new
                {
                    model = _modelName,
                    messages = BuildMessages(userMessage, history),
                    stream = false,
                    options = new
                    {
                        temperature = 0.7,
                        top_p = 0.9
                    }
                };
                
                var json = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/chat", content);
                response.EnsureSuccessStatusCode();
                
                var responseJson = await response.Content.ReadAsStringAsync();
                var responseObj = JObject.Parse(responseJson);
                
                var aiMessage = responseObj["message"]?["content"]?.ToString();
                
                if (string.IsNullOrEmpty(aiMessage))
                {
                    return new OllamaResponse
                    {
                        Success = false,
                        ErrorMessage = "AI'dan boş yanıt geldi"
                    };
                }
                
                return new OllamaResponse
                {
                    Success = true,
                    RawContent = aiMessage,
                    ParsedJson = TryParseJson(aiMessage)
                };
            }
            catch (TaskCanceledException)
            {
                return new OllamaResponse
                {
                    Success = false,
                    ErrorMessage = $"Zaman aşımı ({_timeoutSeconds} saniye)"
                };
            }
            catch (HttpRequestException ex)
            {
                return new OllamaResponse
                {
                    Success = false,
                    ErrorMessage = $"Bağlantı hatası: {ex.Message}. Ollama çalışıyor mu?"
                };
            }
            catch (Exception ex)
            {
                return new OllamaResponse
                {
                    Success = false,
                    ErrorMessage = $"Beklenmeyen hata: {ex.Message}"
                };
            }
        }
        
        /// <summary>
        /// Retry mekanizması ile mesaj gönder
        /// </summary>
        public async Task<OllamaResponse> SendMessageWithRetryAsync(string userMessage, 
                                                                    ConversationHistory history = null,
                                                                    int maxRetries = 3)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                var response = await SendMessageAsync(userMessage, history);
                if (response.Success)
                    return response;
                
                if (i < maxRetries - 1)
                {
                    await Task.Delay(1000 * (i + 1)); // Exponential backoff
                }
            }
            
            return new OllamaResponse
            {
                Success = false,
                ErrorMessage = $"{maxRetries} deneme sonrası başarısız"
            };
        }
        
        /// <summary>
        /// Ollama bağlantısını test et
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/tags");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
        
        private object[] BuildMessages(string userMessage, ConversationHistory history)
        {
            var messages = new System.Collections.Generic.List<object>();
            
            // System prompt
            messages.Add(new
            {
                role = "system",
                content = "Sen RoofAI, parametrik tasarım asistanısın. JSON formatında yanıt ver."
            });
            
            // History (son 5 mesaj)
            if (history != null)
            {
                foreach (var msg in history.GetLastMessages(5))
                {
                    messages.Add(new
                    {
                        role = msg.IsUser ? "user" : "assistant",
                        content = msg.Content
                    });
                }
            }
            
            // Current message
            messages.Add(new
            {
                role = "user",
                content = userMessage
            });
            
            return messages.ToArray();
        }
        
        private JObject TryParseJson(string content)
        {
            try
            {
                // JSON'ı içerikten çıkar (metin içinde olabilir)
                int startIndex = content.IndexOf('{');
                int endIndex = content.LastIndexOf('}');
                
                if (startIndex >= 0 && endIndex > startIndex)
                {
                    string jsonStr = content.Substring(startIndex, endIndex - startIndex + 1);
                    return JObject.Parse(jsonStr);
                }
            }
            catch { }
            
            return null;
        }
    }
    
    /// <summary>
    /// Ollama yanıt modeli
    /// </summary>
    public class OllamaResponse
    {
        public bool Success { get; set; }
        public string RawContent { get; set; }
        public JObject ParsedJson { get; set; }
        public string ErrorMessage { get; set; }
        
        public bool HasValidJson => ParsedJson != null;
    }
    
    /// <summary>
    /// Konuşma geçmişi
    /// </summary>
    public class ConversationHistory
    {
        private readonly System.Collections.Generic.List<ChatMessage> _messages = 
            new System.Collections.Generic.List<ChatMessage>();
        
        public void AddMessage(bool isUser, string content)
        {
            _messages.Add(new ChatMessage
            {
                IsUser = isUser,
                Content = content,
                Timestamp = DateTime.Now
            });
        }
        
        public System.Collections.Generic.IEnumerable<ChatMessage> GetLastMessages(int count)
        {
            int start = Math.Max(0, _messages.Count - count);
            for (int i = start; i < _messages.Count; i++)
            {
                yield return _messages[i];
            }
        }
        
        public void Clear()
        {
            _messages.Clear();
        }
    }
    
    public class ChatMessage
    {
        public bool IsUser { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
