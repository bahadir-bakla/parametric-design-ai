#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
RoofAI Test Suite - Rehberdeki formatta
Tüm unit testlerini çalıştırır
"""

import json
import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))


class TestDataGenerator(unittest.TestCase):
    
    @classmethod
    def setUpClass(cls):
        from data.data_generator import RoofAIDataGenerator
        cls.generator = RoofAIDataGenerator()
    
    def test_basic_geometry_generation(self):
        """Basic geometry samples test"""
        samples = self.generator.generate_basic_geometry(10)
        self.assertEqual(len(samples), 10)
        
        for s in samples:
            self.assertIn("id", s)
            self.assertIn("category", s)
            self.assertIn("messages", s)
            self.assertEqual(len(s["messages"]), 2)
            
            response = json.loads(s["messages"][1]["content"])
            self.assertEqual(response["action"], "create")
            self.assertIn(response["roof_type"], ["gable", "hip", "gambrel", "shed", "flat"])
    
    def test_modifications_generation(self):
        """Modification samples test"""
        samples = self.generator.generate_modifications(10)
        self.assertEqual(len(samples), 10)
        
        for s in samples:
            response = json.loads(s["messages"][1]["content"])
            self.assertEqual(response["action"], "update")
            self.assertIn("parameters", response)
    
    def test_orientation_generation(self):
        """Orientation samples test"""
        samples = self.generator.generate_orientation(10)
        self.assertEqual(len(samples), 10)
        
        for s in samples:
            response = json.loads(s["messages"][1]["content"])
            self.assertEqual(response["action"], "update")
            params = response.get("parameters", {})
            self.assertTrue("orientation" in params or "primary_direction" in params)
    
    def test_light_analysis_generation(self):
        """Light analysis samples test"""
        samples = self.generator.generate_light_analysis(10)
        self.assertEqual(len(samples), 10)
        
        for s in samples:
            response = json.loads(s["messages"][1]["content"])
            self.assertEqual(response["action"], "analyze_light")
            self.assertIn("location", response)
            self.assertIn("analysis_type", response)
    
    def test_skylight_generation(self):
        """Skylight optimization samples test"""
        samples = self.generator.generate_skylight_optimization(10)
        self.assertEqual(len(samples), 10)
        
        for s in samples:
            response = json.loads(s["messages"][1]["content"])
            self.assertEqual(response["action"], "optimize_skylights")
            self.assertIn("room_type", response)
            self.assertIn("count", response)
    
    def test_error_handling_generation(self):
        """Error handling samples test"""
        samples = self.generator.generate_error_handling(10)
        self.assertEqual(len(samples), 10)
        
        for s in samples:
            response = json.loads(s["messages"][1]["content"])
            self.assertEqual(response["action"], "clarify")
            self.assertIn("message", response)
    
    def test_dataset_split(self):
        """Train/validation split test"""
        dataset = self.generator.generate_full_dataset()
        train_count = len(dataset["train"]["conversations"])
        val_count = len(dataset["validation"]["conversations"])
        total = train_count + val_count
        
        # %90 train, %10 validation
        self.assertGreater(total, 1000)
        self.assertAlmostEqual(train_count / total, 0.9, delta=0.05)
    
    def test_unique_ids(self):
        """Unique ID test"""
        dataset = self.generator.generate_full_dataset()
        all_samples = dataset["train"]["conversations"] + dataset["validation"]["conversations"]
        ids = [s["id"] for s in all_samples]
        self.assertEqual(len(ids), len(set(ids)))


class TestDataFiles(unittest.TestCase):
    
    def test_training_data_exists(self):
        """Training data file exists"""
        self.assertTrue(os.path.exists("data/training_data.json"))
    
    def test_validation_data_exists(self):
        """Validation data file exists"""
        self.assertTrue(os.path.exists("data/validation_data.json"))
    
    def test_training_data_format(self):
        """Training data format validation"""
        with open("data/training_data.json", "r", encoding="utf-8") as f:
            data = json.load(f)
        
        self.assertIn("conversations", data)
        self.assertGreater(len(data["conversations"]), 900)
        
        # İlk örneği kontrol et
        sample = data["conversations"][0]
        self.assertIn("id", sample)
        self.assertIn("category", sample)
        self.assertIn("messages", sample)
    
    def test_validation_data_format(self):
        """Validation data format validation"""
        with open("data/validation_data.json", "r", encoding="utf-8") as f:
            data = json.load(f)
        
        self.assertIn("conversations", data)
        self.assertGreater(len(data["conversations"]), 100)


class TestResponseParsing(unittest.TestCase):
    
    def test_parse_create_response(self):
        """Parse create action response"""
        response = '{"action": "create", "roof_type": "gable", "length": 20, "width": 15, "pitch_angle": 30}'
        parsed = json.loads(response)
        self.assertEqual(parsed["action"], "create")
        self.assertEqual(parsed["roof_type"], "gable")
    
    def test_parse_update_response(self):
        """Parse update action response"""
        response = '{"action": "update", "parameters": {"eave_overhang": 0.8}}'
        parsed = json.loads(response)
        self.assertEqual(parsed["action"], "update")
        self.assertEqual(parsed["parameters"]["eave_overhang"], 0.8)
    
    def test_parse_light_analysis_response(self):
        """Parse light analysis response"""
        response = '{"action": "analyze_light", "location": "istanbul", "hour": 12, "analysis_type": "shadow"}'
        parsed = json.loads(response)
        self.assertEqual(parsed["action"], "analyze_light")
        self.assertEqual(parsed["location"], "istanbul")


class TestCityDatabase(unittest.TestCase):
    
    def setUp(self):
        self.db_path = "data/city_database.json"
        with open(self.db_path, "r", encoding="utf-8") as f:
            self.data = json.load(f)
    
    def test_city_database_loads(self):
        """City database loads correctly"""
        self.assertIn("cities", self.data)
        self.assertGreater(len(self.data["cities"]), 0)
    
    def test_city_fields(self):
        """Cities have required fields"""
        for city_key, city in self.data["cities"].items():
            self.assertIn("latitude", city)
            self.assertIn("longitude", city)
            self.assertIn("timezone", city)
            self.assertTrue(-90 <= city["latitude"] <= 90)
            self.assertTrue(-180 <= city["longitude"] <= 180)


def run_tests():
    """Run all tests and print summary"""
    loader = unittest.TestLoader()
    suite = unittest.TestSuite()
    
    # Add all test classes
    suite.addTests(loader.loadTestsFromTestCase(TestDataGenerator))
    suite.addTests(loader.loadTestsFromTestCase(TestDataFiles))
    suite.addTests(loader.loadTestsFromTestCase(TestResponseParsing))
    suite.addTests(loader.loadTestsFromTestCase(TestCityDatabase))
    
    runner = unittest.TextTestRunner(verbosity=2)
    result = runner.run(suite)
    
    # Print summary
    print("\n" + "="*70)
    print("TEST SUMMARY")
    print("="*70)
    print(f"Tests run: {result.testsRun}")
    print(f"Successes: {result.testsRun - len(result.failures) - len(result.errors)}")
    print(f"Failures: {len(result.failures)}")
    print(f"Errors: {len(result.errors)}")
    
    if result.wasSuccessful():
        print("\n✅ ALL TESTS PASSED!")
        return 0
    else:
        print("\n❌ SOME TESTS FAILED")
        return 1


if __name__ == "__main__":
    sys.exit(run_tests())
