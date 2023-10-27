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
        private List<ElementId> copiedViews = new List<ElementId>();
        // (id to reference, (min, max))
        private List<Tuple<ElementId, Tuple<XYZ, XYZ>>> copyIn = new List<Tuple<ElementId, Tuple<XYZ, XYZ>>>();
        private List<Tuple<ElementId, ElementId>> linked = new List<Tuple<ElementId, ElementId>>();

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

                //transactionNew.Start();
                //ViewSheet sheet = ViewSheet.Create(newDoc, ElementId.InvalidElementId);
                //transactionNew.Commit();

                foreach (ElementId id in selectedIds)
                {
                    transactionOld.Start();
                    View view = doc.GetElement(id) as ViewDrafting;
                    transactionOld.Commit();

                    transactionNew.Start();
                    //TaskDialog.Show("revit", doc.GetElement(id).ToString());

                    ElementId viewId = id;
                    if (doc.GetElement(id).ToString() == "Autodesk.Revit.DB.Viewport")
                    {
                        Viewport testPort = doc.GetElement(id) as Viewport;
                        view = doc.GetElement(testPort.ViewId) as View;
                        viewId = testPort.ViewId;
                    }

                    copiedViews.Add(viewId);

                    //ICollection<ElementId> dest = ElementTransformUtils.CopyElements(doc, new List<ElementId> { viewId }, newDoc, Transform.Identity, null);

                    //View destView = newDoc.GetElement(dest.ElementAt(0)) as View;
                    //var parts = new FilteredElementCollector(doc, view.Id);
                    //List<ElementId> items = parts.WhereElementIsNotElementType().ToElementIds().ToList();
                    //items.RemoveAt(0);

                    //ElementTransformUtils.CopyElements(view, items, destView, Transform.Identity, null);
                    Copy(doc, newDoc, view, viewId);


                    //Viewport viewPort = Viewport.Create(newDoc, sheet.Id, destView.Id, new XYZ());

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
        private List<Tuple<ElementId, string>> Copy(Document doc, Document newDoc, View view, ElementId viewId, bool linkBack = false, ElementId linkId = default, XYZ min = default, XYZ max = default)
        {
            List<Tuple<ElementId, string>> copies = new List<Tuple<ElementId, string>>(); 
            copiedViews.Add(viewId);
            ICollection<ElementId> dest = ElementTransformUtils.CopyElements(doc, new List<ElementId> { viewId }, newDoc, Transform.Identity, null);

            View destView = newDoc.GetElement(dest.ElementAt(0)) as View;
            var parts = new FilteredElementCollector(doc, view.Id);
            List<ElementId> items = parts.WhereElementIsNotElementType().ToElementIds().ToList();
            items.RemoveAt(0);

            //string text = " ";
            //for(int i = 0; i< items.Count; ++i)
            //{
            //    ElementId itemId = items[i];
            //    Element item = doc.GetElement(itemId);
            //    text += "item " + item.ToString() + ", " + item.Name + "\n";
            //}
            //TaskDialog.Show("revit", "copying items: " + text);

            for (int i = 0; i < items.Count; ++i)
            {
                ElementId itemId = items[i];
                string item = doc.GetElement(itemId).ToString();
                if (doc.GetElement(itemId).ToString() == "Autodesk.Revit.DB.Element")
                {
                    Element element = doc.GetElement(itemId) ;
                    BoundingBoxXYZ bb = element.get_BoundingBox(view);
                    TaskDialog.Show("revit", "element name: " + element.Name);
                    TaskDialog.Show("revit", "bb: " + element.get_BoundingBox(view).Min + " to " + element.get_BoundingBox(view).Max);
                    try
                    {
                        viewId = ReferenceableViewUtils.GetReferencedViewId(doc, itemId);
                    }
                    catch(Exception)
                    {
                        continue;
                    }

                    View newView = doc.GetElement(viewId) as View;
                    TaskDialog.Show("revit", "found linked view " + newView.Name);
                    if (!copiedViews.Contains(viewId))
                    {
                        copiedViews.Add(viewId);
                        //TaskDialog.Show("revit", "adding new view " + newView.Name);
                        Copy(doc, newDoc, newView, viewId, true, destView.Id, bb.Min, bb.Max);
                        //ViewSection.CreateReferenceSection(newDoc, viewID, viewId, new XYZ(), new XYZ());
                    }
                    else
                    {
                        TaskDialog.Show("revit", newView.Name + "already exists");
                        //ElementId copyId = new FilteredElementCollector(newDoc).WhereElementIsElementType().ToElements().Where(o => o.Name == "Given string").First().Id;

                        //    copyIn.Add(copyId);
                    }
                    items.RemoveAt(i);
                    --i;
                }
            }
            

            ElementTransformUtils.CopyElements(view, items, destView, Transform.Identity, null);

            if (linkBack)
            {
                //ViewSection.CreateReferenceCallout(newDoc, linkId, destView.Id, min, max);
                ViewSection.CreateReferenceSection(newDoc, linkId, destView.Id, min, max);
            }
            //foreach(var element in copyIn)
            //{
            //    ViewSection.CreateReferenceSection(newDoc, destView.Id, element, new XYZ(), new XYZ(0.1, 0.1, 0.1));
            //}

            return copies;
        }
    }
}
