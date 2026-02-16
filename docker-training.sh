#!/bin/bash
# RoofAI Docker Training Script

set -e

echo "==================================="
echo "RoofAI Docker Training Setup"
echo "==================================="
echo ""

# Check if running in WSL
if grep -qE "(Microsoft|WSL)" /proc/version &> /dev/null ; then
    echo "✓ WSL detected"
else
    echo "⚠ Not running in WSL"
fi

# Check Docker
echo "Checking Docker..."
if ! command -v docker &> /dev/null; then
    echo "❌ Docker not found. Please install Docker Desktop with WSL2 support."
    exit 1
fi
echo "✓ Docker found"

# Check NVIDIA Container Toolkit
echo ""
echo "Checking NVIDIA Container Toolkit..."
if ! command -v nvidia-smi &> /dev/null; then
    echo "⚠ nvidia-smi not found. Make sure NVIDIA drivers are installed on Windows."
else
    echo "✓ NVIDIA drivers found"
    nvidia-smi --query-gpu=name,memory.total --format=csv,noheader
fi

# Build image
echo ""
echo "==================================="
echo "Building Docker image..."
echo "==================================="
docker-compose build

# Run tests first
echo ""
echo "==================================="
echo "Running tests..."
echo "==================================="
docker-compose --profile test run --rm roofai-test

# Ask for training confirmation
read -p "Tests passed! Start training? (y/n): " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    echo ""
    echo "==================================="
    echo "Starting training..."
    echo "==================================="
    echo "This will take 2-4 hours depending on your GPU."
    echo "Logs will be saved to ./logs/training.log"
    echo ""
    
    # Create logs directory
    mkdir -p logs
    
    # Run training with logging
    docker-compose up roofai-training 2>&1 | tee logs/training.log
    
    echo ""
    echo "==================================="
    echo "Training complete!"
    echo "==================================="
    echo "Model saved to: ./roof-ai-v1/"
    echo ""
    echo "To export to Ollama, run:"
    echo "  cd roof-ai-v1 && ollama create roof-ai -f Modelfile"
else
    echo "Training cancelled."
fi
