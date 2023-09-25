using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.Exceptions;

namespace revit_plugin_1
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class CopyNew : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIDocument uidoc = commandData.Application.ActiveUIDocument;
                Document doc = uidoc.Document;
                Document newDoc = null;
                Application app = doc.Application;


                Selection selection = uidoc.Selection;
                ICollection<ElementId> selectedIds = uidoc.Selection.GetElementIds();

                View src_view = doc.ActiveView;
                bool chooseFile = true;
                bool docExists = false;

                DocumentSet documents = commandData.Application.Application.Documents;

                if(documents.Size >= 2)
                {
                    docExists = true;

                    DocumentSetIterator iter = documents.ForwardIterator();

                    iter.MoveNext();
                    while (!doc.Equals(iter.Current as Document))
                    {
                        iter.MoveNext();
                    }

                    if(docExists)
                    {
                        iter.MoveNext();
                        try
                        {
                            newDoc = iter.Current as Document;
                        }
                        catch (Autodesk.Revit.Exceptions.InvalidOperationException e)
                        {
                            docExists = false;
                        }
                    }
                }

                if(!docExists)
                {
                    FileSaveDialog selectFile = new FileSaveDialog("Revit Files (*.rvt)|*.rvt");
                    selectFile.Show();

                    var fileName = ModelPathUtils.ConvertModelPathToUserVisiblePath(selectFile.GetSelectedModelPath());

                    newDoc = app.NewProjectDocument(UnitSystem.Imperial);

                    SaveAsOptions options = new SaveAsOptions();
                    options.OverwriteExistingFile = true;

                    newDoc.SaveAs(fileName, options);

                    commandData.Application.OpenAndActivateDocument(fileName);
                    newDoc = commandData.Application.ActiveUIDocument.Document;
                }

                Transaction transactionNew = new Transaction(newDoc, "bobr");
                Transaction transactionOld = new Transaction(doc, "bobr2");

                transactionNew.Start();
                ViewSheet sheet = ViewSheet.Create(newDoc, ElementId.InvalidElementId);
                transactionNew.Commit();

                foreach (ElementId id in selectedIds)
                {
                    transactionOld.Start();
                    View view = doc.GetElement(id) as ViewDrafting;
                    transactionOld.Commit();

                    transactionNew.Start();
                    //TaskDialog.Show("revit", doc.GetElement(id).ToString());

                    ElementId viewId = id;
                    if(doc.GetElement(id).ToString() == "Autodesk.Revit.DB.Viewport")
                    {
                        Viewport testPort = doc.GetElement(id) as Viewport;
                        view = doc.GetElement(testPort.ViewId) as View;
                        viewId = testPort.ViewId;
                    }
                    
                    ICollection<ElementId> dest = ElementTransformUtils.CopyElements(doc, new List<ElementId> { viewId }, newDoc, Transform.Identity, null);

                    View destView = newDoc.GetElement(dest.ElementAt(0)) as View;
                    var parts = new FilteredElementCollector(doc, view.Id);
                    List<ElementId> items = parts.WhereElementIsNotElementType().ToElementIds().ToList();
                    items.RemoveAt(0);

                    ElementTransformUtils.CopyElements(view, items, destView, Transform.Identity, null);

                    Viewport viewPort = Viewport.Create(newDoc, sheet.Id, destView.Id, new XYZ());

                    transactionNew.Commit();
                }
            }
            catch (Exception e)
            {
                message = e.Message;
                return Result.Failed;
            }

            return Result.Succeeded;
        }
    }
}
