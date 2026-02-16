import json
import random
from datetime import datetime


class RoofAIDataGenerator:
    def __init__(self):
        self.roof_types = {
            "gable": ["besik", "besik cati", "iki egimli"],
            "hip": ["dort egim", "dort egimli", "piramit"],
            "gambrel": ["kirma", "kirma cati", "mansart"],
            "shed": ["tek egim", "tek egimli", "rampa"],
            "flat": ["duz", "teras", "duz cati"]
        }

        self.actions = ["yap", "olustur", "tasarla", "ciz", "istiyorum"]
        self.materials = ["kiremit", "celik", "bitum", "shingle", "metal"]
        self.cities = ["Istanbul", "Ankara", "Izmir", "Kayseri", "Antalya", "Bursa",
                       "Trabzon", "Konya", "Gaziantep", "Samsun", "Erzurum", "Diyarbakir"]

    def generate_basic_geometry(self, count=200):
        samples = []
        for i in range(count):
            length = random.randint(10, 50)
            width = random.randint(8, 30)
            pitch = random.randint(15, 45)
            overhang = round(random.uniform(0.3, 1.2), 1)

            roof_type = random.choice(list(self.roof_types.keys()))
            roof_name_tr = random.choice(self.roof_types[roof_type])
            action = random.choice(self.actions)
            material = random.choice(self.materials)

            patterns = [
                f"{length} metreye {width} metre {roof_name_tr} {action}, egim {pitch} derece",
                f"{length}x{width} {roof_name_tr}, {pitch} derece egim",
                f"{roof_name_tr} cati {length} metre uzunluk {width} metre genislik",
                f"{pitch} derece egimli {roof_name_tr} {action}, {length}m x {width}m",
                f"{length} metre uzunlugunda {width} metre genisliginde {roof_name_tr} {action}",
                f"{roof_name_tr} {action} {length}x{width}, sacak {overhang} metre",
                f"bana {length} metreye {width} metre bir {roof_name_tr} {action}",
                f"{roof_name_tr} tipi cati {action}, boyutlar {length}x{width}m, egim {pitch}",
            ]

            user_msg = random.choice(patterns)

            assistant_msg = {
                "action": "create",
                "roof_type": roof_type,
                "length": length,
                "width": width,
                "pitch_angle": pitch,
                "eave_overhang": overhang,
                "ridge_height": "auto",
                "orientation": 0,
                "material": material
            }

            samples.append({
                "id": f"geo_{i:04d}",
                "category": "basic_geometry",
                "messages": [
                    {"role": "user", "content": user_msg},
                    {"role": "assistant", "content": json.dumps(assistant_msg, ensure_ascii=False)}
                ]
            })

        return samples

    def generate_modifications(self, count=150):
        samples = []
        params = [
            ("sacak", "eave_overhang", 0.3, 1.5, "metre"),
            ("egim", "pitch_angle", 15, 50, "derece"),
            ("yukseklik", "ridge_height", 3, 8, "metre"),
            ("uzunluk", "length", 10, 40, "metre"),
            ("genislik", "width", 8, 25, "metre"),
        ]

        for i in range(count):
            param_name, param_key, min_val, max_val, unit = random.choice(params)
            value = round(random.uniform(min_val, max_val), 1)

            patterns = [
                f"{param_name}i {value} {unit} yap",
                f"{param_name} {value} {unit} olsun",
                f"{param_name}lari {value} {unit}'ye cikar",
                f"{param_name}i degistir, {value} {unit} olsun",
                f"{param_name}i biraz daha {'buyuk' if random.random() > 0.5 else 'kucuk'} yap",
                f"daha {'genis' if param_key == 'width' else 'uzun' if param_key == 'length' else 'yuksek'} olsun",
                f"{param_name}i guncelle: {value} {unit}",
            ]

            user_msg = random.choice(patterns)

            assistant_msg = {
                "action": "update",
                "parameters": {param_key: value}
            }

            samples.append({
                "id": f"mod_{i:04d}",
                "category": "modifications",
                "messages": [
                    {"role": "user", "content": user_msg},
                    {"role": "assistant", "content": json.dumps(assistant_msg, ensure_ascii=False)}
                ]
            })

        return samples

    def generate_orientation(self, count=100):
        samples = []
        directions = {
            "north": ["kuzey", "kuzeye", "kuzey yonunde"],
            "south": ["guney", "guneye", "guney yonunde"],
            "east": ["dogu", "doguya", "dogu yonunde"],
            "west": ["bati", "batiya", "bati yonunde"],
        }
        direction_angles = {"north": 0, "south": 180, "east": 90, "west": 270}

        for i in range(count):
            direction = random.choice(list(directions.keys()))
            direction_tr = random.choice(directions[direction])
            angle = random.randint(0, 90)

            patterns = [
                f"{direction_tr} bakan cati",
                f"mahya {direction_tr} olsun",
                f"catiyi {angle} derece dondur",
                f"{direction_tr} {angle} derece",
                f"cati yonu {direction_tr} olsun",
                f"{direction_tr} yonune baksn",
                f"oriyntasyonu {direction_tr} yap",
            ]

            user_msg = random.choice(patterns)

            assistant_msg = {
                "action": "update",
                "parameters": {
                    "orientation": direction_angles.get(direction, angle),
                    "primary_direction": direction
                }
            }

            samples.append({
                "id": f"orient_{i:04d}",
                "category": "orientation",
                "messages": [
                    {"role": "user", "content": user_msg},
                    {"role": "assistant", "content": json.dumps(assistant_msg, ensure_ascii=False)}
                ]
            })

        return samples

    def generate_light_analysis(self, count=200):
        samples = []
        times = ["sabah", "oglen", "ogleden sonra", "aksam"]
        months = ["ocak", "subat", "mart", "nisan", "mayis", "haziran",
                  "temmuz", "agustos", "eylul", "ekim", "kasim", "aralik"]
        analysis_types = ["sunpath", "shadow", "irradiance", "thermal"]

        for i in range(count):
            time_of_day = random.choice(times)
            month = random.choice(months)
            city = random.choice(self.cities)
            hour = random.randint(9, 17)
            analysis = random.choice(analysis_types)

            patterns = [
                f"{time_of_day} isik analizi yap",
                f"{city} icin {month} ayinda gunes analizi",
                f"saat {hour}:00'de golge analizi",
                f"{month} ayi {time_of_day} isinma analizi",
                f"yaz aylarinda fazla isinmasin",
                f"kisin maksimum gunes alsin",
                f"{city} icin {time_of_day} saatlerinde isik durumu",
                f"21 Haziran saat {hour}:00 golge analizi yap",
                f"{city} {month} isik analizi",
            ]

            user_msg = random.choice(patterns)

            assistant_msg = {
                "action": "analyze_light",
                "location": city.lower(),
                "time_of_day": time_of_day,
                "month": month,
                "hour": hour,
                "analysis_type": analysis
            }

            samples.append({
                "id": f"light_{i:04d}",
                "category": "light_analysis",
                "messages": [
                    {"role": "user", "content": user_msg},
                    {"role": "assistant", "content": json.dumps(assistant_msg, ensure_ascii=False)}
                ]
            })

        return samples

    def generate_skylight_optimization(self, count=100):
        samples = []
        rooms = ["salon", "mutfak", "yatak odasi", "calisma odasi", "banyo"]
        goals = ["maximize_daylight", "minimize_glare", "balanced"]

        for i in range(count):
            room = random.choice(rooms)
            count_windows = random.randint(2, 6)
            goal = random.choice(goals)

            patterns = [
                f"{room} icin cati pencereleri",
                f"{count_windows} tane velux koy",
                f"{room}'ya gun isigi alsin",
                f"optimal pencere yerlestir {room}",
                f"{room} icin {count_windows} adet cati penceresi",
                f"guney yuzune {count_windows} pencere koy",
                f"{room} icin isik optimizasyonu yap",
            ]

            user_msg = random.choice(patterns)

            assistant_msg = {
                "action": "optimize_skylights",
                "room_type": room,
                "count": count_windows,
                "optimization_goal": goal
            }

            samples.append({
                "id": f"skylight_{i:04d}",
                "category": "skylight_optimization",
                "messages": [
                    {"role": "user", "content": user_msg},
                    {"role": "assistant", "content": json.dumps(assistant_msg, ensure_ascii=False)}
                ]
            })

        return samples

    def generate_error_handling(self, count=50):
        samples = []
        error_patterns = [
            ("cati yap", {"action": "clarify", "message": "Kac metre x kac metre olsun? Cati tipini de belirtir misiniz?"}),
            ("cok buyuk yap", {"action": "clarify", "message": "Tam olarak kac metre dusunuyorsunuz? Mevcut boyutlari ne kadar artiralim?"}),
            ("egim yok", {"action": "clarify", "message": "Duz cati mi yoksa hafif egimli mi (su tahliyesi icin en az 2-3 derece onerilir)?"}),
            ("guzel bir cati", {"action": "clarify", "message": "Boyutlari ve cati tipini belirtir misiniz? Ornegin: 20x15 besik cati 30 derece"}),
            ("degistir", {"action": "clarify", "message": "Hangi parametreyi degistirmek istiyorsunuz? (egim, sacak, boyut, yon...)"}),
            ("pencere", {"action": "clarify", "message": "Hangi oda icin pencere istiyorsunuz? Kac adet dusunuyorsunuz?"}),
        ]

        for i in range(count):
            user_msg, assistant_response = random.choice(error_patterns)

            if random.random() > 0.5:
                variations = [
                    user_msg,
                    user_msg + " lutfen",
                    "hemen " + user_msg,
                    user_msg.upper(),
                ]
                user_msg = random.choice(variations)

            samples.append({
                "id": f"error_{i:04d}",
                "category": "error_handling",
                "messages": [
                    {"role": "user", "content": user_msg},
                    {"role": "assistant", "content": json.dumps(assistant_response, ensure_ascii=False)}
                ]
            })

        return samples

    def generate_full_dataset(self):
        all_samples = []

        print("Generating basic geometry samples...")
        all_samples.extend(self.generate_basic_geometry(250))

        print("Generating modification samples...")
        all_samples.extend(self.generate_modifications(200))

        print("Generating orientation samples...")
        all_samples.extend(self.generate_orientation(150))

        print("Generating light analysis samples...")
        all_samples.extend(self.generate_light_analysis(250))

        print("Generating skylight optimization samples...")
        all_samples.extend(self.generate_skylight_optimization(150))

        print("Generating error handling samples...")
        all_samples.extend(self.generate_error_handling(100))

        random.shuffle(all_samples)

        split_idx = int(len(all_samples) * 0.9)
        train_data = all_samples[:split_idx]
        val_data = all_samples[split_idx:]

        return {
            "train": {"conversations": train_data},
            "validation": {"conversations": val_data}
        }


if __name__ == "__main__":
    generator = RoofAIDataGenerator()
    dataset = generator.generate_full_dataset()

    with open("training_data.json", "w", encoding="utf-8") as f:
        json.dump(dataset["train"], f, ensure_ascii=False, indent=2)

    with open("validation_data.json", "w", encoding="utf-8") as f:
        json.dump(dataset["validation"], f, ensure_ascii=False, indent=2)

    print(f"\nDataset generated!")
    print(f"   Training samples: {len(dataset['train']['conversations'])}")
    print(f"   Validation samples: {len(dataset['validation']['conversations'])}")
