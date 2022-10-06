using Autodesk.Revit.DB;
using System.Collections.Generic;

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
        List<IGmlExportElement> ChildFeatures { get; set; }
        int HostId { get; set; }
        string UniqId { get; }
    }
}
