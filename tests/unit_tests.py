import json
import os
import sys
import pytest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))


class TestDataGenerator:
    def setup_method(self):
        from data.data_generator import RoofAIDataGenerator
        self.generator = RoofAIDataGenerator()

    def test_basic_geometry_count(self):
        samples = self.generator.generate_basic_geometry(10)
        assert len(samples) == 10

    def test_basic_geometry_structure(self):
        samples = self.generator.generate_basic_geometry(5)
        for s in samples:
            assert "id" in s
            assert "category" in s
            assert "messages" in s
            assert len(s["messages"]) == 2
            assert s["messages"][0]["role"] == "user"
            assert s["messages"][1]["role"] == "assistant"

    def test_basic_geometry_json_valid(self):
        samples = self.generator.generate_basic_geometry(20)
        for s in samples:
            response = json.loads(s["messages"][1]["content"])
            assert response["action"] == "create"
            assert response["roof_type"] in ["gable", "hip", "gambrel", "shed", "flat"]
            assert isinstance(response["length"], int)
            assert isinstance(response["width"], int)
            assert isinstance(response["pitch_angle"], int)

    def test_modifications_structure(self):
        samples = self.generator.generate_modifications(10)
        for s in samples:
            response = json.loads(s["messages"][1]["content"])
            assert response["action"] == "update"
            assert "parameters" in response

    def test_orientation_structure(self):
        samples = self.generator.generate_orientation(10)
        for s in samples:
            response = json.loads(s["messages"][1]["content"])
            assert response["action"] == "update"
            params = response.get("parameters", {})
            assert "orientation" in params or "primary_direction" in params

    def test_light_analysis_structure(self):
        samples = self.generator.generate_light_analysis(10)
        for s in samples:
            response = json.loads(s["messages"][1]["content"])
            assert response["action"] == "analyze_light"
            assert "location" in response
            assert "analysis_type" in response

    def test_skylight_structure(self):
        samples = self.generator.generate_skylight_optimization(10)
        for s in samples:
            response = json.loads(s["messages"][1]["content"])
            assert response["action"] == "optimize_skylights"
            assert "room_type" in response
            assert "count" in response

    def test_error_handling_structure(self):
        samples = self.generator.generate_error_handling(10)
        for s in samples:
            response = json.loads(s["messages"][1]["content"])
            assert response["action"] == "clarify"
            assert "message" in response

    def test_full_dataset_split(self):
        dataset = self.generator.generate_full_dataset()
        train_count = len(dataset["train"]["conversations"])
        val_count = len(dataset["validation"]["conversations"])
        total = train_count + val_count
        assert total == 800
        assert abs(train_count / total - 0.9) < 0.02

    def test_unique_ids(self):
        dataset = self.generator.generate_full_dataset()
        all_samples = dataset["train"]["conversations"] + dataset["validation"]["conversations"]
        ids = [s["id"] for s in all_samples]
        assert len(ids) == len(set(ids))


class TestResponseParsing:
    def test_parse_create_response(self):
        response = '{"action": "create", "roof_type": "gable", "length": 20, "width": 15, "pitch_angle": 30}'
        parsed = json.loads(response)
        assert parsed["action"] == "create"
        assert parsed["roof_type"] == "gable"

    def test_parse_update_response(self):
        response = '{"action": "update", "parameters": {"eave_overhang": 0.8}}'
        parsed = json.loads(response)
        assert parsed["action"] == "update"
        assert parsed["parameters"]["eave_overhang"] == 0.8

    def test_parse_light_analysis_response(self):
        response = '{"action": "analyze_light", "location": "istanbul", "hour": 12, "analysis_type": "shadow"}'
        parsed = json.loads(response)
        assert parsed["action"] == "analyze_light"
        assert parsed["location"] == "istanbul"

    def test_extract_json_from_mixed_text(self):
        text = 'Cati olusturuldu. {"action": "create", "roof_type": "gable"} Parametreler yukarida.'
        json_start = text.find("{")
        json_end = text.rfind("}") + 1
        json_str = text[json_start:json_end]
        parsed = json.loads(json_str)
        assert parsed["action"] == "create"


class TestCityDatabase:
    def test_city_database_loads(self):
        db_path = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))),
                               "data", "city_database.json")
        with open(db_path, "r", encoding="utf-8") as f:
            data = json.load(f)
        assert "cities" in data
        assert len(data["cities"]) > 0

    def test_city_has_required_fields(self):
        db_path = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))),
                               "data", "city_database.json")
        with open(db_path, "r", encoding="utf-8") as f:
            data = json.load(f)

        for city_key, city in data["cities"].items():
            assert "latitude" in city
            assert "longitude" in city
            assert "timezone" in city
            assert -90 <= city["latitude"] <= 90
            assert -180 <= city["longitude"] <= 180

    def test_istanbul_coordinates(self):
        db_path = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))),
                               "data", "city_database.json")
        with open(db_path, "r", encoding="utf-8") as f:
            data = json.load(f)

        istanbul = data["cities"]["istanbul"]
        assert 40 < istanbul["latitude"] < 42
        assert 28 < istanbul["longitude"] < 30


if __name__ == "__main__":
    pytest.main([__file__, "-v"])
