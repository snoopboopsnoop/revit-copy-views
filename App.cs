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
using System.Reflection;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using System.Windows.Media;

namespace revit_plugin_1
{
    public class App : IExternalApplication
    {
        
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
        public Result OnStartup(UIControlledApplication application)
        {

            string thisAssemblyPath = Assembly.GetExecutingAssembly().Location;

            RibbonPanel panel = application.CreateRibbonPanel("My Panel");

            PushButton button1 = panel.AddItem(new PushButtonData("Button1", "Copy to New", thisAssemblyPath, "revit_plugin_1.CopyNew")) as PushButton;
            button1.ToolTip = "bobr";

            Uri uri1 = new Uri(Path.Combine(Path.GetDirectoryName(thisAssemblyPath), "src", "mebeab.jpg"));
            BitmapImage bitmap = new BitmapImage(uri1);
            button1.LargeImage = bitmap;

            PushButton button2 = panel.AddItem(new PushButtonData("Button2", "Copy to...", thisAssemblyPath, "revit_plugin_1.CopyTo")) as PushButton;
            button2.ToolTip = "bobr";

            Uri uri2 = new Uri(Path.Combine(Path.GetDirectoryName(thisAssemblyPath), "src", "goat.jpg"));
            BitmapImage bitmap2 = new BitmapImage(uri2);
            button2.LargeImage = bitmap2;

            return Result.Succeeded;
        }
    }
}
