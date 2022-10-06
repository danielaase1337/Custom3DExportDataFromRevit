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
            var selectedElementIds = uiapp.ActiveUIDocument.Selection.GetElementIds().ToList();
            if (!selectedElementIds.Any())
            {
                selectedElementIds.AddRange(AddElementsFromFec(BuiltInCategory.OST_Walls, doc));
                //selectedElementIds.AddRange(AddElementsFromFec(BuiltInCategory.OST_CurtainGridsWall, doc));
                selectedElementIds.AddRange(AddElementsFromFec(BuiltInCategory.OST_Doors, doc));
                selectedElementIds.AddRange(AddElementsFromFec(BuiltInCategory.OST_Windows, doc));
                selectedElementIds.AddRange(AddElementsFromFec(BuiltInCategory.OST_Floors, doc));
                selectedElementIds.AddRange(AddElementsFromFec(BuiltInCategory.OST_Roofs, doc));
                selectedElementIds.AddRange(AddElementsFromFec(BuiltInCategory.OST_Stairs, doc));
                selectedElementIds.AddRange(AddElementsFromFec(BuiltInCategory.OST_Site, doc));
            }

            var selectedElements = selectedElementIds.Select(f => doc.GetElement(f)).ToList();
            IGMLExporter gmlExporter = new GMLExporter(selectedElements, doc, view, storePath);
            if (gmlExporter.DoExport())
            {

                var dlg = new ExportHoster(gmlExporter.PathToExportedFile);
                dlg.ShowDialog();

                return Result.Succeeded;
            }

            return Result.Cancelled;

        }
        private List<ElementId> AddElementsFromFec(BuiltInCategory cat, Document doc)
        {
            var fec = new FilteredElementCollector(doc).OfCategory(cat).WhereElementIsNotElementType();
            return fec.ToElementIds().ToList();
        }
    }
}
