using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Xml.Schema;
using SaveOptions = System.Xml.Linq.SaveOptions;

namespace CustomExporterAdnMeshJson.GML
{
    public class GMLExporter : IGMLExporter
    {
        private readonly IEnumerable<Element> elementsToExport;
        private readonly Document doc;
        private readonly View3D active3DView;
        private readonly string _outputpath;
        XNamespace srsName = "http://www.opengis.net/def/crs/EPSG/0/5110";

        XNamespace gml = "http://www.opengis.net/gml/3.2";
        XNamespace ogc = "http://www.opengis.net/ogc";
        XNamespace xlink = "http://www.w3.org/1999/xlink";
        XNamespace rvt = "http://www2.focus.no/kundenedlasting/schema/RevitExportModel";
        XNamespace rvtlok = "http://www2.focus.no/kundenedlasting/schema/RevitExportModel http://www2.focus.no/kundenedlasting/schema/RevitExportModel/RevitExportModel.xsd";
        List<XElement> _createdFeatures = new List<XElement>();
        ProjectLocation _projectLocation;

        public string PathToExportedFile { get; set; }

        internal GMLExporter(IEnumerable<Element> elementsToExport, Document doc, View3D active3dView, string outputpath)
        {
            this.elementsToExport = elementsToExport;
            this.doc = doc;
            active3DView = active3dView;
            _outputpath = outputpath;

            _projectLocation = doc.ActiveProjectLocation;

        }


        private XElement SetupRootDocument()
        {
            XNamespace xsiNamespace = XmlSchema.InstanceNamespace;

            XAttribute instanceAttribute = new XAttribute(XNamespace.Xmlns + "xsi", xsiNamespace.NamespaceName);
            XAttribute gmlAttribute = new XAttribute(XNamespace.Xmlns + "gml", gml.NamespaceName);
            XAttribute revitAttribute = new XAttribute(XNamespace.Xmlns + "rvt", rvt.NamespaceName);
            XAttribute ogcAttribute = new XAttribute(XNamespace.Xmlns + "ogc", ogc.NamespaceName);
            XAttribute xlinkAttribute = new XAttribute(XNamespace.Xmlns + "xlink", xlink.NamespaceName);
            XAttribute schemaLocation = new XAttribute(xsiNamespace + "schemaLocation", "http://www2.focus.no/kundenedlasting/schema/RevitExportModel http://www2.focus.no/kundenedlasting/schema/RevitExportModel/RevitExportModel.xsd");

            var rootElement = new XElement(gml + "FeatureCollection",
                    ogcAttribute,
                    gmlAttribute,
                    revitAttribute,
                    instanceAttribute,
                    xlinkAttribute,
                    schemaLocation
                           );
            var envelope = GetEnvelope();
            rootElement.Add(envelope);
            return rootElement;
        }

        private XElement GetEnvelope()
        {
            var fec = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Site).ToElements().Where(f => f is FamilyInstance).ToList();
            var sectionbox = active3DView.GetSectionBox(); 
            
            var boundingelement = new XElement(gml + "boundedBy");
            var srsNameAttribute = new XAttribute("srsName", srsName.NamespaceName);
            var srsDimmensionAttribute = new XAttribute("srsDimension", "2");
            var envelope = new XElement(gml + "Envelope", srsNameAttribute, srsDimmensionAttribute);
            boundingelement.Add(envelope);


            var lower = GetBoundingCornerElement("lowerCorner", sectionbox.Min);
            if (lower != null)
                envelope.Add(lower);
            var upper = GetBoundingCornerElement("upperCorner", sectionbox.Max);
            if (upper != null)
                envelope.Add(upper);
            return boundingelement;
        }
        private XElement GetBoundingCornerElement(string name, XYZ point)
        {
           
            XElement xElementCorner = null;
            if (point != null)
            {
                    var pL = _projectLocation.GetProjectPosition(point);
                    xElementCorner = new XElement(gml + name) { Value = $"{Math.Floor(UnitUtils.ConvertFromInternalUnits(pL.EastWest, UnitTypeId.Meters))} {Math.Floor(UnitUtils.ConvertFromInternalUnits(pL.NorthSouth, UnitTypeId.Meters))}" };
            }
            return xElementCorner;
        }


