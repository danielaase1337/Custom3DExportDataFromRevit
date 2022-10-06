using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;

namespace CustomExporterAdnMeshJson.GML
{
    public class GmlStairExportElement : GmlExportElementBase
    {
        public GmlStairExportElement(FeatureType featureType, Element thisElement, View3D activeView) : base(featureType, thisElement, activeView)
        {



        }

        public override void HandleGeometry()
        {
            var faces = new List<Face>(); 
            foreach (var item in GeometryElement)
            {
                if (item is GeometryInstance inst)
                {
                    var symGeom = inst.GetSymbolGeometry(inst.Transform);
                    foreach (var sG in symGeom)
                    {
                        if (sG is Solid s)
                        {
                           faces.AddRange(GetFaces(s));
                        }
                    }
                }
                else if (item is Solid solid)
                {
                    faces.AddRange(GetFaces(solid));
                }
            }
            MeshedFaces =  faces.Select(face => GetMesh(face)).ToList();
        }
        protected override void AddColorAndTransparancyData()
        {
            var materials = ThisElement.GetMaterialIds(false);
            var colorstring = string.Empty;
            foreach (var nat in materials.Select(f => _document.GetElement(f)))
            {
                if (nat is Material mat)
                {
                    var transparancy = mat.Transparency;
                    colorstring = $"#{mat.Color.Red:X2}{mat.Color.Blue:X2}{mat.Color.Green}{transparancy:X2}";
                    Properties.Add(new PropertiesData("Color", colorstring, typeof(string)));
                    break;
                }
            }
        }
        public override bool PopulateElementPropertyData()
        {
            Properties.Add(new PropertiesData("ElementId", ThisElement.Id.IntegerValue.ToString(), typeof(int)));
            Properties.Add(new PropertiesData("Name", ThisElement.Name, typeof(string)));
            var level = ThisElement.LookupParameter("Base Level");
            var levelid = level?.AsElementId();
            if (levelid != ElementId.InvalidElementId)
            {
                var levelname = _document.GetElement(levelid).Name;
                Properties.Add(new PropertiesData("Level", levelname, typeof(string)));
                Properties.Add(new PropertiesData("LevelId", levelid.IntegerValue.ToString(), typeof(int)));
            }
            base.PopulateElementPropertyData();


            AddColorAndTransparancyData();

            return true;
        }
    }
}
