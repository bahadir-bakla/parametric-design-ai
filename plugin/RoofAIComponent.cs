using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Newtonsoft.Json.Linq;

namespace RoofAI
{
    public class RoofAIComponent : GH_Component
    {
        private readonly ModelConnector _connector;
        private string _conversationHistory = "";
        private JObject _currentRoofParameters;
        private string _lastUserInput = "";
        private string _lastAiResponse = "";

        public RoofAIComponent()
          : base("RoofAI Chat", "RoofAI",
              "AI ile konusarak parametrik cati tasarla",
              "RoofAI", "Design")
        {
            _connector = new ModelConnector();
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("User Input", "Input",
                "Komutunuzu yazin (orn: '20x15 besik cati 30 derece')", GH_ParamAccess.item, "");
            pManager.AddBooleanParameter("Send", "Send",
                "Komutu AI'ya gonder", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Reset", "Reset",
                "Konusmayi ve tasarimi sifirla", GH_ParamAccess.item, false);
            pManager.AddTextParameter("Ollama URL", "URL",
                "Ollama API adresi", GH_ParamAccess.item, "http://localhost:11434");
            pManager.AddIntegerParameter("Rating", "Rate",
                "Son tasarimi puanla (1-5, 0=puanlama)", GH_ParamAccess.item, 0);

            pManager[3].Optional = true;
            pManager[4].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Roof Geometry", "Roof",
                "Uretilen cati geometrisi", GH_ParamAccess.list);
            pManager.AddTextParameter("AI Response", "Response",
                "AI'nin cevabi", GH_ParamAccess.item);
            pManager.AddTextParameter("Parameters", "Params",
                "Guncel cati parametreleri (JSON)", GH_ParamAccess.item);
            pManager.AddTextParameter("Conversation", "Conv",
                "Konusma gecmisi", GH_ParamAccess.item);
            pManager.AddNumberParameter("Roof Area", "Area",
                "Cati alani (m2)", GH_ParamAccess.item);
            pManager.AddTextParameter("Status", "Status",
                "Durum mesaji", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string userInput = "";
            bool send = false;
            bool reset = false;
            string ollamaUrl = "http://localhost:11434";
            int rating = 0;

            DA.GetData(0, ref userInput);
            DA.GetData(1, ref send);
            DA.GetData(2, ref reset);
            DA.GetData(3, ref ollamaUrl);
            DA.GetData(4, ref rating);

            if (rating >= 1 && rating <= 5)
            {
                HandleFeedback(rating, DA);
            }

            if (reset)
            {
                _conversationHistory = "";
                _currentRoofParameters = null;
                _lastUserInput = "";
                _lastAiResponse = "";
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Konusma ve tasarim sifirlandi");
                DA.SetData(3, "Konusma sifirlandi");
                DA.SetData(5, "Sifirlandi");
                return;
            }

            if (!send || string.IsNullOrWhiteSpace(userInput))
            {
                OutputCurrentState(DA);
                DA.SetData(5, _currentRoofParameters != null ? "Hazir" : "Bekleniyor");
                return;
            }

            try
            {
                DA.SetData(5, "AI dusunuyor...");

                if (!ValidateOllamaConnection(ollamaUrl))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                        "Ollama'ya baglanilamiyor. 'ollama serve' komutunu calistirin.");
                    DA.SetData(1, "Hata: Ollama baglantisi yok");
                    DA.SetData(5, "Baglanti hatasi");
                    return;
                }

                string contextPrompt = _conversationHistory + "\nSiz: " + userInput + "\nAI:";
                var aiResponse = Task.Run(() => _connector.SendMessageAsync(contextPrompt)).Result;

                _lastUserInput = userInput;
                _lastAiResponse = aiResponse;

                var roofParams = _connector.ParseJson(aiResponse);

                if (roofParams == null)
                {
                    _conversationHistory += $"\nSiz: {userInput}";
                    _conversationHistory += $"\nAI: {aiResponse}\n";
                    DA.SetData(1, aiResponse);
                    DA.SetData(3, _conversationHistory);
                    DA.SetData(5, "AI yanit verdi (JSON yok)");
                    OutputCurrentState(DA);
                    return;
                }

                string action = roofParams["action"]?.ToString() ?? "create";
                ProcessAction(action, roofParams, userInput, DA);

                string naturalText = _connector.ExtractNaturalText(aiResponse);
                _conversationHistory += $"\nSiz: {userInput}";
                _conversationHistory += $"\nAI: {naturalText}\n";