        public string CreateGMLFile()
        {
            var rootElement = SetupRootDocument();
            XDocument xDocument = new XDocument(new XDeclaration("1.0", "UTF-8", ""));

            foreach (var oneFeature in _createdFeatures)
                rootElement.Add(oneFeature);
            xDocument.Add(rootElement);
            string strXml = xDocument.ToString();
            return strXml;

        }
        public bool DoExport()
        {
            foreach (var element in elementsToExport)
            {
                var exportFeature = GetExportDataElement(element);
                var wallFeature = HandleFeature(exportFeature);
                if (wallFeature != null)
                    _createdFeatures.AddRange(wallFeature);

            }
            var restultingGml = CreateGMLFile();
            PathToExportedFile = _outputpath;
            return FileWriter.WriteFileAsGml(restultingGml, _outputpath);
        }

        private List<XElement> HandleFeature(IGmlExportElement baseFeature)
        {
            List<XElement> _features = new List<XElement>();
            if (baseFeature is GmlHostingSurfaceElement)
            {
                var feature = GetFeatureElement(baseFeature);

                if (feature != null)
                    _features.Add(feature);
                if (baseFeature.ChildFeatures.Any())
                {
                    foreach (var c in baseFeature.ChildFeatures)
                    {
                        var cfeature = GetFeatureElement(c);
                        if (cfeature != null)
                            _features.Add(cfeature);
                    }
                }
            }
            if (baseFeature is GmlOpeningExportElement opening)
            {
                var mainFeature = GetFeatureElement(opening);
                if (mainFeature != null)
                    _features.Add(mainFeature);
                foreach (var child in opening.ChildFeatures)
                {
                    var cFeature = GetFeatureElement(child);
                    if (cFeature != null)
                        _features.Add(cFeature);
                }
            }
            if(baseFeature is GmlStairExportElement stair)
            {
                var f = GetFeatureElement(stair);
                if (f != null)
                    _features.Add(f);
            }
            if(baseFeature is GmlSiteComponentExportElement site)
            {
                var f = GetFeatureElement(site);
                if (f != null)
                    _features.Add(f);
            }
            return _features;
        }


        private XElement GetFeatureElement(IGmlExportElement el)
        {
            if (el.MeshedFaces == null) return null;

            XElement feature = new XElement(gml + "featureMember");


            XElement revitFeature = new XElement(rvt + el.FeatureName, new XAttribute(gml + "id", el.UniqId));

            foreach (var prop in el.Properties)
            {
                XElement revitProp = new XElement(rvt + prop.Name) { Value = prop.ToString() };
                revitFeature.Add(revitProp);
            }

            XElement geometry = new XElement(rvt + "Geometry");
            revitFeature.Add(geometry);

            XElement solid = new XElement(gml + "Solid");
            geometry.Add(solid);
            XElement exterior = new XElement(gml + "exterior");
            solid.Add(exterior);
            XElement shell = new XElement(gml + "Shell");
            exterior.Add(shell);
            XElement surfaceMember = new XElement(gml + "surfaceMember");
            shell.Add(surfaceMember);
            XElement triangulatedSurface = new XElement(gml + "TriangulatedSurface");
            surfaceMember.Add(triangulatedSurface);

            XElement patches = new XElement(gml + "patches");
            triangulatedSurface.Add(patches);
            foreach (var item in el.MeshedFaces)
            {
                for (int i = 0; i < item.NumTriangles; i++)
                {
                    var triangle = GetTriangleElement(item.get_Triangle(i));
                    patches.Add(triangle);
                }
            }
            feature.Add(revitFeature);



            return feature;

        }

