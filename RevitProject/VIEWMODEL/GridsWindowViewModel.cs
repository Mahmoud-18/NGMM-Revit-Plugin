using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;

namespace RevitProject
{
    public class GridsWindowViewModel : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        protected void onPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                      
        }

        #endregion

        #region Constructor
        public GridsWindowViewModel()
        {
            CadLoad();
            OK = new Command(DoneCommand);
            RenumberGrid = new Command(GridStyle);
            Done = new Command(Close);
        }
        #endregion

        #region Properties & Fields

        Document Doc = GridsCommand.Doc;       
        private IList<Arc> arcs = new List<Arc>();
        private IList<Line> lines = new List<Line>();
        private List<Grid> VerticalGrids = new List<Grid>();
        private List<Grid> HorizontalGrids = new List<Grid>();
        private bool IsDimCreated = false;

        public Command OK { get; set; }
        public Command RenumberGrid { get; set; }

        public Command Done { get; set; }
        public IList<string> Layersname { get; set; } = new List<string>();
        public static IList<string> GridsBubble { get; set; } = new List<string>() { "Show at side 1","Show at side 2" ,"Show at both sides", "Hide from both sides" };
        public static IList<string> GridsNameStyles { get; set; } = new List<String>() { "1,2,3,...","A,B,C,...", "a,b,c,...", "I,II,III,..." };

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
        private string _selectedVerticalNumbering = GridsNameStyles[0];

        public string SelectedVerticalNumbering 
        {
            get { return _selectedVerticalNumbering; }
            set
            {

                _selectedVerticalNumbering = value;

                onPropertyChanged(nameof(SelectedVerticalNumbering));
            }
        }
        private string _selectedHorizontalNumbering = GridsNameStyles[1];

        public string SelectedHorizontalNumbering
        {
            get { return _selectedHorizontalNumbering; }
            set
            {

                _selectedHorizontalNumbering = value;

                onPropertyChanged(nameof(SelectedHorizontalNumbering));
            }
        }

        private string _selectedGridBubble = GridsBubble[1];

        public string SelectedGridBubble
        {
            get { return _selectedHorizontalNumbering; }
            set
            {

                _selectedGridBubble = value;

                onPropertyChanged(nameof(SelectedGridBubble));
            }
        }

        private Boolean _reverseVertical;

        public Boolean ReverseVertical
        {
            get { return _reverseVertical; }
            set
            {
                _reverseVertical = value;
                onPropertyChanged(nameof(ReverseVertical));
            }
        }
        private Boolean _reverseHorizontal;

        public Boolean ReverseHorizontal
        {
            get { return _reverseHorizontal; }
            set
            {
                _reverseHorizontal = value;
                onPropertyChanged(nameof(ReverseHorizontal));
            }
        }

        private Boolean _dimensions = true;

        public Boolean Dimensions
        {
            get { return _dimensions; }
            set
            {
                _dimensions = value;
                onPropertyChanged(nameof(Dimensions));
            }
        }

        #endregion

        #region Methods
        public void GridStyle(Object parameter)
        {
            GridSorting();
            RenumberVerticalGrids();
            RenumberHorizontalGrids();
            BubbleVisability();

            if (_dimensions)
            {
                AddDimensions(VerticalGrids);
                AddDimensions(HorizontalGrids);
                IsDimCreated= true;
            }
            else
            {
                if (IsDimCreated)
                {
                    var dimensions = new FilteredElementCollector(Doc).OfClass(typeof(Dimension)).ToElements().ToList();
                    foreach(Dimension dim in dimensions)
                    {
                        using (Transaction t = new Transaction(Doc, "Delete Dimension"))
                        {
                            t.Start();
                            Doc.Delete(dim.Id);
                            t.Commit();
                        }
                    }
                    IsDimCreated= false;
                }
            }

        }
        public void Close(Object parameter)
        {
            GridStyle(parameter);
            (parameter as GridsWindow).Close();
        }
        public void CadLoad()
        {
            IList<ElementId> cadimports = (IList<ElementId>)new FilteredElementCollector(Doc).OfClass(typeof(ImportInstance))
                .WhereElementIsNotElementType().ToElementIds();
           
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
                                if (go2 is Arc)
                                {
                                    GraphicsStyle gstyle = Doc.GetElement(go2.GraphicsStyleId) as GraphicsStyle;
                                    string layer = gstyle.GraphicsStyleCategory.Name;
                                    if (!Layersname.Contains(layer))
                                    {
                                        Layersname.Add(layer);
                                    }
                                    arcs.Add(go2 as Arc);
                                }
                                if (go2 is Line)
                                {
                                    GraphicsStyle gstyle = Doc.GetElement(go2.GraphicsStyleId) as GraphicsStyle;
                                    string layer = gstyle.GraphicsStyleCategory.Name;
                                    if (!Layersname.Contains(layer))
                                    {
                                        Layersname.Add(layer);
                                    }
                                    lines.Add(go2 as Line);
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

        public void BubbleVisability()
        {
            List<Grid> grids = new FilteredElementCollector(Doc).OfCategory(BuiltInCategory.OST_Grids).WhereElementIsNotElementType().Cast<Grid>().ToList();
            View view = Doc.ActiveView;
           
            if (_selectedGridBubble == GridsBubble[0])
            {
                foreach (var gr in grids)
                {
                    double grl1 = gr.Curve.GetEndPoint(0).X + gr.Curve.GetEndPoint(0).Y;
                    double grl2 = gr.Curve.GetEndPoint(1).X + gr.Curve.GetEndPoint(1).Y;
                    using (Transaction tx = new Transaction(Doc, "Bubbleview"))
                    {
                        tx.Start();                       
                        {
                            if (grl1 > grl2)
                            {
                                if (!gr.IsBubbleVisibleInView(DatumEnds.End0, view) && !gr.IsBubbleVisibleInView(DatumEnds.End1, view))
                                {
                                    gr.ShowBubbleInView(DatumEnds.End0, view);
                                    gr.HideBubbleInView(DatumEnds.End1, view);
                                }
                                else if (gr.IsBubbleVisibleInView(DatumEnds.End0, view) && gr.IsBubbleVisibleInView(DatumEnds.End1, view))
                                {
                                    gr.HideBubbleInView(DatumEnds.End1, view);
                                }
                                else if ((gr.IsBubbleVisibleInView(DatumEnds.End1, view) && !gr.IsBubbleVisibleInView(DatumEnds.End0, view)))
                                {
                                    gr.ShowBubbleInView(DatumEnds.End0, view);
                                    gr.HideBubbleInView(DatumEnds.End1, view);
                                }
                            }
                            else
                            {
                                if (!gr.IsBubbleVisibleInView(DatumEnds.End0, view) && !gr.IsBubbleVisibleInView(DatumEnds.End1, view))
                                {
                                    gr.ShowBubbleInView(DatumEnds.End1, view);
                                    gr.HideBubbleInView(DatumEnds.End0, view);
                                }
                                else if (gr.IsBubbleVisibleInView(DatumEnds.End0, view) && gr.IsBubbleVisibleInView(DatumEnds.End1, view))
                                {
                                    gr.HideBubbleInView(DatumEnds.End0, view);
                                }
                                else if ((!gr.IsBubbleVisibleInView(DatumEnds.End1, view) && gr.IsBubbleVisibleInView(DatumEnds.End0, view)))
                                {
                                    gr.ShowBubbleInView(DatumEnds.End1, view);
                                    gr.HideBubbleInView(DatumEnds.End0, view);
                                }
                            }
                                                                                                            
                        }
                        tx.Commit();
                    }
                }
            }
            if (_selectedGridBubble == GridsBubble[1])
            {
                foreach (var gr in grids)
                {
                    double grl1 = gr.Curve.GetEndPoint(0).X + gr.Curve.GetEndPoint(0).Y;
                    double grl2 = gr.Curve.GetEndPoint(1).X + gr.Curve.GetEndPoint(1).Y;
                    using (Transaction tx = new Transaction(Doc, "Bubbleview"))
                    {
                        tx.Start();
                        {
                            if (grl1 > grl2)
                            {
                                if (!gr.IsBubbleVisibleInView(DatumEnds.End0, view) && !gr.IsBubbleVisibleInView(DatumEnds.End1, view))
                                {
                                    gr.HideBubbleInView(DatumEnds.End0, view);
                                    gr.ShowBubbleInView(DatumEnds.End1, view);
                                }
                                else if (gr.IsBubbleVisibleInView(DatumEnds.End0, view) && gr.IsBubbleVisibleInView(DatumEnds.End1, view))
                                {
                                    gr.HideBubbleInView(DatumEnds.End0, view);
                                }
                                else if ((gr.IsBubbleVisibleInView(DatumEnds.End0, view) && !gr.IsBubbleVisibleInView(DatumEnds.End1, view)))
                                {
                                    gr.ShowBubbleInView(DatumEnds.End1, view);
                                    gr.HideBubbleInView(DatumEnds.End0, view);
                                }
                            }
                            else
                            {
                                if (!gr.IsBubbleVisibleInView(DatumEnds.End0, view) && !gr.IsBubbleVisibleInView(DatumEnds.End1, view))
                                {
                                    gr.HideBubbleInView(DatumEnds.End1, view);
                                    gr.ShowBubbleInView(DatumEnds.End0, view);
                                }
                                else if (gr.IsBubbleVisibleInView(DatumEnds.End0, view) && gr.IsBubbleVisibleInView(DatumEnds.End1, view))
                                {
                                    gr.HideBubbleInView(DatumEnds.End1, view);
                                }
                                else if ((!gr.IsBubbleVisibleInView(DatumEnds.End0, view) && gr.IsBubbleVisibleInView(DatumEnds.End1, view)))
                                {
                                    gr.ShowBubbleInView(DatumEnds.End0, view);
                                    gr.HideBubbleInView(DatumEnds.End1, view);
                                }
                            }

                        }
                        tx.Commit();
                    }
                }
            }
            if (_selectedGridBubble == GridsBubble[2])
            {
                foreach (var gr in grids)
                {
                    double grl1 = gr.Curve.GetEndPoint(0).X + gr.Curve.GetEndPoint(0).Y;
                    double grl2 = gr.Curve.GetEndPoint(1).X + gr.Curve.GetEndPoint(1).Y;
                    using (Transaction tx = new Transaction(Doc, "Bubbleview"))
                    {
                        tx.Start();
                        if (!gr.IsBubbleVisibleInView(DatumEnds.End0, view) && !gr.IsBubbleVisibleInView(DatumEnds.End1, view))
                        {
                            gr.ShowBubbleInView(DatumEnds.End0,view);
                            gr.ShowBubbleInView(DatumEnds.End1, view);
                        }
                        else if ((gr.IsBubbleVisibleInView(DatumEnds.End0, view) && !gr.IsBubbleVisibleInView(DatumEnds.End1, view)) || ((!gr.IsBubbleVisibleInView(DatumEnds.End0, view) && gr.IsBubbleVisibleInView(DatumEnds.End1, view))))
                        {
                            if(!gr.IsBubbleVisibleInView(DatumEnds.End0, view))
                            {
                                gr.ShowBubbleInView(DatumEnds.End0, view);
                            }
                            else
                            {
                                gr.ShowBubbleInView(DatumEnds.End1, view);
                            }                          
                        }                       
                        tx.Commit();
                    }
                }
            }
            if (_selectedGridBubble == GridsBubble[3])
            {
                foreach (var gr in grids)
                {
                    double grl1 = gr.Curve.GetEndPoint(0).X + gr.Curve.GetEndPoint(0).Y;
                    double grl2 = gr.Curve.GetEndPoint(1).X + gr.Curve.GetEndPoint(1).Y;
                    using (Transaction tx = new Transaction(Doc, "Bubbleview"))
                    {
                        tx.Start();
                        if ((gr.IsBubbleVisibleInView(DatumEnds.End0, view) && gr.IsBubbleVisibleInView(DatumEnds.End1, view)))
                        {
                            gr.HideBubbleInView(DatumEnds.End0, view);
                            gr.HideBubbleInView(DatumEnds.End1, view);
                        }
                        else if ((gr.IsBubbleVisibleInView(DatumEnds.End0, view) && !gr.IsBubbleVisibleInView(DatumEnds.End1, view)) || ((!gr.IsBubbleVisibleInView(DatumEnds.End0, view) && gr.IsBubbleVisibleInView(DatumEnds.End1, view))))
                        {
                            if (gr.IsBubbleVisibleInView(DatumEnds.End0, view))
                            {
                                gr.HideBubbleInView(DatumEnds.End0, view);
                            }
                            else
                            {
                                gr.HideBubbleInView(DatumEnds.End1, view);
                            }
                        }
                        tx.Commit();
                    }
                }
            }

            
        }
        public void AddDimensions(List<Grid> grids)
        {
            ReferenceArray Gridreferences = new ReferenceArray();
            
            using (Transaction trans = new Transaction(Doc, "Add Dimensions Between Grids"))
            {
                trans.Start();
                if (!IsDimCreated)
                {
                    for (int i = 0; i < grids.Count - 1; i++)
                    {

                        Grid grid1 = grids[i] as Grid;
                        Grid grid2 = grids[i + 1] as Grid;
                        Line line = null ;
                        Reference r1 = new Reference(grid1);
                        Reference r2 = new Reference(grid2);

                        XYZ startPoint1 = grid1.Curve.GetEndPoint(0);
                        XYZ endPoint1 = grid1.Curve.GetEndPoint(1);
                        XYZ startPoint2 = grid2.Curve.GetEndPoint(0);
                        XYZ endPoint2 = grid2.Curve.GetEndPoint(1);
                        
                        if (startPoint1.X-startPoint2.X == 0 || startPoint1.Y-startPoint2.Y == 0)
                        {

                             line = Line.CreateBound(startPoint1, startPoint2);
                        }
                        else if (startPoint1.X-startPoint2.X !=0 && startPoint1.Y-startPoint2.Y != 0)
                        {
                            if (startPoint1.X - endPoint2.X == 0 || startPoint1.Y - endPoint2.Y == 0)
                             line = Line.CreateBound(startPoint1, endPoint2);
                            else
                            {
                                line = Line.CreateBound(startPoint1, startPoint2);
                            }
                        }                       
                        Gridreferences.Append(r1);
                        Gridreferences.Append(r2);

                        try
                        {
                            Doc.Create.NewDimension(Doc.ActiveView, line, Gridreferences);
                        }
                        catch (Exception ex)
                        {

                        }
                        Gridreferences.Clear();
                    }
                }
                
                trans.Commit();
            }

        }

        private void SelectedNumberingStyle(List<Grid> griddirection,string selectedNumberingStyle)
        {
            for (int i = 0; i < griddirection.Count; i++)
            {
                if (selectedNumberingStyle == GridsNameStyles[0])
                {
                    griddirection[i].Name = (i + 1).ToString();
                }
                else if (selectedNumberingStyle == GridsNameStyles[1])
                {
                    griddirection[i].Name = ToLetters(i + 1);
                }
                else if (selectedNumberingStyle == GridsNameStyles[2])
                {
                    griddirection[i].Name = ToLetters(i + 1).ToLower();
                }
                else if (selectedNumberingStyle == GridsNameStyles[3])
                {
                    griddirection[i].Name = ToRoman(i + 1);
                }               
            }
        }
        public void RenumberHorizontalGrids()
        {           
           
            using (Transaction t = new Transaction(Doc, "Renumber Grids"))
            {
                t.Start();
                SelectedNumberingStyle(HorizontalGrids, _selectedHorizontalNumbering);                          
                Doc.Regenerate();
                t.Commit();
            }
        }
        public void RenumberVerticalGrids()
        {
            using (Transaction t = new Transaction(Doc, "Renumber Grids"))
            {
                t.Start();
                SelectedNumberingStyle(VerticalGrids, _selectedVerticalNumbering);
                Doc.Regenerate();
                t.Commit();
            }
        }
        private void GridSorting()
        {
            
            List<Grid> grids = new FilteredElementCollector(Doc).OfCategory(BuiltInCategory.OST_Grids).WhereElementIsNotElementType().Cast<Grid>().ToList();
            using (Transaction t = new Transaction(Doc, "Sort Grids"))
            {
                HorizontalGrids.Clear();
                VerticalGrids.Clear();
                t.Start();
                int i = 0;
                foreach (Grid grid in grids)
                {
                    Curve curve = grid.Curve;
                    XYZ startPoint = curve.GetEndPoint(0);
                    XYZ endPoint = curve.GetEndPoint(1);
                    if (Math.Abs(startPoint.Y - endPoint.Y) < Math.Abs(startPoint.X - endPoint.X))
                    {
                        HorizontalGrids.Add(grid);
                        grid.Name = "grid" + ++i;
                    }
                    else
                    {
                        VerticalGrids.Add(grid);
                        grid.Name = "grid" + ++i;
                    }
                }
                if (_reverseHorizontal)
                {
                    HorizontalGrids = HorizontalGrids.OrderBy(g => g.Curve.GetEndPoint(0).Y > g.Curve.GetEndPoint(1).Y? g.Curve.GetEndPoint(0).Y : g.Curve.GetEndPoint(1).Y).ToList();
                }
                else
                {
                    HorizontalGrids = HorizontalGrids.OrderByDescending(g => g.Curve.GetEndPoint(0).Y > g.Curve.GetEndPoint(1).Y ? g.Curve.GetEndPoint(0).Y : g.Curve.GetEndPoint(1).Y).ToList();
                }
                if (_reverseVertical)
                {
                    VerticalGrids = VerticalGrids.OrderBy(g => g.Curve.GetEndPoint(0).X > g.Curve.GetEndPoint(1).X ? g.Curve.GetEndPoint(0).X : g.Curve.GetEndPoint(1).X).ToList();
                }              
                else
                {
                    VerticalGrids = VerticalGrids.OrderByDescending(g => g.Curve.GetEndPoint(0).X > g.Curve.GetEndPoint(1).X ? g.Curve.GetEndPoint(0).X : g.Curve.GetEndPoint(1).X).ToList();
                }                                           
                Doc.Regenerate();
                t.Commit();
            }
        }
        public void DoneCommand(Object parameter)
        {
            Document Doc = GridsCommand.Doc;
            foreach (Line l in lines)
            {
                GraphicsStyle gstyle = Doc.GetElement(l.GraphicsStyleId) as GraphicsStyle;
                string layer = gstyle.GraphicsStyleCategory.Name;
                if (layer == Selectedlayer)
                {                   
                    using (Transaction transs = new Transaction(Doc, "create"))
                    {
                        transs.Start();
                        try
                        {
                            Autodesk.Revit.DB.Grid gg = Grid.Create(Doc, l);
                           
                        }
                        catch (Exception ex)
                        {
                            TaskDialog.Show(ex.Message, ex.ToString());

                        }
                        transs.Commit();
                    }

                }
                
            }
            foreach (Arc arc in arcs)
            {
                GraphicsStyle gstyle = Doc.GetElement(arc.GraphicsStyleId) as GraphicsStyle;
                string layer = gstyle.GraphicsStyleCategory.Name;
                if (layer == Selectedlayer)
                {                   
                    using (Transaction transs = new Transaction(Doc, "create"))
                    {
                        transs.Start();
                        try
                        {
                            Autodesk.Revit.DB.Grid gg = Grid.Create(Doc, arc);                            
                        }
                        catch (Exception ex)
                        {
                            TaskDialog.Show(ex.Message, ex.ToString());

                        }
                        transs.Commit();
                    }
                }
            }
        }
        private static string ToRoman(int num)
        {
            string[] thousands = { "", "M", "MM", "MMM" };
            string[] hundreds = { "", "C", "CC", "CCC", "CD", "D", "DC", "DCC", "DCCC", "CM" };
            string[] tens = { "", "X", "XX", "XXX", "XL", "L", "LX", "LXX", "LXXX", "XC" };
            string[] ones = { "", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX" };

            int thousandsIndex = num / 1000;
            int hundredsIndex = (num % 1000) / 100;
            int tensIndex = (num % 100) / 10;
            int onesIndex = num % 10;

            return thousands[thousandsIndex] + hundreds[hundredsIndex] + tens[tensIndex] + ones[onesIndex];
        }
        private static string ToLetters(int number)
        {
            string result = "";
            while (number > 0)
            {
                int modulo = (number - 1) % 26;
                result = (char)('A' + modulo) + result;
                number = (number - modulo) / 26;
            }
            return result;
        }

        #endregion
    }
}