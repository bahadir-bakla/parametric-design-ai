import json
import sys
import ollama


def test_model():
    test_cases = [
        {
            "prompt": "20x15 besik cati 30 derece",
            "expected_action": "create",
            "expected_type": "gable"
        },
        {
            "prompt": "dort egimli cati yap 25 metreye 20 metre",
            "expected_action": "create",
            "expected_type": "hip"
        },
        {
            "prompt": "sacaklari 80cm yap",
            "expected_action": "update",
            "expected_param": "eave_overhang"
        },
        {
            "prompt": "egimi 35 dereceye cikar",
            "expected_action": "update",
            "expected_param": "pitch_angle"
        },
        {
            "prompt": "guneye bakan cati",
            "expected_action": "update",
            "expected_param": "orientation"
        },
        {
            "prompt": "Istanbul icin isik analizi",
            "expected_action": "analyze_light"
        },
        {
            "prompt": "oglen saatlerinde golge analizi yap",
            "expected_action": "analyze_light"
        },
        {
            "prompt": "mutfak icin cati pencereleri",
            "expected_action": "optimize_skylights"
        },
        {
            "prompt": "4 tane velux pencere koy",
            "expected_action": "optimize_skylights"
        },
        {
            "prompt": "cati yap",
            "expected_action": "clarify"
        },
    ]

    passed = 0
    failed = 0
    errors = []

    print("=" * 60)
    print("RoofAI Model Test Suite")
    print("=" * 60)

    for i, test in enumerate(test_cases):
        print(f"\nTest {i + 1}/{len(test_cases)}: {test['prompt']}")
        print("-" * 40)

        try:
            response = ollama.chat(model="roof-ai", messages=[
                {"role": "user", "content": test["prompt"]}
            ])

            content = response["message"]["content"]
            print(f"Response: {content[:200]}")

            json_start = content.find("{")
            json_end = content.rfind("}") + 1

            if json_start >= 0 and json_end > json_start:
                json_str = content[json_start:json_end]
                parsed = json.loads(json_str)

                action = parsed.get("action", "")

                if action == test.get("expected_action"):
                    print(f"  [PASS] Action: {action}")
                    passed += 1
                else:
                    print(f"  [FAIL] Expected action '{test.get('expected_action')}', got '{action}'")
                    failed += 1
                    errors.append(f"Test {i + 1}: action mismatch")

                if "expected_type" in test:
                    roof_type = parsed.get("roof_type", "")
                    if roof_type == test["expected_type"]:
                        print(f"  [PASS] Type: {roof_type}")
                    else:
                        print(f"  [WARN] Expected type '{test['expected_type']}', got '{roof_type}'")
            else:
                print("  [FAIL] No valid JSON in response")
                failed += 1
                errors.append(f"Test {i + 1}: no JSON")

        except json.JSONDecodeError as e:
            print(f"  [FAIL] JSON parse error: {e}")
            failed += 1
            errors.append(f"Test {i + 1}: JSON parse error")
        except Exception as e:
            print(f"  [FAIL] Error: {e}")
            failed += 1
            errors.append(f"Test {i + 1}: {e}")

    total = passed + failed
    print(f"\n{'=' * 60}")
    print(f"Results: {passed}/{total} passed ({100 * passed / total:.1f}%)")
    if errors:
        print(f"Errors:")
        for err in errors:
            print(f"  - {err}")
    print("=" * 60)

    return passed == total


if __name__ == "__main__":
    success = test_model()
    sys.exit(0 if success else 1)
