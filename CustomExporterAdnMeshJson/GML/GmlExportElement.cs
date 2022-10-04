using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;

namespace CustomExporterAdnMeshJson.GML
{
    public interface IGmlExportElement
    {
        void HandleGeometry();
        bool PopulateElementPropertyData();
        List<PropertiesData> Properties { get; set; }
        string FeatureName { get; }
        Element ThisElement { get; }
        List<Mesh> MeshedFaces { get; set; }
        int HostId { get; set; }
        string UniqId { get; }
    }
    public abstract class GmlElementBase : IGmlExportElement
    {
        protected readonly View3D activeView;
        protected Document _document;
        public GmlElementBase(FeatureType featureType, Element thisElement, View3D activeView)
        {
            FeatureType = featureType;
            FeatureName = GetFeatureName(featureType);
            ThisElement = thisElement;
            _document = ThisElement.Document;
            UniqId = "_" + ThisElement.UniqueId.ToString();
            this.activeView = activeView;
            Properties = new List<PropertiesData>();
            GeometryElement = thisElement.get_Geometry(new Options() { IncludeNonVisibleObjects = false, View = activeView });
        }
        public FeatureType FeatureType { get; }
        public string FeatureName { get; }
        public List<PropertiesData> Properties { get; set; }
        public Element ThisElement { get; }
        public GeometryElement GeometryElement { get; set; }
        public List<Mesh> MeshedFaces { get; set; }

        public int HostId { get; set; }

        public string UniqId { get; protected set; }

        internal virtual List<Face> GetFaces(Solid solid)
        {
            var faces = new List<Face>();
            if (solid == null) return faces;

            var faceArray = solid.Faces;
            var itt = faceArray.ForwardIterator();
            while (itt.MoveNext())
            {
                if (itt.Current is Face face)
                    faces.Add(face);
            }
            return faces;
        }
        public abstract void HandleGeometry();
        public virtual bool PopulateElementPropertyData()
        {
            try
            {
                var properties = GetAllPropertiesOnElement(ThisElement);
                var asString = JsonConvert.SerializeObject(properties);
                Properties.Add(new PropertiesData("CustomData", asString, "Json"));

            }
            catch (Exception e)
            {
                Debug.Write(e.Message);
                return false;
            }
            return true;
        }
        private string GetFeatureName(FeatureType featureType)
        {
            switch (featureType)
            {
                case FeatureType.Roof:
                    return "Roof";
                case FeatureType.Wall:
                    return "Wall";
                case FeatureType.OpeningFrame:
                    return "OpeningFrame";
                case FeatureType.OpeningSurface:
                    return "OpeningSurface";
                case FeatureType.Frame:
                    return "Frame";
                case FeatureType.GlassSurface:
                    return "GlassSurface";
                case FeatureType.Door:
                    return "Door";
                case FeatureType.Window:
                    return "Window";
                case FeatureType.Floor:
                    return "Floor";
                case FeatureType.Stairs:
                    return "Stair";

            }
            throw new NotSupportedException("Not supported type catched");
        }
        protected Mesh GetMesh(Face face)
        {
            return face.Triangulate(1);
        }
        protected List<PropertiesData> GetAllPropertiesOnElement(Element el)
        {
            var propertiesList = new List<PropertiesData>();

            var itterator = el.ParametersMap.ForwardIterator();
            while (itterator.MoveNext())
            {
                var parameter = itterator.Current as Parameter;
                if (parameter != null)
                {
                    if (parameter.HasValue)
                    {
                        var propData = new PropertiesData(parameter, el.Document);
                        if (propData.InitDataClass())
                            propertiesList.Add(propData);
                    }
                }
            }
            return propertiesList;

        }
        protected void AddLevelAndLevelId()
        {
            var levelid = ThisElement.LevelId;
            if (levelid == ElementId.InvalidElementId) return;
            var level = ThisElement.Document.GetElement(levelid);
            var levelname = level.Name;
            Properties.Add(new PropertiesData("Level", levelname, typeof(string)));
            Properties.Add(new PropertiesData("LevelId", levelid.IntegerValue.ToString(), typeof(int)));

        }
        protected void AddParameterData(string parmeterName)
        {
            var area = ThisElement.LookupParameter(parmeterName);
            if (area != null)
            {
                var propData = new PropertiesData(area, ThisElement.Document);
                if (propData.InitDataClass())
                    Properties.Add(propData);
            }
        }
        protected virtual void AddColorAndTransparancyData()
        {
            string colorValue = string.Empty;
            int transparencyValue = 0;
            if (FeatureType == FeatureType.Wall)
            {
                var wallType = _document.GetElement(ThisElement.GetTypeId()) as WallType;
                if (wallType != null)
                {

                    var compound = wallType.GetCompoundStructure();
                    if(compound != null)
                    {
                        var layers = compound.GetLayers();
                        var outerfinishLayer = layers[0];
                        var material = _document.GetElement(outerfinishLayer.MaterialId) as Material;
                        var color = material.Color;
                        colorValue = $"#{color.Red:X2}{color.Green:X2}{color.Blue:X2}";
                    }
                    

                }

            }
            else if (FeatureType == FeatureType.Floor)
            {
                var floortype = _document.GetElement(ThisElement.GetTypeId()) as FloorType;
                if (floortype != null)
                {

                    var compound = floortype.GetCompoundStructure();
                    var layers = compound.GetLayers();
                    var outerfinishLayer = layers[0];
                    var material = _document.GetElement(outerfinishLayer.MaterialId) as Material;
                    var color = material.Color;
                    colorValue = $"#{color.Red:X2}{color.Green:X2}{color.Blue:X2}";

                }
            }
            else if (FeatureType == FeatureType.Roof)
            {
                var roofType = _document.GetElement(ThisElement.GetTypeId()) as RoofType;
                if (roofType != null)
                {

                    var compound = roofType.GetCompoundStructure();
                    var layers = compound.GetLayers();
                    var outerfinishLayer = layers[0];
                    var material = _document.GetElement(outerfinishLayer.MaterialId) as Material;
                    var color = material.Color;
                    colorValue = $"#{color.Red:X2}{color.Green:X2}{color.Blue:X2}";

                }
            }
            else if (FeatureType == FeatureType.Stairs)
            {


            }

            Properties.Add(new PropertiesData("Color", colorValue, typeof(string)));
            Properties.Add(new PropertiesData("Transparency", transparencyValue.ToString(), typeof(int)));

        }
    }

