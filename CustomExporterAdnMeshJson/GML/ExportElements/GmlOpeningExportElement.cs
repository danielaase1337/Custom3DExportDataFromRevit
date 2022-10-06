using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;
using Color = Autodesk.Revit.DB.Color;

namespace CustomExporterAdnMeshJson.GML
{
    internal class GmlOpeningExportElement : GmlExportElementBase
    {
        public GmlOpeningExportElement(FeatureType featureType, Element el, View3D view3D) : base(featureType, el, view3D)
        {

            var hostParameter = el.GetParameter(ParameterTypeId.HostIdParam);
            if (hostParameter != null)
                HostId = hostParameter.AsElementId().IntegerValue;

            ChildFeatures = new List<IGmlExportElement>();
        }


        private Solid _thisSolid;
        public override void HandleGeometry()
        {
            foreach (var item in GeometryElement)
            {
                if (item is GeometryInstance instanse)
                {
                    var symbolGeom = instanse.GetSymbolGeometry(instanse.Transform);
                    foreach (var sGeom in symbolGeom)
                    {
                        if (sGeom is Solid s)
                        {
                            if (s.GraphicsStyleId == ElementId.InvalidElementId) continue;
                            var gStyle = ThisElement.Document.GetElement(s.GraphicsStyleId) as GraphicsStyle;
                            var catName = gStyle.GraphicsStyleCategory.Name.ToLower();
                            if (catName.Equals("karm"))
                            {//main
                                _thisSolid = s;
                                var facesInMain = GetFaces(s);
                                MeshedFaces = facesInMain.Select(face => GetMesh(face)).ToList();
                            }
                            else if (catName.Equals("dørblad"))
                            {
                                var childcomponent = new ChildOpeningExportElement(FeatureType.OpeningSurface, s, ThisElement, activeView);
                                childcomponent.HandleGeometry();
                                childcomponent.PopulateElementPropertyData();
                                childcomponent.HostId = ThisElement.Id.IntegerValue;
                                ChildFeatures.Add(childcomponent);
                            }
                            else if (catName.Equals("glass"))
                            {//child - glass
                                var childcomponent = new ChildOpeningExportElement(FeatureType.GlassSurface, s, ThisElement, activeView);
                                childcomponent.HandleGeometry();
                                childcomponent.HostId = ThisElement.Id.IntegerValue;
                                childcomponent.PopulateElementPropertyData();
                                ChildFeatures.Add(childcomponent);
                            }
                            else if (catName.Equals("ramme"))
                            {//åpningsvindu - aka det vinduet står i 
                                var childcomponent = new ChildOpeningExportElement(FeatureType.OpeningFrame, s, ThisElement, activeView);
                                childcomponent.HandleGeometry();
                                childcomponent.PopulateElementPropertyData();
                                childcomponent.HostId = ThisElement.Id.IntegerValue;
                                ChildFeatures.Add(childcomponent);
                            }
                            else if (catName.Equals("panel"))
                            {
                                var childcomponent = new ChildOpeningExportElement(FeatureType.OpeningSurface, s, ThisElement, activeView);
                                childcomponent.HandleGeometry();
                                childcomponent.PopulateElementPropertyData();
                                childcomponent.HostId = ThisElement.Id.IntegerValue;
                                ChildFeatures.Add(childcomponent);
                            }

                        }

                    }
                }

               

            }
        }
        protected override void AddColorAndTransparancyData()
        {
            if (_thisSolid == null) return;

            var gStyle = _document.GetElement(_thisSolid.GraphicsStyleId) as GraphicsStyle;
            if (gStyle != null)
            {
                var material = gStyle.GraphicsStyleCategory?.Material;
                var color = material?.Color;
                var transparency = material?.Transparency;
                if (color == null)
                {
                    color = new Color(128, 128, 128);
                }

                var colorValue = $"#{color.Red:X2}{color.Green:X2}{color.Blue:X2}";
                if (transparency != null)
                    colorValue += $"{2.55 * transparency.Value}:X2";
                Properties.Add(new PropertiesData("Color", colorValue, typeof(string)));
            }
        }
        public override bool PopulateElementPropertyData()
        {
            Properties.Add(new PropertiesData("ElementId", ThisElement.Id.IntegerValue.ToString(), typeof(int)));
            Properties.Add(new PropertiesData("Name", ThisElement.Name, typeof(string)));
            AddLevelAndLevelId();
            Properties.Add(new PropertiesData("HostId", HostId.ToString(), typeof(int)));
            AddParameterData("Area");
            base.PopulateElementPropertyData();

            AddColorAndTransparancyData();

            return true;

        }
    }
}
