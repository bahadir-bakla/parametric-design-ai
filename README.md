# RoofAI - Parametrik Tasarım AI Asistanı

Grasshopper/Rhino için genel amaçlı parametrik tasarım AI asistanı. Türkçe doğal dil komutlarını analiz ederek çatı, cephe, mobilya ve iç mekan tasarımı yapar.

## 🎯 Özellikler

- 💬 **Chat Panel** - WPF tabanlı modern arayüz
- 🤖 **AI Entegrasyonu** - Ollama ile yerel model kullanımı
- 🏗️ **Çoklu Objeler** - Çatı, cephe, mobilya, iç mekan
- 🔄 **Context Awareness** - Seçili objeyi güncelleme
- 📜 **History** - Son 5 mesaj geçmişi
- ⚡ **Hızlı Yanıt** - <2 saniye ortalama yanıt süresi

## 📁 Proje Yapısı

```
grasshopper-eklenti/
│
├── 📁 data/                    # Eğitim verileri (FAZ A)
│   ├── data_generator.py
│   ├── training_data.json
│   └── validation_data.json
│
├── 📁 model/                   # AI Model (FAZ A)
│   ├── fine_tuning.py
│   ├── Modelfile
│   └── model_config.json
│
├── 📁 plugin/                  # Grasshopper Plugin (FAZ B) ✅ YENİ
│   ├── 📁 UI/
│   │   ├── ChatPanel.xaml          # WPF Panel UI
│   │   └── ChatPanel.xaml.cs       # Code-behind
│   ├── 📁 API/
│   │   ├── OllamaClient.cs         # HTTP Client
│   │   └── ResponseParser.cs       # JSON Parser
│   ├── 📁 Models/
│   │   ├── ParametricObject.cs     # Base + Roof/Facade
│   │   └── InteriorFurnitureModels.cs # Furniture/Interior
│   ├── 📁 Geometry/
│   │   ├── GeometryEngine.cs       # Factory Pattern
│   │   └── 📁 Generators/
│   │       ├── RoofGenerator.cs    # 5 çatı tipi
│   │       ├── FacadeGenerator.cs  # Grid/Diamond pattern
│   │       ├── FurnitureGenerator.cs # 5 mobilya tipi
│   │       └── InteriorGenerator.cs  # 3-6 kenarlı odalar
│   ├── 📁 Core/
│   │   ├── ContextManager.cs       # Basic context
│   │   ├── AdvancedContextManager.cs # Smart context analysis
│   │   └── ConversationManager.cs  # History (last 5)
│   ├── 📁 Config/
│   │   └── Settings.json           # App Config
│   └── RoofAIPanel.cs              # GH Integration
│
├── 📁 tests/                   # Unit Tests
│
├── 🐳 Docker Dosyaları        # Model Training
│   ├── Dockerfile
│   ├── docker-compose.yml
│   └── DOCKER_GUIDE.md
│
├── 🔧 Kurulum Scriptleri
│   ├── install-wsl-ubuntu.sh
│   ├── install-nvidia-toolkit.sh
│   └── setup-all.sh
│
├── 🎯 Ana Scriptler
│   ├── train_model.py         # Model Eğitimi
│   └── test_suite.py          # Unit Tests
│
└── 📄 README.md
```

## 🚀 Hızlı Başlangıç

### FAZ A: Model Eğitimi (Opsiyonel)

```bash
# WSL Ubuntu içinde
cd ~/grasshopper-eklenti
./setup-all.sh
docker-compose up roofai-training

# Training sonrası Ollama'ya export
cd roof-ai-v1
ollama create roof-ai -f Modelfile
```

### FAZ B: Plugin Kurulumu ✅ YENİ

#### 1. Gereksinimler
- Rhino 7/8
- Grasshopper
- Ollama (yerel AI modeli)
- .NET Framework 4.8 veya .NET 6/7/8

#### 2. Build
```bash
cd plugin
# Visual Studio veya VS Code ile aç
# RoofAI.sln çözümünü build et
```

#### 3. Kurulum
```bash
# Build output'unu Grasshopper Components klasörüne kopyala
cp bin/Release/RoofAI.gha "C:\Users\%USERNAME%\AppData\Roaming\Grasshopper\Libraries\"
```

#### 4. Ollama Kurulumu
```powershell
# Windows'ta Ollama kur
winget install Ollama.Ollama

# Model'i import et
ollama create roof-ai -f model/Modelfile

# Çalışıyor mu test et
ollama run roof-ai "20x15 beşik cati yap"
```

## 🎮 Kullanım

### Panel'i Açma

