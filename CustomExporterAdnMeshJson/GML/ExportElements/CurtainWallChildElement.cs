using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;
using Color = Autodesk.Revit.DB.Color;

namespace CustomExporterAdnMeshJson.GML
{
    internal class CurtainWallChildElement : GmlExportElementBase
    {
        private Solid _thisSolid = null;
        public CurtainWallChildElement(FeatureType featureType, Element thisElement, View3D activeView) : base(featureType, thisElement, activeView)
        {
            UniqId = "_" + thisElement.UniqueId;
        }
        protected override void AddColorAndTransparancyData()
        {
            if (_thisSolid == null) return;
            if (_thisSolid.GraphicsStyleId == ElementId.InvalidElementId) return;

            var gStyle = _document.GetElement(_thisSolid.GraphicsStyleId) as GraphicsStyle;
            if (gStyle != null)
            {
                var material = gStyle.GraphicsStyleCategory?.Material;
                var color = material?.Color;
                var transparency = material?.Transparency;
                if (color == null)
                {
                    color = new Color(128, 128, 128);
                    transparency = 0;
                }

                var colorValue = $"#{color.Red:X2}{color.Green:X2}{color.Blue:X2}";
                if (transparency != null)
                    colorValue += $"{transparency.Value * 2.55}:X2";
                Properties.Add(new PropertiesData("Color", colorValue, typeof(string)));
            }
        }
        public override void HandleGeometry()
        {
            var faces = new List<Face>();
            foreach (var gObject in GeometryElement)
            {

                if (gObject is GeometryInstance gInst)
                {
                    var gSyumObject = gInst.GetSymbolGeometry(gInst.Transform);
                    foreach (var item in gSyumObject)
                    {
                        if (item is Solid solid)
                        {
                            if (_thisSolid == null && solid.GraphicsStyleId != ElementId.InvalidElementId)
                                _thisSolid = solid;

                            faces.AddRange(GetFaces(solid));
                        }
                    }
                }
                if (gObject is Solid s)
                {
                    if (_thisSolid == null && s.GraphicsStyleId != ElementId.InvalidElementId)
                        _thisSolid = s;

                    faces.AddRange(GetFaces(s));

                }
            }

            var meshes = faces.Select(f => GetMesh(f)).ToList();
            MeshedFaces = meshes;
        }
    }
}
