using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;

namespace CustomExporterAdnMeshJson.GML
{
    internal class GmlHostingSurfaceElement : GmlExportElementBase
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
            if (ThisElement is Wall wall)
            {
                var wallType = _document.GetElement(wall.GetTypeId()) as WallType;
                if (wallType.Kind == WallKind.Curtain)
                {
                    var curtainGrid = wall.CurtainGrid;
                    var mullions = curtainGrid.GetMullionIds();
                    if (mullions != null && mullions.Any())
                    {
                        foreach (var id in mullions)
                        {
                            var m = _document.GetElement(id);
                            if (m is FamilyInstance ins)
                            {
                                var geom = m.get_Geometry(new Options() { IncludeNonVisibleObjects = false, View = activeView });
                                foreach (var g in geom)
                                {
                                    if (g is GeometryInstance inst)
                                    {
                                        var gObject = inst.GetSymbolGeometry(inst.Transform);
                                        foreach (var item in gObject)
                                        {
                                            if (item is Solid s)
                                                faces.AddRange(GetFaces(s));
                                        }
                                    }
                                    else
                                    {
                                        if (g is Solid s)
                                            faces.AddRange(GetFaces(s));

                                    }

                                }

                            }
                        }
                    }
                    var panels = curtainGrid.GetPanelIds();
                    foreach (var pId in panels)
                    {
                        var panel = _document.GetElement(pId);
                        var child = new CurtainWallChildElement(FeatureType.GlassSurface, panel, activeView);
                        child.HandleGeometry();
                        child.PopulateElementPropertyData();
                        child.HostId = ThisElement.Id.IntegerValue;
                        ChildFeatures.Add(child);
                    }

                }
                else
                {
                    foreach (var item in GeometryElement)
                    {
                        if (item is Solid s)
                        {
                            faces.AddRange(GetFaces(s));
                        }
                        else if (item is GeometryInstance instance)
                        {
                            var symbolGeom = instance.GetSymbolGeometry(instance.Transform);
                            foreach (var sgeom in symbolGeom)
                            {
                                if (sgeom is Solid solid)
                                {
                                    faces.AddRange(GetFaces(solid));
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (var item in GeometryElement)
                {
                    if (item is Solid s)
                    {
                        faces.AddRange(GetFaces(s));
                    }
                    else if (item is GeometryInstance instance)
                    {
                        var symbolGeom = instance.GetSymbolGeometry(instance.Transform);
                        foreach (var sgeom in symbolGeom)
                        {
                            if (sgeom is Solid solid)
                            {
                                faces.AddRange(GetFaces(solid));
                            }
                        }
                    }
                }
            }

            var meshes = faces.Select(f => GetMesh(f)).ToList();
            MeshedFaces = meshes;
        }
    }
}
