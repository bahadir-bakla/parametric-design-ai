#!/bin/bash
# RoofAI Tam Kurulum Scripti
# WSL2 + Ubuntu + NVIDIA Toolkit + Docker Build

echo "==================================="
echo "RoofAI Tam Kurulum Scripti"
echo "==================================="
echo ""
echo "Bu script şunları yapar:"
echo "  1. WSL2 kontrolü"
echo "  2. Ubuntu 22.04 kurulumu (yoksa)"
echo "  3. NVIDIA Container Toolkit kurulumu"
echo "  4. Docker image build"
echo "  5. Testleri çalıştır"
echo ""

# WSL içinde mi kontrol et
if grep -qE "(Microsoft|WSL)" /proc/version &> /dev/null ; then
    echo "✓ WSL2 içindesin"
    
    # Ubuntu mu kontrol et
    if grep -q "Ubuntu" /etc/os-release 2>/dev/null; then
        echo "✓ Ubuntu tespit edildi"
    else
        echo "⚠️  Docker Desktop WSL içindesin"
        echo "Lütfen normal Ubuntu WSL kullanın:"
        echo "  wsl -d Ubuntu-22.04"
        exit 1
    fi
else
    echo "❌ Bu script WSL2 Ubuntu içinde çalıştırılmalı"
    echo ""
    echo "Önce şunu çalıştır:"
    echo "  wsl -d Ubuntu-22.04"
    echo "  cd /mnt/c/Users/9bakl/OneDrive/Masaüstü/grasshopper\\ eklenti"
    echo "  ./setup-all.sh"
    exit 1
fi

echo ""
read -p "Kuruluma başlamak istiyor musun? (y/n): " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    exit 1
fi

# Adım 1: Sistem güncelleme
echo ""
echo "==================================="
echo "1/5: Sistem paketleri güncelleniyor"
echo "==================================="
sudo apt-get update
sudo apt-get install -y curl gnupg lsb-release

# Adım 2: NVIDIA Container Toolkit
echo ""
echo "==================================="
echo "2/5: NVIDIA Container Toolkit kuruluyor"
echo "==================================="

# NVIDIA repository ekle
distribution=$(. /etc/os-release;echo $ID$VERSION_ID)
curl -fsSL https://nvidia.github.io/libnvidia-container/gpgkey | \
    sudo gpg --dearmor -o /usr/share/keyrings/nvidia-container-toolkit-keyring.gpg

curl -s -L https://nvidia.github.io/libnvidia-container/stable/deb/nvidia-container-toolkit.list | \
    sed 's#deb https://#deb [signed-by=/usr/share/keyrings/nvidia-container-toolkit-keyring.gpg] https://#g' | \
    sudo tee /etc/apt/sources.list.d/nvidia-container-toolkit.list

sudo apt-get update
sudo apt-get install -y nvidia-container-toolkit

# Docker yapılandır
sudo nvidia-ctk runtime configure --runtime=docker
sudo systemctl restart docker || echo "⚠️  Docker restart edilemedi, manuel restart gerekebilir"

# Test
echo ""
echo "🧪 NVIDIA toolkit test ediliyor..."
if sudo docker run --rm --gpus all nvidia/cuda:12.1.0-base-ubuntu22.04 nvidia-smi 2>/dev/null; then
    echo "✅ NVIDIA toolkit çalışıyor!"
else
    echo "⚠️  Test başarısız, ama kurulum tamamlandı"
fi

# Adım 3: Docker Build
echo ""
echo "==================================="
echo "3/5: Docker image build ediliyor"
echo "==================================="
echo "⏳ Bu işlem 10-15 dakika sürebilir..."
echo ""

cd "$(dirname "$0")" || exit 1
docker-compose build

# Adım 4: Test
echo ""
echo "==================================="
echo "4/5: Testler çalıştırılıyor"
echo "==================================="
docker-compose --profile test run --rm roofai-test

# Adım 5: Training hazırlık
echo ""
echo "==================================="
echo "5/5: Kurulum tamamlandı!"
echo "==================================="
echo ""
echo "✅ Tüm kurulumlar tamam!"
echo ""
echo "🎯 Sonraki adım: Model eğitimini başlat"
echo ""
echo "Training'i başlatmak için:"
echo "  docker-compose up roofai-training"
echo ""
echo "VEYA arka planda:"
echo "  docker-compose up -d roofai-training"
echo "  docker-compose logs -f roofai-training"
echo ""
echo "⚠️  NOT: Training 2-4 saat sürecektir!"
echo ""
echo "💡 Training logları: ./logs/training.log"
echo "💡 Model çıktısı: ./roof-ai-v1/"
echo ""
read -p "Şimdi training'i başlatmak ister misin? (y/n): " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    docker-compose up roofai-training
fi
