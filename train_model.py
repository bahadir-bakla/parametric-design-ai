#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
RoofAI Model Training Script
Rehberdeki formatta - Phi-3-mini üzerinde LoRA fine-tuning
"""

import torch
import json
import os
from transformers import (
    AutoModelForCausalLM,
    AutoTokenizer,
    TrainingArguments,
    Trainer,
    DataCollatorForLanguageModeling
)
from peft import LoraConfig, get_peft_model, prepare_model_for_kbit_training
from datasets import load_dataset


class RoofAITrainer:
    def __init__(self, output_dir="./roof-ai-v1"):
        self.output_dir = output_dir
        
        # Config yükle veya varsayılan kullan
        config_path = os.path.join(os.path.dirname(__file__), "model", "model_config.json")
        if os.path.exists(config_path):
            with open(config_path, "r") as f:
                config = json.load(f)
            self.model_name = config.get("model_name", "microsoft/Phi-3-mini-4k-instruct")
        else:
            self.model_name = "microsoft/Phi-3-mini-4k-instruct"
        
        self.device = "cuda" if torch.cuda.is_available() else "cpu"
        
        print("🚀 Initializing RoofAI Trainer")
        print(f"   Model: {self.model_name}")
        print(f"   Device: {self.device}")
        print(f"   LoRA: True")
        print()
    
    def load_model_and_tokenizer(self):
        """Model ve tokenizer'ı yükle"""
        print("📦 Loading model and tokenizer...")
        
        self.tokenizer = AutoTokenizer.from_pretrained(
            self.model_name,
            trust_remote_code=True
        )
        
        if self.tokenizer.pad_token is None:
            self.tokenizer.pad_token = self.tokenizer.eos_token
        
        self.model = AutoModelForCausalLM.from_pretrained(
            self.model_name,
            torch_dtype=torch.float16,
            device_map="auto" if self.device == "cuda" else None,
            trust_remote_code=True,
            attn_implementation="eager"
        )
        
        if self.device == "cpu":
            self.model = self.model.to(self.device)
        
        total_params = sum(p.numel() for p in self.model.parameters())
        print("✅ Model loaded successfully")
        print(f"   Parameters: {total_params:,}")
        print()
    
    def configure_lora(self):
        """LoRA yapılandırması"""
        print("🔧 Configuring LoRA...")
        
        lora_config = LoraConfig(
            r=16,
            lora_alpha=32,
            target_modules=["q_proj", "k_proj", "v_proj", "o_proj"],
            lora_dropout=0.05,
            bias="none",
            task_type="CAUSAL_LM"
        )
        
        self.model = prepare_model_for_kbit_training(self.model)
        self.model = get_peft_model(self.model, lora_config)
        
        trainable_params = sum(p.numel() for p in self.model.parameters() if p.requires_grad)
        total_params = sum(p.numel() for p in self.model.parameters())
        
        print("✅ LoRA configured")
        print(f"   Trainable params: {trainable_params:,} ({100 * trainable_params / total_params:.2f}%)")
        print()
    
    def load_dataset(self, train_file="data/training_data.json", val_file="data/validation_data.json"):
        """Dataset'i yükle ve formatla"""
        print("📚 Loading dataset...")
        
        dataset = load_dataset("json", data_files={
            "train": train_file,
            "validation": val_file if os.path.exists(val_file) else train_file
        }, field="conversations")
        
        def format_conversation(sample):
            messages = sample["messages"]
            formatted = self.tokenizer.apply_chat_template(
                messages,
                tokenize=False,
                add_generation_prompt=False
            )
            return {"text": formatted}
        
        def tokenize_function(sample):
            result = self.tokenizer(
                sample["text"],
                truncation=True,
                max_length=512,
                padding="max_length"
            )
            result["labels"] = result["input_ids"].copy()
            return result
        
        dataset = dataset.map(format_conversation, remove_columns=dataset["train"].column_names)
        dataset = dataset.map(tokenize_function, remove_columns=["text"])
        
        print(f"   Train samples: {len(dataset['train'])}")
        print(f"   Validation samples: {len(dataset['validation'])}")
        print()
        
        return dataset
    
    def train(self, dataset, epochs=3, batch_size=4):
        """Modeli eğit"""
        print("🎓 Starting training...")
        print(f"   Epochs: {epochs}")
        print(f"   Batch size: {batch_size}")
        print()
        
        training_args = TrainingArguments(
            output_dir=self.output_dir,
            num_train_epochs=epochs,
            per_device_train_batch_size=batch_size,
            per_device_eval_batch_size=batch_size,
            gradient_accumulation_steps=4,
            learning_rate=2e-4,
            lr_scheduler_type="cosine",
            warmup_steps=100,
            logging_steps=10,
            save_steps=100,
            save_total_limit=3,
            eval_steps=100,
            evaluation_strategy="steps",
            fp16=torch.cuda.is_available(),
            optim="adamw_torch",
            report_to="none",
            remove_unused_columns=False,
        )
        
        data_collator = DataCollatorForLanguageModeling(
            tokenizer=self.tokenizer,
            mlm=False
        )
        
        trainer = Trainer(
            model=self.model,
            args=training_args,
            train_dataset=dataset["train"],
            eval_dataset=dataset["validation"],
            data_collator=data_collator,
        )
        
        print("🚀 Training started...")
        print()
        trainer.train()
        
        print("\n✅ Training completed!")
        
        # Final eval loss
        eval_results = trainer.evaluate()
        final_loss = eval_results.get("eval_loss", 0)
        print(f"   Final loss: {final_loss:.4f}")
        print()
    
    def save_model(self):
        """Modeli kaydet"""
        print(f"💾 Saving model to {self.output_dir}")
        
        self.model.save_pretrained(self.output_dir)
        self.tokenizer.save_pretrained(self.output_dir)
        
        # Modelfile oluştur
        self._create_modelfile()
        
        print("✅ Model saved successfully!")
    
    def _create_modelfile(self):
        """Ollama için Modelfile oluştur"""
        modelfile_content = '''FROM ./roof-ai-v1

PARAMETER temperature 0.7
PARAMETER top_p 0.9
PARAMETER stop "<|end|>"
PARAMETER stop "<|endoftext|>"

TEMPLATE """{{ if .System }}<|system|>
{{ .System }}<|end|>
{{ end }}{{ if .Prompt }}<|user|>
{{ .Prompt }}<|end|>
{{ end }}<|assistant|>
{{ .Response }}<|end|>
"""

SYSTEM """Sen RoofAI, parametrik cati tasarimi yapan bir AI asistanisin. 
Kullanicinin komutlarini anlayip JSON formatinda cati parametreleri uretiyorsun.
Turkce dogal dille konus ama ciktiyi JSON olarak ver.

Destekledigin cati tipleri:
- gable (besik)
- hip (dort egim)
- gambrel (kirma)
- shed (tek egim)
- flat (duz)

JSON formati:
{
  "action": "create" | "update" | "analyze_light" | "optimize_skylights",
  "roof_type": "gable" | "hip" | "gambrel" | "shed" | "flat",
  "length": number,
  "width": number,
  "pitch_angle": number,
  "eave_overhang": number,
  "parameters": {...}
}
"""
'''
        
        modelfile_path = os.path.join(self.output_dir, "Modelfile")
        with open(modelfile_path, "w", encoding="utf-8") as f:
            f.write(modelfile_content)
        
        print(f"   Created Modelfile at {modelfile_path}")
    
    def test_model(self, prompt):
        """Modeli test et"""
        messages = [{"role": "user", "content": prompt}]
        formatted = self.tokenizer.apply_chat_template(
            messages,
            tokenize=False,
            add_generation_prompt=True
        )
        
        inputs = self.tokenizer(formatted, return_tensors="pt").to(self.device)
        
        with torch.no_grad():
            outputs = self.model.generate(
                **inputs,
                max_new_tokens=256,
                temperature=0.7,
                do_sample=True,
                top_p=0.9
            )
        
        response = self.tokenizer.decode(outputs[0], skip_special_tokens=True)
        # Assistant kısmını çıkar
        if "assistant" in response:
            response = response.split("assistant")[-1].strip()
        
        return response


def main():
    """Ana eğitim fonksiyonu"""
    trainer = RoofAITrainer(output_dir="./roof-ai-v1")
    
    # Model ve tokenizer'ı yükle
    trainer.load_model_and_tokenizer()
    
    # LoRA yapılandır
    trainer.configure_lora()
    
    # Dataset'i yükle
    dataset = trainer.load_dataset()
    
    # Eğitimi başlat
    trainer.train(dataset, epochs=3, batch_size=4)
    
    # Modeli kaydet
    trainer.save_model()
    
    # Test et
    print("\n🧪 Testing model...")
    test_prompts = [
        "20x15 besik cati yap 30 derece",
        "sacaklari 80cm yap",
        "Istanbul icin oglen isik analizi"
    ]
    
    for prompt in test_prompts:
        print(f"\nPrompt: {prompt}")
        try:
            response = trainer.test_model(prompt)
            print(f"Response: {response}")
        except Exception as e:
            print(f"Error: {e}")
    
    print("\n✨ Training pipeline complete!")


if __name__ == "__main__":
    main()
