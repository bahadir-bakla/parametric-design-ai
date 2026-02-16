using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace RoofAI
{
    public class LightAnalysisComponent : GH_Component
    {
        public LightAnalysisComponent()
          : base("RoofAI Light Analysis", "LightAI",
              "Cati icin gunes yolu ve golge analizi",
              "RoofAI", "Analysis")
        { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Roof Geometry", "Roof",
                "Analiz edilecek cati geometrisi", GH_ParamAccess.list);
            pManager.AddTextParameter("City", "City",
                "Sehir adi (orn: Istanbul)", GH_ParamAccess.item, "Istanbul");
            pManager.AddIntegerParameter("Month", "Month",
                "Ay (1-12)", GH_ParamAccess.item, 6);
            pManager.AddIntegerParameter("Day", "Day",
                "Gun (1-31)", GH_ParamAccess.item, 21);
            pManager.AddNumberParameter("Hour", "Hour",
                "Saat (0-24)", GH_ParamAccess.item, 12);
            pManager.AddBooleanParameter("Run", "Run",
                "Analizi calistir", GH_ParamAccess.item, false);

            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddVectorParameter("Sun Vector", "SunVec",
                "Gunes yonu vektoru", GH_ParamAccess.item);
            pManager.AddNumberParameter("Sun Altitude", "Alt",
                "Gunes yukseklik acisi (derece)", GH_ParamAccess.item);
            pManager.AddNumberParameter("Sun Azimuth", "Az",
                "Gunes azimut acisi (derece)", GH_ParamAccess.item);
            pManager.AddBrepParameter("Shadow", "Shadow",
                "Golge geometrisi", GH_ParamAccess.list);
            pManager.AddNumberParameter("Shadow Area", "ShArea",
                "Golge alani (m2)", GH_ParamAccess.item);
            pManager.AddCurveParameter("Sun Path", "SunPath",
                "Gunes yolu egrisi", GH_ParamAccess.item);
            pManager.AddNumberParameter("Irradiance", "Irr",
                "Dogrudan isinlanma (W/m2)", GH_ParamAccess.item);
            pManager.AddTextParameter("Report", "Report",
                "Analiz raporu", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var roofBreps = new List<Brep>();
            string city = "Istanbul";
            int month = 6, day = 21;
            double hour = 12;
            bool run = false;

            DA.GetDataList(0, roofBreps);
            DA.GetData(1, ref city);
            DA.GetData(2, ref month);
            DA.GetData(3, ref day);
            DA.GetData(4, ref hour);
            DA.GetData(5, ref run);

            if (!run || roofBreps.Count == 0) return;

            month = Math.Max(1, Math.Min(12, month));
            day = Math.Max(1, Math.Min(31, day));
            hour = Math.Max(0, Math.Min(24, hour));

            var cityInfo = LocationData.GetCity(city);
            int year = DateTime.Now.Year;

            var sunPos = SunPathCalculator.CalculateSunPosition(
                cityInfo.Latitude, cityInfo.Longitude,
                year, month, day, hour, cityInfo.Timezone);

            DA.SetData(0, sunPos.SunVector);
            DA.SetData(1, Math.Round(sunPos.Altitude, 2));
            DA.SetData(2, Math.Round(sunPos.Azimuth, 2));

            if (sunPos.IsAboveHorizon)
            {
                var shadowResult = ShadowAnalyzer.CalculateShadow(roofBreps, sunPos);
                DA.SetDataList(3, shadowResult.ShadowBreps);
                DA.SetData(4, Math.Round(shadowResult.ShadowArea, 2));

                double irradiance = SunPathCalculator.CalculateDirectIrradiance(sunPos.Altitude);
                DA.SetData(6, Math.Round(irradiance, 1));
            }

            var sunPath = SunPathCalculator.CalculateSunPath(
                cityInfo.Latitude, cityInfo.Longitude,
                year, month, day, cityInfo.Timezone);
            var sunCurve = SunPathCalculator.CreateSunPathCurve(sunPath);
            if (sunCurve != null) DA.SetData(5, sunCurve);

            var sunriseSunset = SunPathCalculator.GetSunriseAndSunset(
                cityInfo.Latitude, cityInfo.Longitude,
                year, month, day, cityInfo.Timezone);

            double dailyIrr = SunPathCalculator.CalculateDailyIrradiance(
                cityInfo.Latitude, cityInfo.Longitude,
                year, month, day, cityInfo.Timezone);

            string report = $"=== Isik Analizi Raporu ===\n" +
                           $"Sehir: {cityInfo.Name}\n" +
                           $"Tarih: {day:D2}/{month:D2}/{year}\n" +
                           $"Saat: {hour:F1}\n" +
                           $"---\n" +
                           $"Gunes Yuksekligi: {sunPos.Altitude:F1} derece\n" +
                           $"Gunes Azimutu: {sunPos.Azimuth:F1} derece\n" +
                           $"Gunes Durumu: {(sunPos.IsAboveHorizon ? "Ufuk uzerinde" : "Ufuk altinda")}\n" +
                           $"---\n" +
                           $"Gun Dogumu: {sunriseSunset[0]:F1}\n" +
                           $"Gun Batimi: {sunriseSunset[1]:F1}\n" +
                           $"Gunluk Toplam Isinlanma: {dailyIrr:F0} Wh/m2\n" +
                           $"Anlık Isinlanma: {SunPathCalculator.CalculateDirectIrradiance(sunPos.Altitude):F0} W/m2";

            DA.SetData(7, report);
        }

        protected override System.Drawing.Bitmap Icon => null;

        public override Guid ComponentGuid =>
            new Guid("B2C3D4E5-F6A7-8901-BCDE-F12345678901");
    }
}
