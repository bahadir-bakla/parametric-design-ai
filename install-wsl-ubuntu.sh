#!/bin/bash
# Ubuntu WSL2 Kurulum Scripti
# Windows'ta WSL2 ve Ubuntu 22.04 kurar

echo "==================================="
echo "Ubuntu WSL2 Kurulumu"
echo "==================================="
echo ""


if [[ "$OSTYPE" != "msys" && "$OSTYPE" != "win32" && "$OSTYPE" != "cygwin" ]]; then
    echo "❌ Bu script Windows'ta çalıştırılmalı"
    echo "PowerShell veya Git Bash kullanın"
    exit 1
fi

echo "ℹ️  Bu script şunları yapacak:"
echo "   1. WSL2'yi etkinleştirecek"
echo "   2. Ubuntu 22.04 LTS kuracak"
echo "   3. WSL2'yi varsayılan yapacak"
echo ""
read -p "Devam etmek istiyor musun? (y/n): " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "İptal edildi"
    exit 1
fi

echo ""
echo "📦 WSL2 etkinleştiriliyor..."


powershell.exe -Command "wsl --install" 2>/dev/null || {
    echo "⚠️  WSL zaten kurulu olabilir veya manuel etkinleştirme gerekiyor"
    echo ""
    echo "Manuel kurulum için PowerShell (Admin):"
    echo "  wsl --install"
}

echo ""
echo "🐧 Ubuntu 22.04 kuruluyor..."
powershell.exe -Command "wsl --install -d Ubuntu-22.04 --web-download" 2>/dev/null || {
    echo "⚠️  Ubuntu kurulumu başarısız veya zaten kurulu"
}

echo ""
echo "⚙️  WSL2 varsayılan olarak ayarlanıyor..."
powershell.exe -Command "wsl --set-default-version 2" 2>/dev/null || true

echo ""
echo "==================================="
echo "Kurulum Tamamlandı!"
echo "==================================="
echo ""
echo "⚠️  ÖNEMLİ: Bilgisayarını YENİDEN BAŞLATMAN gerekiyor!"
echo ""
echo "Yeniden başlattıktan sonra:"
echo ""
echo "1. Ubuntu'yu başlat:"
echo "   wsl -d Ubuntu-22.04"
echo ""
echo "2. İlk kurulumu tamamla (kullanıcı adı/şifre oluştur)"
echo ""
echo "3. Sonra NVIDIA toolkit kur:"
echo "   cd /mnt/c/Users/9bakl/OneDrive/Masaüstü/grasshopper\\ eklenti"
echo "   ./install-nvidia-toolkit.sh"
echo ""
echo "💡 NOT: İlk Ubuntu açılışında kullanıcı adı ve şifre oluşturman istenecek."
echo "   Şifre girerken ekranda görünmez, normaldir."
echo ""
read -p "Yeniden başlatmak için Enter'a bas..."
