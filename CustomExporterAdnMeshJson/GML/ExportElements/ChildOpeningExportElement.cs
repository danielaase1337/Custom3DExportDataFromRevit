using Autodesk.Revit.DB;
using System;
using System.Linq;

namespace CustomExporterAdnMeshJson.GML
{
    internal class ChildOpeningExportElement : GmlExportElementBase
    {
        private Solid _thiSolid;

        public ChildOpeningExportElement(FeatureType type, Solid solid, Element parent, View3D view3d) : base(type, parent, view3d)
        {
            _thiSolid = solid;
            UniqId = "_" + Guid.NewGuid().ToString();
            HostId = parent.Id.IntegerValue;

        }
        public override void HandleGeometry()
        {
            if (_thiSolid == null) return;
            var faces = GetFaces(_thiSolid);
            var meshes = faces.Select(f => GetMesh(f)).ToList();
            MeshedFaces = meshes;

        }

        public override bool PopulateElementPropertyData()
        {
            AddColorAndTransparancyData();
            return true;

        }
        protected override void AddColorAndTransparancyData()
        {
            var gStyle = _document.GetElement(_thiSolid.GraphicsStyleId) as GraphicsStyle;
            if (gStyle != null)
            {
                var material = gStyle.GraphicsStyleCategory?.Material;
                var color = material?.Color;
                var transparency = material?.Transparency;
                if (color != null)
                {
                    var colorValue = $"#{color.Red:X2}{color.Green:X2}{color.Blue:X2}";
                    if (transparency != null)
                    {
                        int trans = (int)Math.Round(transparency.Value * 2.55, 0);
                        colorValue += $"{trans:X2}";
                    }
                    Properties.Add(new PropertiesData("Color", colorValue, typeof(string)));
                }
            }



        }
    }
}
