import json
import os
import ollama


FEEDBACK_FILE = "data/feedback.jsonl"


def load_feedback(min_rating=4):
    if not os.path.exists(FEEDBACK_FILE):
        print(f"Feedback file not found: {FEEDBACK_FILE}")
        return []

    high_quality = []
    total = 0

    with open(FEEDBACK_FILE, "r", encoding="utf-8") as f:
        for line in f:
            line = line.strip()
            if not line:
                continue
            try:
                entry = json.loads(line)
                total += 1
                if entry.get("rating", 0) >= min_rating:
                    high_quality.append(entry)
            except json.JSONDecodeError:
                continue

    print(f"Loaded {len(high_quality)}/{total} high-quality feedback entries (rating >= {min_rating})")
    return high_quality


def convert_feedback_to_training(feedback_entries):
    training_samples = []

    for entry in feedback_entries:
        user_input = entry.get("user_input", "")
        ai_response = entry.get("ai_response", "")

        if not user_input or not ai_response:
            continue

        training_samples.append({
            "id": f"feedback_{entry.get('id', 'unknown')}",
            "category": "user_feedback",
            "messages": [
                {"role": "user", "content": user_input},
                {"role": "assistant", "content": ai_response}
            ]
        })

    return training_samples


def merge_with_existing(new_samples, existing_file="data/training_data.json"):
    if os.path.exists(existing_file):
        with open(existing_file, "r", encoding="utf-8") as f:
            existing = json.load(f)
    else:
        existing = {"conversations": []}

    existing_ids = {s["id"] for s in existing["conversations"]}
    added = 0

    for sample in new_samples:
        if sample["id"] not in existing_ids:
            existing["conversations"].append(sample)
            added += 1

    print(f"Added {added} new samples from feedback")
    print(f"Total training samples: {len(existing['conversations'])}")

    with open(existing_file, "w", encoding="utf-8") as f:
        json.dump(existing, f, ensure_ascii=False, indent=2)

    return existing


def save_feedback(user_input, ai_response, rating, notes=""):
    entry = {
        "id": f"fb_{len(open(FEEDBACK_FILE).readlines()) if os.path.exists(FEEDBACK_FILE) else 0:06d}",
        "user_input": user_input,
        "ai_response": ai_response,
        "rating": rating,
        "notes": notes
    }

    os.makedirs(os.path.dirname(FEEDBACK_FILE), exist_ok=True)
    with open(FEEDBACK_FILE, "a", encoding="utf-8") as f:
        f.write(json.dumps(entry, ensure_ascii=False) + "\n")

    return entry["id"]


def main():
    print("=" * 60)
    print("RoofAI - Feedback-based Retraining")
    print("=" * 60)

    feedback = load_feedback(min_rating=4)

    if not feedback:
        print("No high-quality feedback found. Collect more user ratings first.")
        return

    training_samples = convert_feedback_to_training(feedback)
    print(f"Converted {len(training_samples)} feedback entries to training format")

    merge_with_existing(training_samples)

    print("\nTo retrain the model, run:")
    print("  python model/fine_tuning.py")
    print("\nThen recreate the Ollama model:")
    print("  cd model && ollama create roof-ai -f Modelfile")


if __name__ == "__main__":
    main()
