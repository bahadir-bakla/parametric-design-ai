using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RoofAI
{
    public class ModelConnector : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _modelName;

        public ModelConnector(string baseUrl = "http://localhost:11434", string modelName = "roof-ai")
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _modelName = modelName;
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        public async Task<string> SendMessageAsync(string prompt, string conversationContext = "")
        {
            string fullPrompt = string.IsNullOrEmpty(conversationContext)
                ? prompt
                : conversationContext + "\n" + prompt;

            var payload = new
            {
                model = _modelName,
                prompt = fullPrompt,
                stream = false,
                options = new
                {
                    temperature = 0.7,
                    top_p = 0.9
                }
            };

            var jsonContent = JsonConvert.SerializeObject(payload);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/api/generate", content);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Ollama API hatasi: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            }

            var responseString = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(responseString);

            return json["response"]?.ToString() ?? "";
        }

        public async Task<ChatResponse> ChatAsync(string userMessage, string conversationContext = "")
        {
            string rawResponse = await SendMessageAsync(userMessage, conversationContext);

            return new ChatResponse
            {
                RawText = rawResponse,
                Parameters = ParseJson(rawResponse),
                NaturalText = ExtractNaturalText(rawResponse)
            };
        }

        public JObject ParseJson(string text)
        {
            int jsonStart = text.IndexOf('{');
            int jsonEnd = text.LastIndexOf('}') + 1;

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                string jsonStr = text.Substring(jsonStart, jsonEnd - jsonStart);
                try
                {
                    return JObject.Parse(jsonStr);
                }
                catch (JsonException)
                {
                    return null;
                }
            }
            return null;
        }

        public string ExtractNaturalText(string text)
        {
            int jsonStart = text.IndexOf('{');
            if (jsonStart > 0)
            {
                return text.Substring(0, jsonStart).Trim();
            }
            return "Cati olusturuldu.";
        }

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

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    public class ChatResponse
    {
        public string RawText { get; set; }
        public JObject Parameters { get; set; }
        public string NaturalText { get; set; }

        public string Action => Parameters?["action"]?.ToString();
        public bool HasValidJson => Parameters != null;
    }
}
