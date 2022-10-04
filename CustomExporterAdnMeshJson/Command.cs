#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.IO;
using System.Reflection;
using CustomExporterAdnMeshJson.GML;
using System.Linq;
#endregion

namespace CustomExporterAdnMeshJson
{
    /// <summary>
    /// ADN mesh data custom exporter 
    /// external command mainline.
    /// </summary>
    [Transaction(TransactionMode.ReadOnly)]
    public class Command : IExternalCommand
    {
        public Result Execute(
          ExternalCommandData commandData,
          ref string message,
          ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            // This command requires an active document
            var exportAsJson = false;


            if (null == uidoc)
            {
                message = "Please run this command in an active project document.";
                return Result.Failed;
            }

            View3D view = doc.ActiveView as View3D;

            if (null == view)
            {
                message = "Please run this command in a 3D view.";
                return Result.Failed;
            }


            string storePath = String.Empty;
            var saveDlg = new System.Windows.Forms.SaveFileDialog();
            saveDlg.InitialDirectory = @"D:\tmp\Exportet3DJsonFiles";
            saveDlg.RestoreDirectory = true;
            saveDlg.Filter = "All files | *.*";
            var res = saveDlg.ShowDialog();
            if (res == System.Windows.Forms.DialogResult.OK)
                storePath = saveDlg.FileName;

            if (string.IsNullOrEmpty(storePath))
            {
                var executingPath = Assembly.GetExecutingAssembly().Location;
                var pathAbove = Directory.GetParent(executingPath);
                var outputDataFolder = Path.Combine(pathAbove.FullName, "OutputDir");
                if (!Directory.Exists(outputDataFolder))
                    Directory.CreateDirectory(outputDataFolder);

                storePath = Path.Combine(outputDataFolder, "Exported3dTestData");
            }

            // Instantiate our custom context
            if (!exportAsJson)
            {
                var selectedElementIds = uiapp.ActiveUIDocument.Selection.GetElementIds();
                var selectedElements = selectedElementIds.Select(f => doc.GetElement(f)).ToList();
                IGMLExporter gmlExporter = new GMLExporter(selectedElements, doc, view, storePath);
                if (gmlExporter.DoExport())
                    return Result.Succeeded;
                else return Result.Cancelled;
            }

            ExportContextAdnMesh context
              = new ExportContextAdnMesh(doc);

            // Instantiate a custom exporter with it

            using (CustomExporter exporter = new CustomExporter(doc, context))
            {
                // Tell the exporter whether we need face info.
                // If not, it is better to exclude them, since 
                // processing faces takes significant time and 
                // memory. In any case, tessellated polymeshes
                // can be exported (and will be sent to the 
                // context). Excluding faces just excludes the calls, 
                // not the actual processing of face tessellation. 
                // Meshes of the faces will still be received by 
                // the context.

                //exporter.IncludeFaces = false; // removed in Revit 2017

                exporter.IncludeGeometricObjects = false; // Revit 2017
               
                try
                {
                    exporter.Export(view);
                }
                catch (Autodesk.Revit.Exceptions.ExternalApplicationException ex)
                {
                    Debug.Print("ExternalApplicationException " + ex.Message);
                }
            }


            

            

            if (exportAsJson)
                FileWriter.WriteFileAsJson(context.MeshData, storePath);
            else
            {
                //var gmlExporter = new GMLExporter(context.MeshData);
                //var stringresult = gmlExporter.CreateGMLFile();
                //FileWriter.WriteFileAsGml(stringresult, storePath);
            }
       

            return Result.Succeeded;
        }
    }
}
