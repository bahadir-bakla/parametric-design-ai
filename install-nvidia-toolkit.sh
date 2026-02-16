#!/bin/bash
# NVIDIA Container Toolkit Kurulum Scripti
# WSL2 Ubuntu için

set -e

echo "==================================="
echo "NVIDIA Container Toolkit Kurulumu"
echo "==================================="
echo ""

# Check if running as root
if [ "$EUID" -eq 0 ]; then 
   echo "❌ Lütfen root olarak çalıştırmayın (sudo kullanmayın)"
   echo "Script gerekli yerlerde sudo kullanacak"
   exit 1
fi

# Check if running in WSL
echo "WSL kontrol ediliyor..."
if grep -qE "(Microsoft|WSL)" /proc/version &> /dev/null ; then
    echo "✓ WSL2 tespit edildi"
else
    echo "⚠ WSL2 ortamı tespit edilemedi"
    echo "Bu script WSL2 için tasarlanmıştır"
    read -p "Devam etmek istiyor musunuz? (y/n): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        exit 1
    fi
fi

# Check Ubuntu version
echo ""
echo "Ubuntu versiyonu kontrol ediliyor..."
UBUNTU_VERSION=$(lsb_release -rs)
echo "✓ Ubuntu $UBUNTU_VERSION"

# Update package list
echo ""
echo "📦 Paket listesi güncelleniyor..."
sudo apt-get update

# Install prerequisites
echo ""
echo "📦 Gerekli paketler kuruluyor..."
sudo apt-get install -y \
    curl \
    gnupg \
    lsb-release \
    software-properties-common \
    apt-transport-https \
    ca-certificates

# Add NVIDIA package repositories
echo ""
echo "🔧 NVIDIA paket kaynakları ekleniyor..."

# Get distribution
distribution=$(. /etc/os-release;echo $ID$VERSION_ID)
echo "Dağıtım: $distribution"

# Add NVIDIA GPG key
echo "NVIDIA GPG anahtarı ekleniyor..."
curl -fsSL https://nvidia.github.io/libnvidia-container/gpgkey | \
    sudo gpg --dearmor -o /usr/share/keyrings/nvidia-container-toolkit-keyring.gpg

# Add NVIDIA repository
echo "NVIDIA repository ekleniyor..."
curl -s -L https://nvidia.github.io/libnvidia-container/stable/deb/nvidia-container-toolkit.list | \
    sed 's#deb https://#deb [signed-by=/usr/share/keyrings/nvidia-container-toolkit-keyring.gpg] https://#g' | \
    sudo tee /etc/apt/sources.list.d/nvidia-container-toolkit.list

# Update package list again
echo ""
echo "📦 Paket listesi tekrar güncelleniyor..."
sudo apt-get update

# Install NVIDIA Container Toolkit
echo ""
echo "🔧 NVIDIA Container Toolkit kuruluyor..."
sudo apt-get install -y nvidia-container-toolkit

# Configure Docker
echo ""
echo "🔧 Docker yapılandırılıyor..."
sudo nvidia-ctk runtime configure --runtime=docker

# Restart Docker
echo ""
echo "🔄 Docker servisi yeniden başlatılıyor..."
if sudo systemctl restart docker 2>/dev/null; then
    echo "✓ Docker servisi yeniden başlatıldı"
else
    echo "⚠ Docker servisi yeniden başlatılamadı"
    echo "Docker Desktop'ı manuel olarak yeniden başlatmanız gerekebilir"
fi

# Verify installation
echo ""
echo "==================================="
echo "Kurulum Doğrulanıyor"
echo "==================================="
echo ""

# Check if nvidia-ctk is installed
if command -v nvidia-ctk &> /dev/null; then
    echo "✓ nvidia-ctk kurulu"
    nvidia-ctk --version
else
    echo "❌ nvidia-ctk bulunamadı"
    exit 1
fi

# Test Docker GPU access
echo ""
echo "🧪 Docker GPU erişimi test ediliyor..."
if sudo docker run --rm --gpus all nvidia/cuda:12.1.0-base-ubuntu22.04 nvidia-smi 2>/dev/null; then
    echo ""
    echo "✅ BAŞARILI! NVIDIA Container Toolkit çalışıyor!"
else
    echo ""
    echo "⚠️ Test başarısız olabilir (Docker imajı indiriliyor olabilir)"
    echo "Kurulum tamamlandı, ancak test için imaj indirilmesi gerekiyor"
    echo ""
    echo "Manuel test için:"
    echo "  sudo docker run --rm --gpus all nvidia/cuda:12.1.0-base-ubuntu22.04 nvidia-smi"
fi

echo ""
echo "==================================="
echo "Kurulum Tamamlandı!"
echo "==================================="
echo ""
echo "🎯 Sonraki Adımlar:"
echo ""
echo "1. Docker Desktop'ı yeniden başlat (gerekirse)"
echo "   - Windows'ta Docker Desktop uygulamasını kapatıp açın"
echo ""
echo "2. RoofAI Docker imajını build edin:"
echo "   docker-compose build"
echo ""
echo "3. Testleri çalıştırın:"
echo "   docker-compose --profile test run --rm roofai-test"
echo ""
echo "4. Training'i başlatın:"
echo "   docker-compose up roofai-training"
echo ""
echo "💡 Sorun olursa DOCKER_GUIDE.md dosyasına bakın"
echo ""
