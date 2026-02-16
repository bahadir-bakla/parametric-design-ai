using System;
using System.Windows;
using System.Windows.Forms.Integration;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Rhino;
using Rhino.Commands;
using RoofAI.UI;

namespace RoofAI
{
    /// <summary>
    /// Grasshopper Panel entegrasyonu
    /// </summary>
    public class RoofAIPanel : GH_AssemblyPriority
    {
        private static ChatPanelHost _host;
        
        public override GH_LoadingInstruction PriorityLoad()
        {
            // Grasshopper yüklendiğinde panel'i register et
            Grasshopper.Instances.CanvasCreated += RegisterRoofAIPanel;
            return GH_LoadingInstruction.Proceed;
        }
        
        private void RegisterRoofAIPanel(GH_Canvas canvas)
        {
            // Panel'i Grasshopper sidebar'a ekle
            var existingPanel = Grasshopper.Instances.DocumentEditor.FindPanel(typeof(ChatPanelHost));
            
            if (existingPanel == null)
            {
                _host = new ChatPanelHost();
                Grasshopper.Instances.DocumentEditor.AddPanel(_host);
            }
        }
    }
    
    /// <summary>
    /// WPF Panel Host - Grasshopper entegrasyonu için
    /// </summary>
    public class ChatPanelHost : GH_Panel
    {
        private ElementHost _elementHost;
        private ChatPanel _chatPanel;
        
        public ChatPanelHost()
            : base("RoofAI", "RoofAI", GH_PanelAlignment.Right, true)
        {
            InitializeComponent();
        }
        
        private void InitializeComponent()
        {
            // ElementHost oluştur (WinForms -> WPF köprüsü)
            _elementHost = new ElementHost
            {
                Dock = System.Windows.Forms.DockStyle.Fill
            };
            
            // WPF ChatPanel'i oluştur
            _chatPanel = new ChatPanel();
            _elementHost.Child = _chatPanel;
            
            // Host'u panele ekle
            this.Controls.Add(_elementHost);
        }
        
        public override Guid PanelId => new Guid("A1B2C3D4-E5F6-7890-1234-567890ABCDEF");
        
        public override string Title => "RoofAI Asistan";
        
        public override string Description => "AI destekli parametrik tasarım asistanı";
    }
    
    /// <summary>
    /// Manuel panel açma komutu (Rhino komutu olarak)
    /// </summary>
    public class RoofAICommand : Command
    {
        public static RoofAICommand Instance { get; private set; }
        
        public override string EnglishName => "RoofAI";
        
        public RoofAICommand()
        {
            Instance = this;
        }
        
        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                // Panel'i aç/kapat
                var panel = Grasshopper.Instances.DocumentEditor?.FindPanel(typeof(ChatPanelHost));
                
                if (panel != null)
                {
                    // Panel zaten açık, odaklan
                    panel.Focus();
                }
                else
                {
                    // Panel'i oluştur ve aç
                    var host = new ChatPanelHost();
                    Grasshopper.Instances.DocumentEditor?.AddPanel(host);
                }
                
                return Result.Success;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"RoofAI Hatası: {ex.Message}");
                return Result.Failure;
            }
        }
    }
}
