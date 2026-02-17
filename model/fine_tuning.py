import torch
from transformers import (
    AutoModelForCausalLM,
    AutoTokenizer,
    TrainingArguments,
    Trainer,
    DataCollatorForLanguageModeling
)
from peft import LoraConfig, get_peft_model, prepare_model_for_kbit_training
from datasets import load_dataset
import json
import os


class RoofAITrainer:
    def __init__(self, model_name="microsoft/Phi-3-mini-4k-instruct"):
        self.model_name = model_name
        self.device = "cuda" if torch.cuda.is_available() else "cpu"
        print(f"Using device: {self.device}")
        if self.device == "cuda":
            print(f"GPU: {torch.cuda.get_device_name(0)}")
            print(f"VRAM: {torch.cuda.get_device_properties(0).total_mem / 1e9:.1f} GB")

    def load_model_and_tokenizer(self):
        print(f"Loading model: {self.model_name}")

        self.tokenizer = AutoTokenizer.from_pretrained(
            self.model_name,
            trust_remote_code=True
        )

        if self.tokenizer.pad_token is None:
            self.tokenizer.pad_token = self.tokenizer.eos_token

        self.model = AutoModelForCausalLM.from_pretrained(
            self.model_name,
            torch_dtype=torch.float16,
            device_map="auto",
            trust_remote_code=True,
            attn_implementation="eager"
        )

        print(f"Model loaded on {self.device}")

    def configure_lora(self):
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
        print(f"Trainable params: {trainable_params:,} ({100 * trainable_params / total_params:.2f}%)")

    def load_dataset(self, train_file, val_file=None):
        print(f"Loading dataset from {train_file}")

        dataset = load_dataset("json", data_files={
            "train": train_file,
            "validation": val_file if val_file else train_file
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

        print(f"Dataset loaded: {len(dataset['train'])} training, {len(dataset['validation'])} validation")
        return dataset

    def train(self, dataset, output_dir="./roof-ai-v1"):
        training_args = TrainingArguments(
            output_dir=output_dir,
            num_train_epochs=3,
            per_device_train_batch_size=4,
            per_device_eval_batch_size=4,
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

        print("Starting training...")
        trainer.train()
        print("Training complete!")

        self.model.save_pretrained(output_dir)
        self.tokenizer.save_pretrained(output_dir)
        print(f"Model saved to {output_dir}")

    def test_model(self, prompt):
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
        response = response.split("assistant")[-1].strip()
        return response


def main():
    config_path = os.path.join(os.path.dirname(__file__), "model_config.json")
    if os.path.exists(config_path):
        with open(config_path, "r") as f:
            config = json.load(f)
        model_name = config.get("model_name", "microsoft/Phi-3-mini-4k-instruct")
    else:
        model_name = "microsoft/Phi-3-mini-4k-instruct"

    trainer = RoofAITrainer(model_name=model_name)
    trainer.load_model_and_tokenizer()
    trainer.configure_lora()

    dataset = trainer.load_dataset(
        train_file="data/training_data.json",
        val_file="data/validation_data.json"
    )

    trainer.train(dataset, output_dir="./roof-ai-v1")

    print("\nTesting model...")
    test_prompts = [
        "20x15 besik cati yap 30 derece",
        "sacaklari 80cm yap",
        "Istanbul icin oglen isik analizi"
    ]

    for prompt in test_prompts:
        print(f"\nPrompt: {prompt}")
        response = trainer.test_model(prompt)
        print(f"Response: {response}")


if __name__ == "__main__":
    main()