    internal class GmlHostingSurfaceElement : GmlElementBase
    {
        public GmlHostingSurfaceElement(FeatureType featureType, Element el, View3D view3D) : base(featureType, el, view3D)
        {
            MeshedFaces = new List<Mesh>();
            Properties = new List<PropertiesData>();
            HostId = -1;
        }
        public override bool PopulateElementPropertyData()
        {//Aka hente properties.. 
            Properties.Add(new PropertiesData("ElementId", ThisElement.Id.IntegerValue.ToString(), typeof(int)));
            Properties.Add(new PropertiesData("Name", ThisElement.Name, typeof(string)));
            AddLevelAndLevelId();
            Properties.Add(new PropertiesData("HostId", HostId.ToString(), typeof(int)));
            AddParameterData("Area");
            base.PopulateElementPropertyData();
            //add other data 

            AddColorAndTransparancyData();
            return true;
        }





        public override void HandleGeometry()
        {
            var faces = new List<Face>();
            foreach (var item in GeometryElement)
            {
                if (item is Solid s)
                {
                    faces.AddRange(GetFaces(s));
                }
                else if (item is GeometryInstance instance)
                {
                    foreach (var symbolGeom in instance.SymbolGeometry)
                    {
                        if (symbolGeom is Solid solid)
                        {
                            faces.AddRange(GetFaces(solid));
                        }
                    }
                }
            }
            var meshes = faces.Select(f => GetMesh(f)).ToList();
            MeshedFaces = meshes;
        }





    }
    internal class ChildOpeningExportElement : GmlElementBase
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
                var transparency = material?.Transparency.ToString();
                var colorValue = $"#{color.Red:X2}{color.Green:X2}{color.Blue:X2}";
                Properties.Add(new PropertiesData("Color", colorValue, typeof(string)));
                Properties.Add(new PropertiesData("Transparency", transparency, typeof(int)));
            }



        }
    }
    internal class OpeningExportElement : GmlElementBase
    {
        public OpeningExportElement(FeatureType featureType, Element el, View3D view3D) : base(featureType, el, view3D)
        {
            var hostParameter = el.GetParameter(ParameterTypeId.HostIdParam);
            if (hostParameter != null)
                HostId = hostParameter.AsElementId().IntegerValue;

            ChildFeatures = new List<IGmlExportElement>();
        }

        internal List<IGmlExportElement> ChildFeatures { get; set; }

        private Solid _thisSolid;
        public override void HandleGeometry()
        {
            foreach (var item in GeometryElement)
            {
                if (item is GeometryInstance instanse)
                {
                    foreach (var sGeom in instanse.SymbolGeometry)
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

                        }

                    }
                }

                //else if (item is GeometryInstance instance)
                //{
                //    foreach (var symbolGeom in instance.SymbolGeometry)
                //    {
                //        if (symbolGeom is Solid solid)
                //        {
                //            var faces = GetFaces(solid);
                //        }
                //    }
                //}

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
                var transparency = material?.Transparency.ToString();
                if (color == null)
                { 
                    color = new Color(255, 255, 255);
                    transparency = "0"; 
                }
                
                var colorValue = $"#{color.Red:X2}{color.Green:X2}{color.Blue:X2}";
                Properties.Add(new PropertiesData("Color", colorValue, typeof(string)));
                Properties.Add(new PropertiesData("Transparency", transparency, typeof(int)));
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
