using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Globalization;
using System.Windows.Media.Imaging;

namespace RevitProject
{
    public class ExternalApp : IExternalApplication
    {

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application)
        {
            application.CreateRibbonTab("NGMM");
            string path = Assembly.GetExecutingAssembly().Location;

            PushButtonData columns = new PushButtonData("column", "Columns from Cad", path, "RevitProject.ColumnsCommand");
            PushButtonData grids = new PushButtonData("grid", "Grids", path, "RevitProject.GridsCommand");
            PushButtonData levels = new PushButtonData("level", "Levels Importer", path, "RevitProject.LevelsCommand");

            levels.LargeImage = new BitmapImage(new Uri("D:\\Visual studio Projects\\Revit Project\\RevitProject\\RevitProject\\Images\\levels.jpeg", UriKind.RelativeOrAbsolute));
            levels.ToolTip = "Create Levels from an excel sheet.";

            grids.LargeImage =  new BitmapImage(new Uri("D:\\Visual studio Projects\\Revit Project\\RevitProject\\RevitProject\\Images\\Grids.png", UriKind.RelativeOrAbsolute));
            grids.ToolTip = "Create grids from a cad import or link and renumber the grids the way you want.";

            columns.LargeImage = new BitmapImage(new Uri("D:\\Visual studio Projects\\Revit Project\\RevitProject\\RevitProject\\Images\\columns.png", UriKind.RelativeOrAbsolute));
            columns.ToolTip = "Create Columns from a cad import or link to a specific level. Important note : Make sure to Load the required column families before using this button.";
            
            RibbonPanel panel1 = application.CreateRibbonPanel("NGMM", "Import Levels");
            RibbonPanel panel2 = application.CreateRibbonPanel("NGMM", "Grids and Columns from cad");

            panel1.AddItem(levels);           
            panel2.AddItem(grids);
            panel2.AddItem(columns);


            return Result.Succeeded;
        }
    }
}