        private XElement GetTriangleElement(MeshTriangle meshedTriangle)
        {
            XElement triangle1 = new XElement(gml + "Triangle");
            XElement exteror1 = new XElement(gml + "exterior");
            triangle1.Add(exteror1);
            XElement linearRing1 = new XElement(gml + "LinearRing");
            exteror1.Add(linearRing1);
            var srsDim = new XAttribute("srsDimension", 3);
            XElement posList1 = new XElement(gml + "posList", srsDim);
            linearRing1.Add(posList1);
            var valueString = "";

            var firstVertexString = string.Empty;

            for (int j = 0; j < 3; j++)
            {
                var vert = meshedTriangle.get_Vertex(j);

                var pl = _projectLocation.GetProjectPosition(vert);
                var x = UnitUtils.ConvertFromInternalUnits(pl.EastWest, UnitTypeId.Meters);
                var y = UnitUtils.ConvertFromInternalUnits(pl.NorthSouth, UnitTypeId.Meters);
                var z = UnitUtils.ConvertFromInternalUnits(pl.Elevation, UnitTypeId.Meters);
                valueString += $"{GetKoordinatWithDot(x)} {GetKoordinatWithDot(y)} {GetKoordinatWithDot(z)} ";
                if (j == 0)
                    firstVertexString = valueString.TrimEnd();
            }
            valueString += firstVertexString;
            valueString.TrimEnd();
            posList1.Value = valueString;
            return triangle1;
        }
        private string GetKoordinatWithDot(double value)
        {
            var strinValue = value.ToString();
            return strinValue.Replace(",", ".");
        }

        private IGmlExportElement GetExportDataElement(Element el)
        {
            IGmlExportElement exportDataElemet = null;

            if (el is Wall wall)
                exportDataElemet = new GmlHostingSurfaceElement(FeatureType.Wall, wall, active3DView);
            else if (el is FootPrintRoof roof)
                exportDataElemet = new GmlHostingSurfaceElement(FeatureType.Roof, roof, active3DView);
            else if (el is Floor floor)
                exportDataElemet = new GmlHostingSurfaceElement(FeatureType.Floor, floor, active3DView);
            else if (el is FamilyInstance inst)
            {
                var catId = inst.Category.Id.IntegerValue;
                if (catId == (int)BuiltInCategory.OST_Doors)
                {
                    exportDataElemet = new GmlOpeningExportElement(FeatureType.Door, inst, active3DView);
                }
                else if (catId == (int)BuiltInCategory.OST_Windows)
                {
                    exportDataElemet = new GmlOpeningExportElement(FeatureType.Window, inst, active3DView);
                }
                else if (catId == (int)BuiltInCategory.OST_Floors)
                    exportDataElemet = new GmlHostingSurfaceElement(FeatureType.Floor, inst, active3DView);
                else if(catId == (int)BuiltInCategory.OST_Site)
                    exportDataElemet = new GmlSiteComponentExportElement(FeatureType.SiteComponent, inst, active3DView);

            }
            else if (el is Stairs stair)
                exportDataElemet = new GmlStairExportElement(FeatureType.Stairs, stair,active3DView);
            
            if (exportDataElemet == null) return null;

            exportDataElemet.HandleGeometry();
            exportDataElemet.PopulateElementPropertyData();
            return exportDataElemet;
        }


        //private IEnumerable<Face> GetFaces(Element wall)
        //{
        //    var faces = new List<Face>();
        //    var geom = wall.get_Geometry(new Options() { IncludeNonVisibleObjects = false, View = active3DView });
        //    foreach (var f in geom)
        //    {
        //        if (f is Solid s)
        //        {
        //            var faceArray = s.Faces;
        //            var itt = faceArray.ForwardIterator();
        //            while (itt.MoveNext())
        //            {
        //                if (itt.Current is Face face)
        //                    faces.Add(face);
        //            }
        //        }
        //        if(f is GeometryInstance gInstElement)
        //        {
        //            foreach (var symbolGeom in gInstElement.SymbolGeometry)
        //            {
        //                if(symbolGeom is Solid solid)
        //                {
        //                    var faceArray = solid.Faces;
        //                    var itt = faceArray.ForwardIterator();
        //                    while (itt.MoveNext())
        //                    {
        //                        if (itt.Current is Face face)
        //                            faces.Add(face);
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    return faces;
        //}


        //private Mesh GetMesh(Face face)
        //{
        //    return face.Triangulate(1);
        //}


    }

}
