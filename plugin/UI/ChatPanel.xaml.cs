using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using RoofAI.API;
using RoofAI.Core;
using RoofAI.Geometry;
using RoofAI.Models;
using Newtonsoft.Json.Linq;
using Rhino;
using Rhino.Geometry;
using Rhino.DocObjects;

namespace RoofAI.UI
{
    /// <summary>
    /// RoofAI Chat Panel code-behind
    /// </summary>
    public partial class ChatPanel : UserControl
    {
        private OllamaClient _ollamaClient;
        private ResponseParser _responseParser;
        private GeometryEngine _geometryEngine;
        private ConversationManager _conversationManager;
        private AdvancedContextManager _contextManager;
        
        public ChatPanel()
        {
            InitializeComponent();
            InitializeServices();
            
            // Otomatik scroll
            _conversationManager.MessageAdded += (s, e) => 
                Dispatcher.BeginInvoke(new Action(() => ScrollToBottom()));
        }
        
        private void InitializeServices()
        {
            _ollamaClient = new OllamaClient(
                baseUrl: "http://localhost:11434",
                modelName: "roof-ai",
                timeoutSeconds: 30
            );
            
            _responseParser = new ResponseParser();
            _geometryEngine = new GeometryEngine();
            _conversationManager = new ConversationManager(maxHistorySize: 5);
            _contextManager = new AdvancedContextManager();
        }
        
        #region UI Event Handlers
        
        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            await ProcessUserInput();
        }
        
