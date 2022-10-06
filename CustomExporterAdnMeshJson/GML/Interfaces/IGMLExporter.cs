using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Globalization;

namespace CustomExporterAdnMeshJson.GML
{
    public interface IGMLExporter
    {
        string CreateGMLFile();
        bool DoExport();
        string PathToExportedFile { get; set; }
        
    }
}