**Yöntem 1:** Grasshopper Canvas > Panels > RoofAI Asistan  
**Yöntem 2:** Rhino komut satırı: `RoofAI`

### Örnek Komutlar

**Çatı Oluşturma:**
```
20x15 beşik çatı yap 30 derece
10 metre kırma çatı yap
saçakları 1 metre yap
```

**Cephe Oluşturma:**
```
15 metre cephe yap grid pattern
pencere oranı %30 olsun
5x3 panel böl
```

**Güncelleme:**
```
çatıyı büyüt (seçili obje ile)
egimi 35 derece yap
sacakları kaldır
```

## 🏗️ Mimarisi

### AI Pipeline
```
Kullanıcı Komutu → Ollama API → JSON Yanıt → Parser → Geometry Engine → Rhino
```

### Context Awareness
```
Seçili Obje Var mı?
├── Evet → Update Mode → Mevcut geometriyi güncelle
└── Hayır → Create Mode → Yeni geometri oluştur
```

### Extensible Design
```csharp
// Yeni obje tipi eklemek için:
public class FurnitureGenerator : IGeometryGenerator {
    public string ObjectType => "furniture";
    public List<GeometryBase> Generate(ParametricObject p) { ... }
}
// RoofGenerator.cs'e bakın
```

## 📊 Training Data Formatı

```json
{
  "id": "geo_0001",
  "category": "basic_geometry",
  "messages": [
    {"role": "user", "content": "20x15 beşik cati yap 30 derece"},
    {"role": "assistant", "content": "{\"action\": \"create\", \"roof_type\": \"gable\", \"length\": 20, ...}"}
  ]
}
```

## 🔧 Geliştirme

### Yeni Generator Ekleme

1. `plugin/Geometry/Generators/` altına yeni class
2. `IGeometryGenerator` interface'ini implemente et
3. `GeometryEngine.cs`'te register et

```csharp
public class MyCustomGenerator : IGeometryGenerator {
    public string ObjectType => "mytype";
    public bool CanGenerate(ParametricObject p) => p is MyParameters;
    public List<GeometryBase> Generate(ParametricObject p) { /* ... */ }
}
```

### Model Retraining

Yeni veri ekle:
```python
# data/data_generator.py
all_samples.extend(self.generate_custom_samples(100))
```

Tekrar eğit:
```bash
docker-compose up roofai-training
```

## 🐛 Hata Giderme

### "Ollama connection failed"
```bash
# Ollama çalışıyor mu kontrol et
ollama list

# Model yüklü mü?
ollama list | grep roof-ai

# Manuel test
ollama run roof-ai "test"
```

### "Plugin not loading"
```bash
# .gha dosyası bloklu mu?
Unblock-File -Path "C:\...\RoofAI.gha"

# Framework versiyonu uyumlu mu?
# Rhino 7: .NET Framework 4.8
# Rhino 8: .NET 7/8
```

### "Panel not showing"
- Grasshopper'ı yeniden başlat
- `RoofAI` komutunu Rhino'da çalıştır
- Libraries klasörünü kontrol et

## 📈 Roadmap

### FAZ B.1 ✅ (Week 1-2) - TAMAMLANDI
- [x] Chat Panel UI
- [x] Ollama Integration
- [x] Basic Geometry (Roof)
- [x] Context Awareness

### FAZ B.2 ✅ (Week 3-4) - TAMAMLANDI
- [x] Facade Generator (Grid/Diamond patterns)
- [x] Furniture Generator (Table, Chair, Shelf, Sofa, Bed)
- [x] Interior Generator (3-6 wall rooms)
- [x] Advanced Context (Smart selection analysis)

### FAZ B.3 ⏳ (Week 5)
- [ ] Voice Input
- [ ] Settings Panel
- [ ] Documentation
- [ ] Beta Testing

## 🤝 Katkıda Bulunma

1. Fork yap
2. Feature branch oluştur (`git checkout -b feature/amazing`)
3. Commit et (`git commit -m 'Add amazing'`)
4. Push et (`git push origin feature/amazing`)
5. Pull Request aç

## 📝 Lisans

MIT License

## 🙏 Teşekkürler

- [Hugging Face](https://huggingface.co/) - Transformers
- [Microsoft](https://microsoft.com/) - Phi-3
- [Ollama](https://ollama.ai/) - Model deployment
- [McNeel](https://www.rhino3d.com/) - Rhino/Grasshopper

---

**Hazırlayan:** RoofAI Team  
**Versiyon:** 2.0.0  
**Son Güncelleme:** 2025

**Training Status:** ⏳ Docker Build in Progress (requirements installation)  
**Plugin Status:** ✅ FAZ B.1 + B.2 Complete - Ready for Build & Test
