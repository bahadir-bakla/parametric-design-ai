import json
import ollama


class RoofAIInference:
    def __init__(self, model_name="roof-ai", base_url="http://localhost:11434"):
        self.model_name = model_name
        self.conversation_history = []

    def chat(self, user_message):
        self.conversation_history.append({
            "role": "user",
            "content": user_message
        })

        response = ollama.chat(
            model=self.model_name,
            messages=self.conversation_history
        )

        assistant_message = response["message"]["content"]
        self.conversation_history.append({
            "role": "assistant",
            "content": assistant_message
        })

        return assistant_message

    def parse_response(self, response_text):
        json_start = response_text.find("{")
        json_end = response_text.rfind("}") + 1

        if json_start >= 0 and json_end > json_start:
            json_str = response_text[json_start:json_end]
            try:
                return json.loads(json_str)
            except json.JSONDecodeError:
                return None
        return None

    def get_natural_text(self, response_text):
        json_start = response_text.find("{")
        if json_start > 0:
            return response_text[:json_start].strip()
        return response_text.strip()

    def reset(self):
        self.conversation_history = []

    def chat_and_parse(self, user_message):
        response_text = self.chat(user_message)
        parsed = self.parse_response(response_text)
        natural_text = self.get_natural_text(response_text)

        return {
            "raw": response_text,
            "parsed": parsed,
            "text": natural_text,
            "action": parsed.get("action") if parsed else None
        }


def interactive_mode():
    print("=" * 60)
    print("RoofAI - Parametrik Cati Tasarim Asistani")
    print("=" * 60)
    print("Komutlar: 'cikis' veya 'exit' ile cik, 'sifirla' ile sifirla")
    print()

    engine = RoofAIInference()

    while True:
        try:
            user_input = input("Siz: ").strip()
        except (EOFError, KeyboardInterrupt):
            print("\nGorumuzere!")
            break

        if not user_input:
            continue

        if user_input.lower() in ("cikis", "exit", "quit"):
            print("Gorusmeler!")
            break

        if user_input.lower() in ("sifirla", "reset"):
            engine.reset()
            print("Konusma sifirlandi.\n")
            continue

        try:
            result = engine.chat_and_parse(user_input)
            print(f"\nAI: {result['text']}")

            if result["parsed"]:
                print(f"Parametreler: {json.dumps(result['parsed'], indent=2, ensure_ascii=False)}")
            print()

        except Exception as e:
            print(f"Hata: {e}")
            print("Ollama calistiginden emin olun: ollama serve\n")


if __name__ == "__main__":
    interactive_mode()
