# RoofAI Docker Kullanım Kılavuzu

## 🚀 Hızlı Başlangıç

### 1. NVIDIA Container Toolkit Kurulumu (WSL2'de)

```bash
# 1. NVIDIA Container Toolkit'i kur
distribution=$(. /etc/os-release;echo $ID$VERSION_ID)
curl -s -L https://nvidia.github.io/nvidia-docker/gpgkey | sudo apt-key add -
curl -s -L https://nvidia.github.io/nvidia-docker/$distribution/nvidia-docker.list | sudo tee /etc/apt/sources.list.d/nvidia-docker.list

sudo apt-get update
sudo apt-get install -y nvidia-docker2

# 2. Docker servisini yeniden başlat
sudo systemctl restart docker
```

### 2. Docker Image'ını Build Et

```bash
# Proje klasörüne git
cd ~/grasshopper-eklenti

# Image'ı build et (~5-10 dk sürebilir)
docker-compose build
```

### 3. Testleri Çalıştır

```bash
# Test servisini çalıştır
docker-compose --profile test run --rm roofai-test
```

### 4. Model Eğitimini Başlat

```bash
# Training'i başlat (2-4 saat sürebilir)
docker-compose up roofai-training

# VEYA arka planda çalıştır
docker-compose up -d roofai-training

# Logları izle
docker-compose logs -f roofai-training
```

---

## 📁 Dosya Yapısı

```
grasshopper-eklenti/
├── Dockerfile                 # Docker imaj tanımı
├── docker-compose.yml         # Multi-service yapılandırması
├── .dockerignore             # Docker build'inde hariç tutulanlar
├── docker-training.sh        # Linux/Mac training script
├── train_model.py            # Ana training script
├── test_suite.py             # Unit testler
├── data/                     # Eğitim verileri (volume)
├── model/                    # Model kodları
├── roof-ai-v1/              # Eğitilmiş model çıktısı (volume)
└── logs/                    # Training logları (volume)
```

---

## 🐳 Docker Komutları

### Temel Komutlar

```bash
# Image'ı build et
docker-compose build

# Training'i başlat
docker-compose up roofai-training

# Arka planda başlat
docker-compose up -d roofai-training

# Container'ı durdur
docker-compose down

# Logları izle
docker-compose logs -f roofai-training

# Container içine gir (debug için)
docker-compose exec roofai-training bash

# Container'ı temizle ve yeniden başlat
docker-compose down -v
docker-compose up roofai-training
```

### GPU Kontrolü

```bash
# Container içinde GPU'yu kontrol et
docker-compose exec roofai-training nvidia-smi

# PyTorch CUDA desteğini kontrol et
docker-compose exec roofai-training python -c "import torch; print(f'CUDA: {torch.cuda.is_available()}')"
```

---

## 📊 Training İzleme

### Log Dosyaları

Training logları otomatik olarak `./logs/` klasörüne kaydedilir:

```bash
# Log dosyasını izle
tail -f logs/training.log

# Son 100 satırı gör
tail -n 100 logs/training.log

# GPU kullanımını izle (başka terminal)
watch -n 1 nvidia-smi
```

### Training Durumu

```bash
# Container durumunu kontrol et
docker-compose ps

# GPU kullanımını gör
docker-compose exec roofai-training nvidia-smi
```

---

## 🔧 Sorun Giderme

### 1. "nvidia-smi not found" Hatası

**Çözüm:** Windows tarafında NVIDIA sürücülerini kontrol et

```powershell
# PowerShell'de (Windows tarafında)
nvidia-smi
```

Eğer çalışmıyorsa NVIDIA sürücülerini güncelle.

### 2. "could not select device driver" Hatası

**Çözüm:** NVIDIA Container Toolkit kurulu değil

```bash
# Kurulumu doğrula
docker run --rm --gpus all nvidia/cuda:12.1.0-base-ubuntu22.04 nvidia-smi
```

### 3. Bellek Hatası (OOM)

**Çözüm:** Batch size'ı düşür

`train_model.py` içinde:
```python
training_args = TrainingArguments(
    per_device_train_batch_size=2,  # 4 yerine 2
    gradient_accumulation_steps=8,  # 4 yerine 8
    ...
)
```

### 4. Model İndirme Sorunları

**Çözüm:** HuggingFace cache'i temizle

```bash
docker-compose down -v
rm -rf ~/.cache/huggingface
```

### 5. Port/Socket Hatası

**Çözüm:** Docker Desktop WSL2 integration'ı kontrol et

1. Docker Desktop → Settings → Resources → WSL Integration
2. Ubuntu'yu enable et
3. Docker Desktop'ı yeniden başlat

---

## 📦 Ollama Export (Training Sonrası)

Training tamamlandığında:

```bash
# Model klasörüne git
cd roof-ai-v1

# Ollama'ya import et
ollama create roof-ai -f Modelfile

# Test et
ollama run roof-ai "20x15 besik cati yap"
```

---

## 💡 Pro Tips

### Hızlı Iterasyon

```bash
# Sadece test et
docker-compose --profile test run --rm roofai-test

# Training'i debug modunda başlat (container açık kalır)
docker-compose run --rm roofai-training bash
# Sonra içinde: python train_model.py
```

### Model Cache'ini Sakla

HuggingFace modelleri büyük (3-4 GB). İlk indirmeden sonra cache'lenir:

```bash
# Volume'u silme, cache kalsın
docker-compose down

# Sadece training container'ını sil
docker-compose rm roofai-training
```

### Disk Alanı

```bash
# Docker disk kullanımını gör
docker system df

# Temizlik yap
docker system prune -a

# Image'ları listele
docker images
```

---

## 🎯 Training Sonrası Checklist

- [ ] `roof-ai-v1/` klasörü oluşmuş
- [ ] `roof-ai-v1/Modelfile` var
- [ ] `logs/training.log` loss değerlerini gösteriyor
- [ ] Ollama model listesinde `roof-ai` var

**Başarılar! 🚀**