                DA.SetData(1, aiResponse);
                DA.SetData(3, _conversationHistory);
                OutputCurrentState(DA);
                DA.SetData(5, $"Tamamlandi: {action}");
            }
            catch (AggregateException ae)
            {
                string msg = ae.InnerException?.Message ?? ae.Message;
                HandleError(msg, DA);
            }
            catch (Exception ex)
            {
                HandleError(ex.Message, DA);
            }
        }

        private void ProcessAction(string action, JObject roofParams, string userInput, IGH_DataAccess DA)
        {
            switch (action)
            {
                case "create":
                    if (!ValidateCreateParams(roofParams))
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                            "Eksik parametreler var, varsayilan degerler kullaniliyor.");
                    }
                    _currentRoofParameters = roofParams;
                    break;

                case "update":
                    if (_currentRoofParameters == null)
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                            "Guncellenecek cati yok. Once bir cati olusturun.");
                        return;
                    }
                    var updates = roofParams["parameters"] as JObject;
                    if (updates != null)
                    {
                        foreach (var prop in updates.Properties())
                        {
                            if (ValidateParameterValue(prop.Name, prop.Value))
                                _currentRoofParameters[prop.Name] = prop.Value;
                            else
                                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                                    $"Gecersiz deger: {prop.Name} = {prop.Value}");
                        }
                    }
                    break;

                case "analyze_light":
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                        "Isik analizi icin 'RoofAI Light Analysis' componentini kullanin.");
                    break;

                case "optimize_skylights":
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                        "Pencere optimizasyonu icin 'RoofAI Skylight' componentini kullanin.");
                    break;

                case "clarify":
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                        "AI ek bilgi istiyor. Lutfen detay verin.");
                    break;
            }
        }

        private bool ValidateCreateParams(JObject p)
        {
            bool valid = true;
            if (p["roof_type"] == null) valid = false;
            if (p["length"] == null) valid = false;
            if (p["width"] == null) valid = false;
            return valid;
        }

        private bool ValidateParameterValue(string name, JToken value)
        {
            try
            {
                double v = value.Value<double>();
                switch (name)
                {
                    case "length":
                    case "width":
                        return v > 0 && v < 200;
                    case "pitch_angle":
                        return v >= 0 && v <= 85;
                    case "eave_overhang":
                        return v >= 0 && v <= 5;
                    case "orientation":
                        return v >= -360 && v <= 360;
                    case "ridge_height":
                        return v > 0 && v < 50;
                    default:
                        return true;
                }
            }
            catch
            {
                return true;
            }
        }

        private bool ValidateOllamaConnection(string url)
        {
            try
            {
                return Task.Run(() => _connector.TestConnectionAsync()).Result;
            }
            catch
            {
                return false;
            }
        }

        private void HandleFeedback(int rating, IGH_DataAccess DA)
        {
            if (string.IsNullOrEmpty(_lastUserInput)) return;

            try
            {
                string paramsJson = _currentRoofParameters?.ToString() ?? "";
                FeedbackCollector.SaveFeedback(
                    _lastUserInput, _lastAiResponse, rating, paramsJson);

                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                    $"Puan kaydedildi: {rating}/5");
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    "Feedback kaydedilemedi: " + ex.Message);
            }
        }

        private void HandleError(string message, IGH_DataAccess DA)
        {
            string userFriendly;

            if (message.Contains("baginamad") || message.Contains("connection") ||
                message.Contains("No connection"))
            {
                userFriendly = "Ollama'ya baglanılamadı. Cozum:\n" +
                              "1) Terminal'de 'ollama serve' komutunu calistirin\n" +
                              "2) Ollama URL'sini kontrol edin (varsayilan: http://localhost:11434)";
            }
            else if (message.Contains("model") || message.Contains("not found"))
            {
                userFriendly = "roof-ai modeli bulunamadi. Cozum:\n" +
                              "1) 'ollama list' ile modelleri kontrol edin\n" +
                              "2) 'ollama create roof-ai -f Modelfile' ile modeli olusturun";
            }
            else if (message.Contains("timeout") || message.Contains("Timeout"))
            {
                userFriendly = "AI yanit suresi doldu. Model cok mu yogun? " +
                              "Daha kisa bir komut deneyin.";
            }
            else
            {
                userFriendly = "Beklenmeyen hata: " + message;
            }

            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, userFriendly);
            DA.SetData(1, "Hata: " + userFriendly);
            DA.SetData(5, "Hata");
        }

        private void OutputCurrentState(IGH_DataAccess DA)
        {
            if (_currentRoofParameters == null) return;

            try
            {
                string roofType = _currentRoofParameters["roof_type"]?.ToString() ?? "gable";
                double length = _currentRoofParameters["length"]?.Value<double>() ?? 20;
                double width = _currentRoofParameters["width"]?.Value<double>() ?? 15;
                double pitch = _currentRoofParameters["pitch_angle"]?.Value<double>() ?? 30;
                double overhang = _currentRoofParameters["eave_overhang"]?.Value<double>() ?? 0.5;
                double orientation = _currentRoofParameters["orientation"]?.Value<double>() ?? 0;

                var geometry = GeometryEngine.GenerateRoof(roofType, length, width, pitch, overhang, orientation);
                double area = GeometryEngine.CalculateRoofArea(geometry);

                DA.SetDataList(0, geometry);
                DA.SetData(2, _currentRoofParameters.ToString());
                DA.SetData(4, Math.Round(area, 2));
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Geometri hatasi: " + ex.Message);
            }
        }

        protected override System.Drawing.Bitmap Icon => null;

        public override Guid ComponentGuid =>
            new Guid("A1B2C3D4-E5F6-7890-ABCD-EF1234567890");
    }
}
