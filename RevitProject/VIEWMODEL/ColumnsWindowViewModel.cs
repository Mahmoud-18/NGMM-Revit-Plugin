using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Autodesk.Revit.Attributes;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;

namespace RevitProject
{
    public class ColumnsWindowViewModel : INotifyPropertyChanged
    {

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        protected void onPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));                     
        }
        #endregion

        #region Constructor
        public ColumnsWindowViewModel()
        {
            CadLoad();
            Create = new Command(DoneCommand);
            Done = new Command(Close);
        }
        #endregion

        #region Properties & Fields

        Document Doc = ColumnsCommand.Doc;

        private IList<PolyLine> plines = new List<PolyLine>();
        private IList<Arc> arcs = new List<Arc>();        

        public Command Create { get; set; }
        public Command Done { get; set; }
        public IList<string> layersname { get; set; } = new List<string>();
        public IList<string> levelsname { get; set; } = new List<string>();

        public static IList<string> columntype { get; set; } = new List<string>() { "Structural Column","Architectural Column" };


        private string _selectedType = columntype[0];

        public string SelectedType
        {
            get { return _selectedType; }
            set
            {
                _selectedType = value;

                onPropertyChanged(nameof(SelectedType));
            }
        }

        private string _selectedlevel;

        public string Selectedlevel
        {
            get { return _selectedlevel; }
            set
            {

                _selectedlevel = value;

                onPropertyChanged(nameof(Selectedlevel));
            }
        }

        private string _selectedlayer;

        public string Selectedlayer
        {
            get { return _selectedlayer; }
            set
            {

                _selectedlayer = value;

                onPropertyChanged(nameof(Selectedlayer));
            }
        }

        private Boolean _structural = true;

        public Boolean Structural
        {
            get { return _structural; }
            set
            {
                _structural = value;
                onPropertyChanged(nameof(Structural));
            }
        }
        #endregion

        #region Methods
        public void Close(Object parameter)
        {
            (parameter as ColumnsWindow).Close();
        }
        public void CadLoad()
        {
            IList<ElementId> cadimports = (IList<ElementId>)new FilteredElementCollector(Doc).OfClass(typeof(ImportInstance))
                .WhereElementIsNotElementType().ToElementIds();

            IList<Level> levels = new FilteredElementCollector(ColumnsCommand.Doc).OfClass(typeof(Level)).Cast<Level>().ToList();
            foreach(var level in levels)
            {
                levelsname.Add(level.Name);
            }

            if (cadimports.Count > 0)
            {
                ImportInstance imp = Doc.GetElement(cadimports.First()) as ImportInstance;
                GeometryElement geoel = imp.get_Geometry(new Options());
                foreach (GeometryObject go in geoel)
                {
                    if (go is GeometryInstance)
                    {
                        GeometryInstance gi = go as GeometryInstance;
                        GeometryElement gl = gi.GetInstanceGeometry();
                        if (gl != null)
                        {
                            foreach (GeometryObject go2 in gl)
                            {
                                if (go2 is PolyLine)
                                {
                                    GraphicsStyle gstyle =Doc.GetElement(go2.GraphicsStyleId) as GraphicsStyle;
                                    string layer = gstyle.GraphicsStyleCategory.Name;
                                    if (!layersname.Contains(layer))
                                    {                                      
                                        layersname.Add(layer);
                                    }
                                   
                                    plines.Add(go2 as PolyLine);
                                }
                                if (go2 is Arc)
                                {
                                    GraphicsStyle gstyle =Doc.GetElement(go2.GraphicsStyleId) as GraphicsStyle;
                                    string layer = gstyle.GraphicsStyleCategory.Name;
                                    if (!layersname.Contains(layer))
                                    { 
                                        layersname.Add(layer);
                                    }                                   
                                    arcs.Add(go2 as Arc);
                                }                                
                            }

                        }
                    }
                }
                                          
            }
            else
            {
                TaskDialog.Show("error", "can't find cad import");
            }            
        }
        public void DoneCommand(Object parameter)
        {
            Document Doc = ColumnsCommand.Doc;

            IList<Level> levelsname = new FilteredElementCollector(Doc).OfClass(typeof(Level)).Cast<Level>().ToList();
            Level collevel = null;

            StructuralType structuralType;
            if (_structural == true)
            {
                structuralType = StructuralType.Column;
            }
            else
            {
                structuralType = StructuralType.NonStructural;
            }

            foreach (Level level in levelsname)
            {
                if (level.Name == _selectedlevel)
                {
                    collevel = level;
                }
            }

            foreach (PolyLine pl in plines)
            {
                GraphicsStyle gstyle = Doc.GetElement(pl.GraphicsStyleId) as GraphicsStyle;
                string layer = gstyle.GraphicsStyleCategory.Name;
                if (layer == _selectedlayer)
                {
                    IList<XYZ> points = pl.GetCoordinates();
                    XYZ p1 = points[0];
                    XYZ p2 = points[1];
                    XYZ p3 = points[2]; 
                    
                    Vector3D v1 = new Vector3D();
                    v1.X = Math.Abs(p2.X - p1.X);
                    v1.Y = Math.Abs(p2.Y - p1.Y);
                    v1.Z = Math.Abs(p2.Z - p1.Z);
                    
                    Vector3D v2 = new Vector3D();
                    v2.X = Math.Abs(p3.X - p2.X);
                    v2.Y = Math.Abs(p3.Y - p2.Y);
                    v2.Z = Math.Abs(p3.Z - p2.Z);

                    Outline o = pl.GetOutline();
                    XYZ fpoint = o.MaximumPoint;
                    XYZ spoint = o.MinimumPoint;
                    XYZ midline = MidPoint(fpoint.X, fpoint.Y, fpoint.Z, spoint.X, spoint.Y, spoint.Z);

                    string name = null;
                    bool flag = true;
                    Family family;
                    FamilySymbol familySymbol;
                    Autodesk.Revit.DB.Document familyDoc;

                    double length;
                    double width;
                    if (v1.X > v2.X)
                    {
                        length = Math.Round(SideLength(p1.X, p1.Y, p1.Z, p2.X, p2.Y, p2.Z));
                        width = Math.Round(SideLength(p2.X, p2.Y, p2.Z, p3.X, p3.Y, p3.Z));
                    }
                    else
                    {
                        width = Math.Round(SideLength(p1.X, p1.Y, p1.Z, p2.X, p2.Y, p2.Z));
                        length = Math.Round(SideLength(p2.X, p2.Y, p2.Z, p3.X, p3.Y, p3.Z));
                    }

                    name = length.ToString() + " x " + width.ToString() + "mm";

                    if (_selectedType == columntype[0])
                    {
                        IList<Element> columns = new FilteredElementCollector(Doc).OfCategory(BuiltInCategory.OST_StructuralColumns).WhereElementIsElementType().ToElements();
                        foreach (var item in columns)
                        {
                            if (item.Name == name)
                            {
                                flag = false;
                            }
                        }                        
                        familySymbol = new FilteredElementCollector(Doc).OfCategory(BuiltInCategory.OST_StructuralColumns).WhereElementIsElementType().Where(x => x.Name == "300 x 450mm" || x.Name == "450 x 600mm" || x.Name == "600 x 750mm" || x.Name == "12 x 18" || x.Name == "18 x 24" || x.Name == "24 x 30").FirstOrDefault() as FamilySymbol;
                        if (null == familySymbol)
                        {
                            MessageBox.Show("could not find the required family loaded. Please make sure to load the required families.");
                            return;    // could not find the family 
                        }
                        family = familySymbol.Family;
                             
                        familyDoc = Doc.EditFamily(family);
                            if (null == familyDoc)
                            {
                                return;    // could not open a family for edit
                            }

                            FamilyManager familyManager = familyDoc.FamilyManager;
                            if (null == familyManager)
                            {
                                return;  // could not get a family manager
                            }
                        if (flag)
                        {
                            using (Transaction newFamilyTypeTransaction = new Transaction(familyDoc, "Add Type to Family"))
                            {
                                int changesMade = 0;
                                newFamilyTypeTransaction.Start();
                                // add a new type and edit its parameters                           
                                FamilyType newFamilyType = familyManager.NewType(name);

                                if (newFamilyType != null)
                                {
                                    // look for 'b' and 'h' parameters and set them to 2 feet
                                    FamilyParameter familyParam = familyManager.get_Parameter("h");
                                    if (null != familyParam)
                                    {
                                        familyManager.Set(familyParam, width / 304.8);
                                        changesMade += 1;
                                    }                                    
                                    familyParam = familyManager.get_Parameter("b");
                                    if (null != familyParam)
                                    {
                                        familyManager.Set(familyParam, length / 304.8);
                                        changesMade += 1;
                                    }
                                }

                                if (2 == changesMade)   // set both paramaters?
                                {
                                    newFamilyTypeTransaction.Commit();
                                }
                                else   // could not make the change -> should roll back 
                                {
                                    newFamilyTypeTransaction.RollBack();
                                }
                                if (newFamilyTypeTransaction.GetStatus() != TransactionStatus.Committed)
                                {
                                    return;
                                }
                            }
                        }


                        // now update the Revit project with Family which has a new type
                        LoadOpts loadOptions = new LoadOpts();

                        // This overload is necessary for reloading an edited family
                        // back into the source document from which it was extracted
                        family = familyDoc.LoadFamily(Doc, loadOptions);
                        if (null != family)
                        {
                            // find the new type and assign it to FamilyInstance
                            ISet<ElementId> familySymbolIds = family.GetFamilySymbolIds();
                            foreach (ElementId id in familySymbolIds)
                            {
                                FamilySymbol familysymbol = family.Document.GetElement(id) as FamilySymbol;
                                if ((null != familysymbol) && familysymbol.Name == name)
                                {
                                    using (Transaction changeSymbol = new Transaction(Doc, "Change Symbol Assignment"))
                                    {
                                        changeSymbol.Start();
                                        familySymbol = familysymbol;
                                        changeSymbol.Commit();
                                    }
                                    break;
                                }
                            }
                        }
                        IList<Element> columnss = new FilteredElementCollector(Doc).OfCategory(BuiltInCategory.OST_StructuralColumns).WhereElementIsElementType().ToElements();
                        FamilySymbol fs = null;
                        foreach (Element ele in columnss)
                        {
                            if (ele.Name == name)
                            {
                                fs = ele as FamilySymbol;
                            }
                        }
                        using (Transaction trans = new Transaction(Doc, "create columns"))
                        {
                            trans.Start();
                            try
                            {
                                if (!fs.IsActive)
                                {
                                    fs.Activate();
                                }
                                Doc.Create.NewFamilyInstance(midline, fs, collevel, structuralType);
                            }
                            catch (Exception ex)
                            {
                                TaskDialog.Show(ex.Message, ex.ToString());

                            }
                            trans.Commit();
                        }
                    }
                    
                    else
                    {                             
                        IList<Element> columns = new FilteredElementCollector(Doc).OfCategory(BuiltInCategory.OST_Columns).WhereElementIsElementType().ToElements();
                        foreach (var item in columns)
                        {
                            if (item.Name == name)
                            {
                                flag = false;
                            }
                        }
                        
                        familySymbol = new FilteredElementCollector(Doc).OfCategory(BuiltInCategory.OST_Columns).WhereElementIsElementType().Where(x => x.Name == "475 x 610mm" || x.Name == "610 x 610mm" || x.Name == "457 x 475mm" || x.Name == "18\" x 18\"" || x.Name == "18\" x 24\"" || x.Name == "24\" x 24\"").FirstOrDefault() as FamilySymbol;
                        if (null == familySymbol)
                        {
                            MessageBox.Show("could not find the required family loaded. Please make sure to load the required families.");
                            return;    // could not find the family 
                        }
                        family = familySymbol.Family;
                        
                        familyDoc = Doc.EditFamily(family);
                            if (null == familyDoc)
                            {
                                return;    // could not open a family for edit
                            }

                            FamilyManager familyManager = familyDoc.FamilyManager;
                            if (null == familyManager)
                            {
                                return;  // could not get a family manager
                            }

                        if (flag)
                        {
                            using (Transaction newFamilyTypeTransaction = new Transaction(familyDoc, "Add Type to Family"))
                            {
                                int changesMade = 0;
                                newFamilyTypeTransaction.Start();
                                // add a new type and edit its parameters                           
                                FamilyType newFamilyType = familyManager.NewType(name);

                                if (newFamilyType != null)
                                {
                                    // look for 'b' and 'h' parameters and set them to 2 feet
                                    FamilyParameter familyParam = familyManager.get_Parameter("Depth");
                                    if (null != familyParam)
                                    {
                                        familyManager.Set(familyParam, width / 304.8);
                                        changesMade += 1;
                                    }

                                    familyParam = familyManager.get_Parameter("Width");
                                    if (null != familyParam)
                                    {
                                        familyManager.Set(familyParam, length / 304.8);
                                        changesMade += 1;
                                    }
                                }

                                if (2 == changesMade)   // set both paramaters?
                                {
                                    newFamilyTypeTransaction.Commit();
                                }
                                else   // could not make the change -> should roll back 
                                {
                                    newFamilyTypeTransaction.RollBack();
                                }
                                if (newFamilyTypeTransaction.GetStatus() != TransactionStatus.Committed)
                                {
                                    return;
                                }
                            }
                        }
                    

                        // now update the Revit project with Family which has a new type
                        LoadOpts loadOptions = new LoadOpts();

                        // This overload is necessary for reloading an edited family
                        // back into the source document from which it was extracted
                        family = familyDoc.LoadFamily(Doc, loadOptions);
                        if (null != family)
                        {
                            // find the new type and assign it to FamilyInstance
                            ISet<ElementId> familySymbolIds = family.GetFamilySymbolIds();
                            foreach (ElementId id in familySymbolIds)
                            {
                                FamilySymbol familysymbol = family.Document.GetElement(id) as FamilySymbol;
                                if ((null != familysymbol) && familysymbol.Name == name)
                                {
                                    using (Transaction changeSymbol = new Transaction(Doc, "Change Symbol Assignment"))
                                    {
                                        changeSymbol.Start();
                                        familySymbol = familysymbol;
                                        changeSymbol.Commit();
                                    }
                                    break;
                                }
                            }
                        }
                        IList<Element> columnss = new FilteredElementCollector(Doc).OfCategory(BuiltInCategory.OST_Columns).WhereElementIsElementType().ToElements();
                        FamilySymbol fs = null;
                        foreach (Element ele in columnss)
                        {
                            if (ele.Name == name)
                            {
                                fs = ele as FamilySymbol;
                            }
                        }
                        using (Transaction trans = new Transaction(Doc, "create columns"))
                        {
                            trans.Start();
                            try
                            {
                                if (!fs.IsActive)
                                {
                                    fs.Activate();
                                }
                                Doc.Create.NewFamilyInstance(midline, fs, collevel, structuralType);
                            }
                            catch (Exception ex)
                            {
                                TaskDialog.Show(ex.Message, ex.ToString());

                            }
                            trans.Commit();
                        }
                    }
                                      

                }
            }

            foreach (Arc ar in arcs)
            {
                GraphicsStyle gstyle = Doc.GetElement(ar.GraphicsStyleId) as GraphicsStyle;
                string layer = gstyle.GraphicsStyleCategory.Name;
                if (layer == _selectedlayer)
                {
                    XYZ c = ar.Center;
                    double d =Math.Round( (ar.Radius) * 2 * 304.8);

                    string name = null;
                    bool flag = true;

                    name = d.ToString() + "mm Diameter";

                    if (_selectedType == columntype[0])
                    {
                        IList<Element> rcolumns = new FilteredElementCollector(Doc).OfCategory(BuiltInCategory.OST_StructuralColumns).WhereElementIsElementType().ToElements();
                        foreach (var item in rcolumns)
                        {
                            if (item.Name == name)
                            {
                                flag = false;
                            }
                        }

                        FamilySymbol familySymbol = new FilteredElementCollector(Doc).OfCategory(BuiltInCategory.OST_StructuralColumns).WhereElementIsElementType().Where(x => x.Name == "12\"" || x.Name == "18\"" || x.Name == "24\"" || x.Name == "30\"").FirstOrDefault() as FamilySymbol;
                        
                        if (null == familySymbol)
                        {
                            MessageBox.Show("could not find the required family loaded. Please make sure to load the required families.");
                            return;    // could not find the family 
                        }
                        Family family = familySymbol.Family;
                        
                        Autodesk.Revit.DB.Document familyDocc = Doc.EditFamily(family);
                        if (null == familyDocc)
                        {
                            return;    // could not open a family for edit
                        }

                        FamilyManager familyManager = familyDocc.FamilyManager;
                        if (null == familyManager)
                        {
                            return;  // could not get a family manager
                        }

                        if (flag)
                        {
                            using (Transaction newFamilyTypeTransaction = new Transaction(familyDocc, "Add Type to Family"))
                            {
                                int changesMad = 0;
                                newFamilyTypeTransaction.Start();
                                FamilyType newFamilyTypee = null;
                                // add a new type and edit its parameters
                                try
                                {
                                    newFamilyTypee = familyManager.NewType(name);
                                }
                                catch (Exception ex)
                                {
                                    string message = ex.Message;
                                }


                                if (newFamilyTypee != null)
                                {
                                    // look for 'd' parameters and set them to 2 feet
                                    FamilyParameter familyParamm = familyManager.get_Parameter("b");
                                    if (null != familyParamm)
                                    {
                                        familyManager.Set(familyParamm, d / 304.8);
                                        changesMad += 1;
                                    }

                                }

                                if (1 == changesMad)   // set both paramaters?
                                {
                                    newFamilyTypeTransaction.Commit();
                                }
                                else   // could not make the change -> should roll back 
                                {
                                    newFamilyTypeTransaction.RollBack();
                                }
                                if (newFamilyTypeTransaction.GetStatus() != TransactionStatus.Committed)
                                {
                                    return;
                                }
                            }

                            // now update the Revit project with Family which has a new type
                            LoadOpts loadOptions = new LoadOpts();

                            // This overload is necessary for reloading an edited family
                            // back into the source document from which it was extracted
                            family = familyDocc.LoadFamily(Doc, loadOptions);
                            if (null != family)
                            {
                                // find the new type and assign it to FamilyInstance
                                ISet<ElementId> familySymbolIdss = family.GetFamilySymbolIds();
                                foreach (ElementId id in familySymbolIdss)
                                {
                                    FamilySymbol familysymbol = family.Document.GetElement(id) as FamilySymbol;
                                    if ((null != familysymbol) && familysymbol.Name == name)
                                    {
                                        using (Transaction changeSymbol = new Transaction(Doc, "Change Symbol Assignment"))
                                        {
                                            changeSymbol.Start();
                                            familySymbol = familysymbol;
                                            changeSymbol.Commit();
                                        }
                                        break;
                                    }
                                }
                            }
                        }

                        IList<Element> roundedcolumns = new FilteredElementCollector(Doc).OfCategory(BuiltInCategory.OST_StructuralColumns).WhereElementIsElementType().ToElements();
                        FamilySymbol fss = null;
                        foreach (Element ele in roundedcolumns)
                        {
                            if (ele.Name == name)
                            {
                                fss = ele as FamilySymbol;
                            }
                        }
                        using (Transaction tr = new Transaction(Doc, "create rounded columns"))
                        {
                            tr.Start();
                            try
                            {
                                if (!fss.IsActive)
                                {
                                    fss.Activate();
                                }
                                Doc.Create.NewFamilyInstance(c, fss, collevel, structuralType);
                            }
                            catch (Exception ex)
                            {
                                TaskDialog.Show(ex.Message, ex.ToString());

                            }
                            tr.Commit();
                        }
                    }
                    else if (_selectedType == columntype[1])
                    {
                        IList<Element> rcolumns = new FilteredElementCollector(Doc).OfCategory(BuiltInCategory.OST_Columns).WhereElementIsElementType().ToElements();
                        foreach (var item in rcolumns)
                        {
                            if (item.Name == name)
                            {
                                flag = false;
                            }
                        }

                        FamilySymbol familySymbol = new FilteredElementCollector(Doc).OfCategory(BuiltInCategory.OST_Columns).WhereElementIsElementType().Where(x => x.Name == "06\" Diameter" || x.Name == "12\" Diameter" || x.Name == "24\" Diameter").FirstOrDefault() as FamilySymbol;
                        
                        if (null == familySymbol)
                        {
                            MessageBox.Show("could not find the required family loaded. Please make sure to load the required families.");
                            return;    // could not find the family 
                        }
                        Family family = familySymbol.Family;
                        
                        Autodesk.Revit.DB.Document familyDocc = Doc.EditFamily(family);
                        if (null == familyDocc)
                        {
                            return;    // could not open a family for edit
                        }

                        FamilyManager familyManager = familyDocc.FamilyManager;
                        if (null == familyManager)
                        {
                            return;  // could not get a family manager
                        }

                        if (flag)
                        {
                            using (Transaction newFamilyTypeTransaction = new Transaction(familyDocc, "Add Type to Family"))
                            {
                                int changesMad = 0;
                                newFamilyTypeTransaction.Start();
                                FamilyType newFamilyTypee = null;
                                // add a new type and edit its parameters
                                try
                                {
                                    newFamilyTypee = familyManager.NewType(name);
                                }
                                catch (Exception ex)
                                {
                                    string message = ex.Message;
                                }


                                if (newFamilyTypee != null)
                                {
                                    // look for 'd' parameters and set them to 2 feet
                                    FamilyParameter familyParamm = familyManager.get_Parameter("Diameter");
                                    if (null != familyParamm)
                                    {
                                        familyManager.Set(familyParamm, d / 304.8);
                                        changesMad += 1;
                                    }

                                }

                                if (1 == changesMad)   // set both paramaters?
                                {
                                    newFamilyTypeTransaction.Commit();
                                }
                                else   // could not make the change -> should roll back 
                                {
                                    newFamilyTypeTransaction.RollBack();
                                }
                                if (newFamilyTypeTransaction.GetStatus() != TransactionStatus.Committed)
                                {
                                    return;
                                }
                            }

                            // now update the Revit project with Family which has a new type
                            LoadOpts loadOptions = new LoadOpts();

                            // This overload is necessary for reloading an edited family
                            // back into the source document from which it was extracted
                            family = familyDocc.LoadFamily(Doc, loadOptions);
                            if (null != family)
                            {
                                // find the new type and assign it to FamilyInstance
                                ISet<ElementId> familySymbolIdss = family.GetFamilySymbolIds();
                                foreach (ElementId id in familySymbolIdss)
                                {
                                    FamilySymbol familysymbol = family.Document.GetElement(id) as FamilySymbol;
                                    if ((null != familysymbol) && familysymbol.Name == name)
                                    {
                                        using (Transaction changeSymbol = new Transaction(Doc, "Change Symbol Assignment"))
                                        {
                                            changeSymbol.Start();
                                            familySymbol = familysymbol;
                                            changeSymbol.Commit();
                                        }
                                        break;
                                    }
                                }
                            }
                        }

                        IList<Element> roundedcolumns = new FilteredElementCollector(Doc).OfCategory(BuiltInCategory.OST_Columns).WhereElementIsElementType().ToElements();
                        FamilySymbol fss = null;
                        foreach (Element ele in roundedcolumns)
                        {
                            if (ele.Name == name)
                            {
                                fss = ele as FamilySymbol;
                            }
                        }
                        using (Transaction tr = new Transaction(Doc, "create rounded columns"))
                        {
                            tr.Start();
                            try
                            {
                                if (!fss.IsActive)
                                {
                                    fss.Activate();
                                }
                                Doc.Create.NewFamilyInstance(c, fss, collevel, structuralType);
                            }
                            catch (Exception ex)
                            {
                                TaskDialog.Show(ex.Message, ex.ToString());

                            }
                            tr.Commit();
                        }
                    }
                   
                }
            }
            
        }
        private static XYZ MidPoint(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            XYZ midPoint = new XYZ((x1 + x2) / 2, (y1 + y2) / 2, (z1 + z2) / 2);
            return midPoint;
        }
        private static double SideLength(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            double length;
            length = Math.Sqrt(Math.Pow((x2 - x1), 2) + Math.Pow((y2 - y1), 2) + Math.Pow((z2 - z1), 2)) * 304.8;
            return length;
        }

        #endregion         

    }
    class LoadOpts : IFamilyLoadOptions
    {
        public bool OnFamilyFound(bool familyInUse, out bool overwriteParameterValues)
        {
            overwriteParameterValues = true;
            return true;
        }

        public bool OnSharedFamilyFound(Family sharedFamily, bool familyInUse, out FamilySource source, out bool overwriteParameterValues)
        {
            source = FamilySource.Family;
            overwriteParameterValues = true;
            return true;
        }
    }
}
