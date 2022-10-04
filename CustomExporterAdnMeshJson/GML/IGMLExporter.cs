using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace CustomExporterAdnMeshJson.GML
{
    public interface IGMLExporter
    {
        string CreateGMLFile();
        bool DoExport(); 
        
    }
}