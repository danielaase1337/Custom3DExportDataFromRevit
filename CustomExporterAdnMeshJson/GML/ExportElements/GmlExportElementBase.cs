using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Windows.Media;

namespace CustomExporterAdnMeshJson.GML
{
    public abstract class GmlExportElementBase : IGmlExportElement
    {
        protected readonly View3D activeView;
        protected Document _document;
        public GmlExportElementBase(FeatureType featureType, Element thisElement, View3D activeView)
        {
            FeatureType = featureType;
            FeatureName = GetFeatureName(featureType);
            ThisElement = thisElement;
            _document = ThisElement.Document;
            UniqId = "_" + ThisElement.UniqueId.ToString();
            this.activeView = activeView;
            Properties = new List<PropertiesData>();
            GeometryElement = thisElement.get_Geometry(new Options() { IncludeNonVisibleObjects = false, View = activeView });
            ChildFeatures = new List<IGmlExportElement>();
        }
        public FeatureType FeatureType { get; }
        public string FeatureName { get; }
        public List<PropertiesData> Properties { get; set; }
        public Element ThisElement { get; }
        public GeometryElement GeometryElement { get; set; }
        public List<Mesh> MeshedFaces { get; set; }

        public int HostId { get; set; }

        public string UniqId { get; protected set; }
        public List<IGmlExportElement> ChildFeatures { get; set; }

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
                    return "Stairs";
                case FeatureType.SiteComponent:
                    return "Site";

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
            if (FeatureType == FeatureType.Wall)
            {

                var wallType = _document.GetElement(ThisElement.GetTypeId()) as WallType;
                if (wallType != null)
                {
                    if (wallType.Kind == WallKind.Curtain)
                    {
                        var c = new Autodesk.Revit.DB.Color(190, 228, 231);
                        var trans = Math.Round(2.55 * 85, 0); 
                        colorValue = $"#{c.Red:X2}{c.Blue:X2}{c.Green:X2}{(int)trans:X2}";
                        
                    }
                    else
                    {
                        var compound = wallType.GetCompoundStructure();
                        if (compound != null)
                        {
                            var layers = compound.GetLayers();
                            var outerfinishLayer = layers[0];
                            var material = _document.GetElement(outerfinishLayer.MaterialId) as Material;
                            var color = material.Color;
                            colorValue = $"#{color.Red:X2}{color.Green:X2}{color.Blue:X2}";
                        }
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

        }
    }
}
