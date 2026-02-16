using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace RoofAI
{
    public class SkylightComponent : GH_Component
    {
        public SkylightComponent()
          : base("RoofAI Skylight", "SkyAI",
              "Cati penceresi optimizasyonu ve yerlestirme",
              "RoofAI", "Analysis")
        { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Roof Geometry", "Roof",
                "Pencere yerlestirilecek cati", GH_ParamAccess.list);
            pManager.AddTextParameter("Room Type", "Room",
                "Oda tipi (salon, mutfak, yatak odasi, calisma odasi, banyo)",
                GH_ParamAccess.item, "salon");
            pManager.AddIntegerParameter("Count", "Count",
                "Pencere sayisi", GH_ParamAccess.item, 4);
            pManager.AddTextParameter("Goal", "Goal",
                "Optimizasyon hedefi (maximize_daylight, minimize_glare, balanced)",
                GH_ParamAccess.item, "balanced");
            pManager.AddTextParameter("City", "City",
                "Sehir (yon hesabi icin)", GH_ParamAccess.item, "Istanbul");
            pManager.AddBooleanParameter("Run", "Run",
                "Optimizasyonu calistir", GH_ParamAccess.item, false);

            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Skylights", "Sky",
                "Pencere geometrileri", GH_ParamAccess.list);
            pManager.AddPointParameter("Centers", "Ctr",
                "Pencere merkez noktalari", GH_ParamAccess.list);
            pManager.AddNumberParameter("Daylight Factors", "DF",
                "Her pencere icin gun isigi faktoru", GH_ParamAccess.list);
            pManager.AddNumberParameter("Total Glazing Area", "GlzArea",
                "Toplam cam alani (m2)", GH_ParamAccess.item);
            pManager.AddNumberParameter("Glazing Ratio", "GlzRatio",
                "Cam/zemin orani", GH_ParamAccess.item);
            pManager.AddTextParameter("Report", "Report",
                "Optimizasyon raporu", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var roofBreps = new List<Brep>();
            string roomType = "salon";
            int count = 4;
            string goal = "balanced";
            string city = "Istanbul";
            bool run = false;

            DA.GetDataList(0, roofBreps);
            DA.GetData(1, ref roomType);
            DA.GetData(2, ref count);
            DA.GetData(3, ref goal);
            DA.GetData(4, ref city);
            DA.GetData(5, ref run);

            if (!run || roofBreps.Count == 0) return;

            count = Math.Max(1, Math.Min(count, 20));

            var cityInfo = LocationData.GetCity(city);

            var result = SkylightOptimizer.OptimizeSkylights(
                roofBreps, roomType, count, goal, cityInfo.Latitude);

            var skylightBreps = new List<Brep>();
            var centers = new List<Point3d>();
            var daylightFactors = new List<double>();

            foreach (var skylight in result.Skylights)
            {
                if (skylight.Geometry != null)
                    skylightBreps.Add(skylight.Geometry);
                centers.Add(skylight.Center);
                daylightFactors.Add(Math.Round(skylight.DaylightFactor, 2));
            }

            DA.SetDataList(0, skylightBreps);
            DA.SetDataList(1, centers);
            DA.SetDataList(2, daylightFactors);
            DA.SetData(3, Math.Round(result.TotalGlazingArea, 2));
            DA.SetData(4, Math.Round(result.GlazingToFloorRatio, 4));

            string report = $"=== Pencere Optimizasyonu Raporu ===\n" +
                           $"Oda Tipi: {roomType}\n" +
                           $"Sehir: {cityInfo.Name}\n" +
                           $"Hedef: {goal}\n" +
                           $"---\n" +
                           $"Yerlestirilen Pencere: {result.Skylights.Count}/{count}\n" +
                           $"Pencere Boyutu: {result.Skylights[0]?.Width:F2}m x {result.Skylights[0]?.Height:F2}m\n" +
                           $"Toplam Cam Alani: {result.TotalGlazingArea:F2} m2\n" +
                           $"Cam/Cati Orani: {result.GlazingToFloorRatio * 100:F1}%\n" +
                           $"Ortalama Gun Isigi Faktoru: {result.AverageDaylightFactor:F1}%\n" +
                           $"---\n";

            for (int i = 0; i < result.Skylights.Count; i++)
            {
                var s = result.Skylights[i];
                report += $"Pencere {i + 1}: DF={s.DaylightFactor:F1}%, " +
                         $"Konum=({s.Center.X:F1}, {s.Center.Y:F1}, {s.Center.Z:F1})\n";
            }

            DA.SetData(5, report);
        }

        protected override System.Drawing.Bitmap Icon => null;

        public override Guid ComponentGuid =>
            new Guid("C3D4E5F6-A7B8-9012-CDEF-123456789012");
    }
}
