using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;

namespace CustomExporterAdnMeshJson.GML
{
    public class GmlSiteComponentExportElement : GmlExportElementBase
    {
        public GmlSiteComponentExportElement(FeatureType featureType, Element thisElement, View3D activeView) : base(featureType, thisElement, activeView)
        {



        }

        public override void HandleGeometry()
        {
            var faces = new List<Face>();
            var foundMeshes = new List<Mesh>(); 
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
                        if(sG is Mesh m)
                        {
                            foundMeshes.Add(m);
                        }
                    }
                }
                else if (item is Solid solid)
                {
                    faces.AddRange(GetFaces(solid));
                }
            }
            MeshedFaces =  faces.Select(face => GetMesh(face)).ToList();
            if (foundMeshes.Any())
                MeshedFaces.AddRange(foundMeshes);
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
            base.PopulateElementPropertyData();
            
            var level = ThisElement.LookupParameter("Base Level");
            var levelid = level?.AsElementId();
            if (levelid != null && levelid != ElementId.InvalidElementId)
            {
                Properties.Add(new PropertiesData("LevelId", levelid.IntegerValue.ToString(), typeof(int)));
                var levelname = _document.GetElement(levelid).Name;
                Properties.Add(new PropertiesData("Level", levelname, typeof(string)));
            }
            
            
            AddColorAndTransparancyData();

            return true;
        }
    }
}