        private async void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await ProcessUserInput();
            }
        }
        
        private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Placeholder effect (isteğe bağlı)
        }
        
        private void QuickAction_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag != null)
            {
                InputTextBox.Text = button.Tag.ToString();
                ProcessUserInput();
            }
        }
        
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // Settings penceresi aç
            MessageBox.Show("Ayarlar yakında eklenecek!", "RoofAI", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Konuşma geçmişi temizlensin mi?", "Onay", 
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                ClearMessages();
                _conversationManager.Clear();
            }
        }
        
        #endregion
        
        #region Core Logic
        
        private async Task ProcessUserInput()
        {
            string userMessage = InputTextBox.Text.Trim();
            if (string.IsNullOrEmpty(userMessage)) return;
            
            // UI güncelle
            InputTextBox.Clear();
            AddUserMessage(userMessage);
            ShowTypingIndicator(true);
            SetStatus("AI düşünüyor...", Brushes.Orange);
            
            try
            {
                // Advanced Context Analysis
                var selectionAnalysis = _contextManager.AnalyzeSelection();
                var commandContext = _contextManager.DetermineCommandContext(userMessage, selectionAnalysis);
                
                UpdateContextIndicator(selectionAnalysis.Count, commandContext.Action);
                
                // AI'a gönder
                var response = await _ollamaClient.SendMessageWithRetryAsync(
                    userMessage, 
                    _conversationManager.GetRecentMessages(5)
                );
                
                ShowTypingIndicator(false);
                
                if (!response.Success)
                {
                    AddErrorMessage($"Hata: {response.ErrorMessage}");
                    SetStatus("Bağlantı hatası", Brushes.Red);
                    return;
                }
                
                // Yanıtı işle (context bilgisi ile)
                await ProcessAIResponseWithContext(response, userMessage, commandContext);
                SetStatus("Hazır", Brushes.Green);
            }
            catch (Exception ex)
            {
                ShowTypingIndicator(false);
                AddErrorMessage($"Beklenmeyen hata: {ex.Message}");
                SetStatus("Hata", Brushes.Red);
            }
        }
        
        private async Task ProcessAIResponseWithContext(OllamaResponse response, string originalCommand, 
                                            CommandContext commandContext)
        {
            if (!response.HasValidJson)
            {
                AddAIMessage(response.RawContent);
                _conversationManager.AddMessage(MessageType.AI, response.RawContent, originalCommand);
                return;
            }
            
            var json = response.ParsedJson;
            
            ParseResult parseResult;
            
            // Context mode'e göre işlem yap
            if (commandContext.Mode == ContextMode.UpdateExisting && commandContext.TargetObjectIds.Count > 0)
            {
                // Mevcut objeyi güncelle
                var existingObj = GetParametricObjectFromSelection(commandContext.TargetObjectIds[0]);
                parseResult = _responseParser.ParseUpdateCommand(json, existingObj);
            }
            else if (json["action"]?.ToString()?.ToLower() == "clarify")
            {
                parseResult = _responseParser.ParseClarification(json);
            }
            else
            {
                // Yeni obje oluştur
                parseResult = _responseParser.ParseResponse(json, originalCommand);
            }
            
            if (!parseResult.Success)
            {
                AddAIMessage($"❌ {parseResult.ErrorMessage}");
                _conversationManager.AddMessage(MessageType.AI, parseResult.ErrorMessage, originalCommand, false);
                return;
            }
            
            if (parseResult.IsClarification)
            {
                AddAIMessage($"🤔 {parseResult.ErrorMessage}");
                _conversationManager.AddMessage(MessageType.AI, parseResult.ErrorMessage, originalCommand);
                return;
            }
            
            // Geometri üret (context'e göre)
            GenerationResult genResult;
            if (commandContext.Mode == ContextMode.UpdateExisting && commandContext.TargetObjectIds.Count > 0)
            {
                genResult = _geometryEngine.Update(parseResult.Parameters, commandContext.TargetObjectIds);
            }
            else
            {
                genResult = _geometryEngine.Generate(parseResult.Parameters);
            }
            
            if (!genResult.Success)
            {
                AddAIMessage($"❌ Geometri hatası: {genResult.ErrorMessage}");
                _conversationManager.AddMessage(MessageType.AI, genResult.ErrorMessage, originalCommand, false);
                return;
            }
            
            // Rhino'ya ekle
            var rhinoDoc = RhinoDoc.ActiveDoc;
            if (rhinoDoc != null)
            {
                var addedIds = new System.Collections.Generic.List<Guid>();
                
                foreach (var geom in genResult.Geometries)
                {
                    Guid objId = Guid.Empty;
                    
                    if (geom is Curve curve)
                    {
                        objId = rhinoDoc.Objects.AddCurve(curve);
                    }
                    else if (geom is Brep brep)
                    {
                        objId = rhinoDoc.Objects.AddBrep(brep);
                    }
                    else if (geom is Mesh mesh)
                    {
                        objId = rhinoDoc.Objects.AddMesh(mesh);
                    }
                    
                    if (objId != Guid.Empty)
                    {
                        addedIds.Add(objId);
                        _contextManager.AttachMetadata(objId, parseResult.ObjectType, 
                            parseResult.Parameters.ToJson().ToString());
                    }
                }
                
                rhinoDoc.Views.Redraw();
                
                // Başarı mesajı
                string successMsg = $"✅ {parseResult.ObjectType} objesi oluşturuldu: " +
                    $"{genResult.Geometries.Count} geometri eklendi";
                AddAIMessage(successMsg);
                _conversationManager.AddMessage(MessageType.AI, successMsg, originalCommand, true);
            }
        }
        
        private ParametricObject GetParametricObjectFromSelection(SelectedObjectInfo selected)
        {
            // Metadata'dan parametrik obje oluştur
            var metadata = _contextManager.GetMetadata(selected.Id);
            if (metadata != null)
            {
                // Basit implementasyon - gerçekte metadata'dan deserialize edilmeli
                return new RoofParameters(); // Placeholder
            }
            return new RoofParameters();
        }
        
        #endregion
        
        #region UI Helpers
        
        private void AddUserMessage(string text)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(220, 248, 198)),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12, 8),
                Margin = new Thickness(5),
                HorizontalAlignment = HorizontalAlignment.Right,
                MaxWidth = 280,
                Child = new TextBlock
                {
                    Text = text,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 13
                }
            };
            
            MessagesPanel.Children.Add(border);
            ScrollToBottom();
        }
        
        private void AddAIMessage(string text)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12, 8),
                Margin = new Thickness(5),
                HorizontalAlignment = HorizontalAlignment.Left,
                MaxWidth = 280,
                BorderBrush = new SolidColorBrush(Color.FromRgb(221, 221, 221)),
                BorderThickness = new Thickness(1),
                Child = new TextBlock
                {
                    Text = text,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 13
                }
            };
            
            MessagesPanel.Children.Add(border);
            ScrollToBottom();
        }
        
        private void AddErrorMessage(string text)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(248, 215, 218)),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12, 8),
                Margin = new Thickness(5),
                HorizontalAlignment = HorizontalAlignment.Center,
                MaxWidth = 300,
                Child = new TextBlock
                {
                    Text = text,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(114, 28, 36))
                }
            };
            
            MessagesPanel.Children.Add(border);
            ScrollToBottom();
        }
        
        private void ClearMessages()
        {
            // İlk hoşgeldin mesajı hariç temizle
            while (MessagesPanel.Children.Count > 1)
            {
                MessagesPanel.Children.RemoveAt(1);
            }
        }
        
        private void ShowTypingIndicator(bool show)
        {
            TypingIndicator.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            if (show)
            {
                StartTypingAnimation();
            }
        }
        
        private void StartTypingAnimation()
        {
            // Basit dots animasyonu
            var storyboard = new Storyboard();
            // Animasyon implementasyonu...
        }
        
        private void SetStatus(string text, Brush color)
        {
            StatusText.Text = text;
            StatusIndicator.Fill = color;
        }
        
        private void UpdateContextIndicator(int selectedCount, CommandType commandType)
        {
            if (selectedCount > 0)
            {
                ContextIndicator.Text = $"• {selectedCount} obje seçili • {commandType}";
            }
            else
            {
                ContextIndicator.Text = "";
            }
        }
        
        private void ScrollToBottom()
        {
            MessagesScrollViewer.ScrollToEnd();
        }
        
        #endregion
    }
}
