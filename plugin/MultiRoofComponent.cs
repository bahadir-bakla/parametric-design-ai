using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace RoofAI
{
    public class MultiRoofComponent : GH_Component
    {
        public MultiRoofComponent()
          : base("RoofAI Multi Roof", "MultiAI",
              "Birden fazla cati bolumunu birlestir",
              "RoofAI", "Design")
        { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Roof Types", "Types",
                "Cati tipleri (orn: gable, hip)", GH_ParamAccess.list);
            pManager.AddNumberParameter("Lengths", "Len",
                "Her bolum uzunlugu (m)", GH_ParamAccess.list);
            pManager.AddNumberParameter("Widths", "Wid",
                "Her bolum genisligi (m)", GH_ParamAccess.list);
            pManager.AddNumberParameter("Pitches", "Pitch",
                "Her bolum egimi (derece)", GH_ParamAccess.list);
            pManager.AddNumberParameter("Overhang", "OH",
                "Sacak genisligi (m)", GH_ParamAccess.item, 0.5);
            pManager.AddNumberParameter("Spacing", "Spc",
                "Bolumler arasi bosluk (m)", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("Orientation", "Ori",
                "Genel yon (derece)", GH_ParamAccess.item, 0);
            pManager.AddBooleanParameter("Run", "Run",
                "Olustur", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("All Geometry", "All",
                "Tum cati geometrileri", GH_ParamAccess.list);
            pManager.AddBrepParameter("Sections", "Sec",
                "Her bolum ayri", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Total Area", "Area",
                "Toplam cati alani (m2)", GH_ParamAccess.item);
            pManager.AddTextParameter("Report", "Report",
                "Cati raporu", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var types = new List<string>();
            var lengths = new List<double>();
            var widths = new List<double>();
            var pitches = new List<double>();
            double overhang = 0.5;
            double spacing = 0;
            double orientation = 0;
            bool run = false;

            DA.GetDataList(0, types);
            DA.GetDataList(1, lengths);
            DA.GetDataList(2, widths);
            DA.GetDataList(3, pitches);
            DA.GetData(4, ref overhang);
            DA.GetData(5, ref spacing);
            DA.GetData(6, ref orientation);
            DA.GetData(7, ref run);

            if (!run || types.Count == 0) return;

            int count = types.Count;
            while (lengths.Count < count) lengths.Add(lengths.Count > 0 ? lengths[lengths.Count - 1] : 20);
            while (widths.Count < count) widths.Add(widths.Count > 0 ? widths[widths.Count - 1] : 15);
            while (pitches.Count < count) pitches.Add(pitches.Count > 0 ? pitches[pitches.Count - 1] : 30);

            var allGeometry = new List<Brep>();
            var sectionTree = new Grasshopper.DataTree<Brep>();
            double totalArea = 0;
            string report = "=== Coklu Cati Raporu ===\n";

            double offsetX = 0;

            for (int i = 0; i < count; i++)
            {
                try
                {
                    var sectionBreps = GeometryEngine.GenerateRoof(
                        types[i], lengths[i], widths[i], pitches[i], overhang, 0);

                    double sectionOffset = offsetX + lengths[i] / 2.0;

                    var xform = Transform.Translation(new Vector3d(sectionOffset, 0, 0));
                    foreach (var brep in sectionBreps)
                        brep.Transform(xform);

                    if (Math.Abs(orientation) > 0.01)
                    {
                        var rotXform = Transform.Rotation(
                            orientation * Math.PI / 180.0, Vector3d.ZAxis, Point3d.Origin);
                        foreach (var brep in sectionBreps)
                            brep.Transform(rotXform);
                    }

                    double sectionArea = GeometryEngine.CalculateRoofArea(sectionBreps);
                    totalArea += sectionArea;

                    allGeometry.AddRange(sectionBreps);

                    var path = new Grasshopper.Kernel.Data.GH_Path(i);
                    sectionTree.AddRange(sectionBreps, path);

                    report += $"\nBolum {i + 1}: {types[i]}\n" +
                             $"  Boyut: {lengths[i]}m x {widths[i]}m\n" +
                             $"  Egim: {pitches[i]} derece\n" +
                             $"  Alan: {sectionArea:F1} m2\n";

                    offsetX += lengths[i] + spacing;
                }
                catch (Exception ex)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                        $"Bolum {i + 1} hatasi: {ex.Message}");
                    report += $"\nBolum {i + 1}: HATA - {ex.Message}\n";
                }
            }

            report += $"\n---\nToplam Bolum: {count}\n";
            report += $"Toplam Alan: {totalArea:F1} m2\n";
            report += $"Toplam Uzunluk: {offsetX:F1} m\n";

            DA.SetDataList(0, allGeometry);
            DA.SetDataTree(1, sectionTree);
            DA.SetData(2, Math.Round(totalArea, 2));
            DA.SetData(3, report);
        }

        protected override System.Drawing.Bitmap Icon => null;

        public override Guid ComponentGuid =>
            new Guid("D4E5F6A7-B8C9-0123-DEF0-234567890123");
    }
}
