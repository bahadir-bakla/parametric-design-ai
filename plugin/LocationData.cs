using System.Collections.Generic;

namespace RoofAI
{
    public class CityInfo
    {
        public string Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int Timezone { get; set; }
        public string ClimateZone { get; set; }
        public int HeatingDegreeDays { get; set; }
        public int CoolingDegreeDays { get; set; }
    }

    public static class LocationData
    {
        private static readonly Dictionary<string, CityInfo> Cities = new Dictionary<string, CityInfo>
        {
            ["istanbul"] = new CityInfo { Name = "Istanbul", Latitude = 41.0082, Longitude = 28.9784, Timezone = 3, ClimateZone = "Csa", HeatingDegreeDays = 1750, CoolingDegreeDays = 450 },
            ["ankara"] = new CityInfo { Name = "Ankara", Latitude = 39.9334, Longitude = 32.8597, Timezone = 3, ClimateZone = "Dsb", HeatingDegreeDays = 2700, CoolingDegreeDays = 300 },
            ["izmir"] = new CityInfo { Name = "Izmir", Latitude = 38.4237, Longitude = 27.1428, Timezone = 3, ClimateZone = "Csa", HeatingDegreeDays = 1200, CoolingDegreeDays = 600 },
            ["antalya"] = new CityInfo { Name = "Antalya", Latitude = 36.8969, Longitude = 30.7133, Timezone = 3, ClimateZone = "Csa", HeatingDegreeDays = 800, CoolingDegreeDays = 750 },
            ["bursa"] = new CityInfo { Name = "Bursa", Latitude = 40.1826, Longitude = 29.0665, Timezone = 3, ClimateZone = "Csa", HeatingDegreeDays = 1900, CoolingDegreeDays = 350 },
            ["kayseri"] = new CityInfo { Name = "Kayseri", Latitude = 38.7312, Longitude = 35.4787, Timezone = 3, ClimateZone = "Dsb", HeatingDegreeDays = 3000, CoolingDegreeDays = 200 },
            ["trabzon"] = new CityInfo { Name = "Trabzon", Latitude = 41.0027, Longitude = 39.7168, Timezone = 3, ClimateZone = "Cfa", HeatingDegreeDays = 1800, CoolingDegreeDays = 150 },
            ["erzurum"] = new CityInfo { Name = "Erzurum", Latitude = 39.9055, Longitude = 41.2658, Timezone = 3, ClimateZone = "Dfb", HeatingDegreeDays = 4500, CoolingDegreeDays = 50 },
            ["diyarbakir"] = new CityInfo { Name = "Diyarbakir", Latitude = 37.9144, Longitude = 40.2306, Timezone = 3, ClimateZone = "Csa", HeatingDegreeDays = 2200, CoolingDegreeDays = 600 },
            ["konya"] = new CityInfo { Name = "Konya", Latitude = 37.8746, Longitude = 32.4932, Timezone = 3, ClimateZone = "Bsk", HeatingDegreeDays = 2800, CoolingDegreeDays = 250 },
            ["gaziantep"] = new CityInfo { Name = "Gaziantep", Latitude = 37.0662, Longitude = 37.3833, Timezone = 3, ClimateZone = "Csa", HeatingDegreeDays = 2000, CoolingDegreeDays = 500 },
            ["samsun"] = new CityInfo { Name = "Samsun", Latitude = 41.2867, Longitude = 36.33, Timezone = 3, ClimateZone = "Cfa", HeatingDegreeDays = 1700, CoolingDegreeDays = 200 }
        };

        public static CityInfo GetCity(string name)
        {
            string key = name.ToLower()
                .Replace("i", "i")
                .Replace("ş", "s")
                .Replace("ç", "c")
                .Replace("ü", "u")
                .Replace("ö", "o")
                .Replace("ğ", "g")
                .Trim();

            if (Cities.TryGetValue(key, out var city))
                return city;

            foreach (var kvp in Cities)
            {
                if (kvp.Value.Name.ToLower().Contains(key) || key.Contains(kvp.Key))
                    return kvp.Value;
            }

            return Cities["istanbul"];
        }

        public static IEnumerable<string> GetAllCityNames()
        {
            foreach (var city in Cities.Values)
                yield return city.Name;
        }

        public static bool CityExists(string name)
        {
            string key = name.ToLower().Trim();
            return Cities.ContainsKey(key);
        }
    }
}
