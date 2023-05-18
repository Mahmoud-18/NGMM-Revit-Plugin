using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Ganss.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RevitProject
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class LevelsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData,
            ref string message, ElementSet elements)
        {
            UIDocument Uidoc = commandData.Application.ActiveUIDocument;
            Autodesk.Revit.DB.Document doc = Uidoc.Document;


            using (Transaction tr = new Transaction(doc, "Draw Level"))
            {
                String filename;
                try
                {
                    tr.Start();


                    FilteredElementCollector Collector1 = new FilteredElementCollector(doc);
                    ICollection<Element> All_levels_in_doc = Collector1.OfClass(typeof(Level)).ToElements();
                    List<ElementId> elementsToBeDeleted = new List<ElementId>();
                    foreach (Element element in All_levels_in_doc)
                    {
                        elementsToBeDeleted.Add(element.Id);
                    }

                    doc.Delete(elementsToBeDeleted);


                    try
                    {
                        filename = GetPath();
                    }
                    catch (Exception Ex)
                    {
                        message = Ex.Message;
                        return Result.Failed;
                    }



                    var levels = new ExcelMapper(filename).Fetch<Imported_Data>();

                    foreach (var level in levels)
                    {
                        Level lev = Level.Create(doc, level.Elevation / 304.8);
                        lev.Name = level.LevelName;

                        FilteredElementCollector collector = new FilteredElementCollector(doc);
                        collector.OfClass(typeof(Autodesk.Revit.DB.View));
                        var views = collector.ToElements();
                        foreach (Autodesk.Revit.DB.View item in views)
                        {
                            lev = item.GenLevel;
                        }


                    }







                    FilteredElementCollector Collector2 = new FilteredElementCollector(doc);
                    ICollection<Element> All_levels_in_doc2 = Collector2.OfClass(typeof(Level)).ToElements();

                    foreach (var item in All_levels_in_doc) All_levels_in_doc2.Remove(item);



                    ViewFamilyType FloorplanFamily = new FilteredElementCollector(doc)
               .OfClass(typeof(ViewFamilyType))
               .Cast<ViewFamilyType>()
               .First(x => x.ViewFamily == ViewFamily.FloorPlan);
                    ViewFamilyType CeilingPlanFamily = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .First(x => x.ViewFamily == ViewFamily.CeilingPlan);

                    ViewFamilyType StructuralPlan = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .First(x => x.ViewFamily == ViewFamily.StructuralPlan);



                    //List<Element> tempList = new List<Element>();
                    //foreach (var item in All_levels_in_doc)
                    //{
                    //    tempList.Add(item);
                    //}

                    //foreach (var item in tempList)
                    //{
                    //    All_levels_in_doc2.Remove(item);
                    //}





                    foreach (Element element in All_levels_in_doc2)
                    {
                        ViewPlan Floorplan = ViewPlan.Create(doc, FloorplanFamily.Id, element.Id);
                        ViewPlan CeilingPlan = ViewPlan.Create(doc, CeilingPlanFamily.Id, element.Id);
                        ViewPlan Structural = ViewPlan.Create(doc, StructuralPlan.Id, element.Id);
                        Floorplan.Name = element.Name;
                        CeilingPlan.Name = element.Name;
                        Structural.Name = element.Name;
                    }

                    //FilteredElementCollector tempcollector = new FilteredElementCollector(doc);
                    //tempcollector.OfClass(typeof(Autodesk.Revit.DB.View));
                    //Autodesk.Revit.DB.View myView = tempcollector.FirstElement() as Autodesk.Revit.DB.View;

                    //UIDocument uidoc = commandData.Application.ActiveUIDocument;
                    //uidoc.ActiveView = myView;

                    //UIApplication uiapp = commandData.Application;
                    //uiapp.ActiveUIDocument.ActiveView = myView;

                    //doc.Delete(elementsToBeDeleted);


                    tr.Commit();
                    return Result.Succeeded;
                }
                catch (Exception Ex)
                {
                    message = Ex.Message;
                    return Result.Failed;
                }
            }
        }
        public static string GetPath()
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.Title = "Select an Excel File";
            openDialog.Filter = "Excel Files (.xlsx)|*.xlsx";

            openDialog.RestoreDirectory = true;
            if (openDialog.ShowDialog() == DialogResult.OK)
            {
                string fileName = openDialog.FileName;
                return fileName;
            }
            else
                return null;
        }

        public static void MoveItems(ICollection<string> source, ICollection<string> target)
        {
            List<string> tempList = new List<string>();
            foreach (string item in source)
            {
                tempList.Add(item);
            }

            foreach (string item in tempList)
            {
                target.Remove(item);
            }
        }

    }
    public class Imported_Data
    {
        public string LevelName { get; set; }
        public double Elevation { get; set; }
    }
}

