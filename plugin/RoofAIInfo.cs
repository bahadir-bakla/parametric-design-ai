using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace RoofAI
{
    public class RoofAIInfo : GH_AssemblyInfo
    {
        public override string Name => "RoofAI";
        public override Bitmap Icon => null;
        public override string Description => "AI ile konusarak parametrik cati tasarimi. Dogal dilde komut verin, AI cati geometrisi uretsin.";
        public override Guid Id => new Guid("A0B1C2D3-E4F5-6789-ABCD-EF0123456789");
        public override string AuthorName => "RoofAI Team";
        public override string AuthorContact => "https://github.com/yourusername/RoofAI";
        public override string Version => "1.0.0";
    }
}
