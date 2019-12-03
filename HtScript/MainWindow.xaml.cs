using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;

namespace HtScript
{

    public enum IndexChoice
    {
        NoIndex,
        NonUniqueIndex,
        UniqueIndex
    }
    public class Config
    {
        public string User { get; set; }
        public string Project { get; set; }
        public string SqlScriPath { get; set; }
        public string UpdateScriptPaths { get; set; }
        public string IndexScriptPath { get; set; }
        public string ForeignKeyScriptPath { get; set; }
        public string PrimaryKeyScriptPath { get; set; }
        public string Suffix { get; set; }
    }

    public class Table : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public string Name { get; set; }
        public string Comment { get; set; }
        public ObservableCollection<TableColumn> TableColumns { get; set; }
        protected void OnPropertyChanged(string name)
        {
        PropertyChangedEventHandler handler = PropertyChanged;
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
    }

    public class TableColumn
    {
        public string Name { get; set; }
        public string PrimaryKey { get; set; }
        public string Type { get; set; }
        public bool Nullable { get; set; } = true;
        public string Comment { get; set; } = "";
        public string ForeignKeyTo { get; set; }
        public IndexChoice CreateIndex { get; set; }

    }

    public partial class MainWindow : Window
    {
        private void DataGrid_CellGotFocus(object sender, RoutedEventArgs e)
        {
            // Lookup for the source to be DataGridCell
            if (e.OriginalSource.GetType() == typeof(DataGridCell))
            {
                // Starts the Edit on the row;
                DataGrid grd = (DataGrid)sender;
                grd.BeginEdit(e);

                Control control = GetFirstChildByType<Control>(e.OriginalSource as DataGridCell);
                if (control != null)
                {
                    control.Focus();
                }
            }
        }

        private T GetFirstChildByType<T>(DependencyObject prop) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(prop); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild((prop), i) as DependencyObject;
                if (child == null)
                    continue;

                T castedProp = child as T;
                if (castedProp != null)
                    return castedProp;

                castedProp = GetFirstChildByType<T>(child);

                if (castedProp != null)
                    return castedProp;
            }
            return null;
        }
        public Table Table { get; set; } = new Table { TableColumns = new ObservableCollection<TableColumn>(), Name = "hallo"};
        public Config Config { get; set; } =
            new Config { Project = "SENSO ORACLE",
                Suffix = "",
                User = "EDE",
                SqlScriPath = @"c:\users\ederer\desktop\testsqlscri"
            };
        public MainWindow()
        {
            for(int i = 0; i < 100; ++i)
            {
                Table.TableColumns.Add(new TableColumn());
            }
            InitializeComponent();
            DataContext = this;
            dgColumns.ItemsSource = Table.TableColumns;
        }

        private void btnCreateHtScript_Click(object sender, RoutedEventArgs e)
        {
            var x = Table.Name;
            File.WriteAllText("htbewope.sql", getHtScript(Config, Table));
            File.WriteAllText("hsbewope.sql", getHsScript(Config, Table));
        }


        private string getHtScript(Config config, Table table)
        {
            var lines = new List<string>();
            lines.Add("--                                   DevelopGroup (SIGMA GmbH) Erlangen");
            lines.Add("-- Projekt: " + config.Project);
            lines.Add("---");
            lines.Add("-- Beschreibung:");
            lines.Add("-- Inhalt: " + table.Comment);
            lines.Add("---");
            lines.Add("-- Historie:");
            lines.Add("---");
            lines.Add("-- " + DateTime.Today.ToString("dd.MM.yyyy") + " " + config.User);
            lines.Add("-----------------------------------------------------------------------------");
            lines.Add("CREATE TABLE " + table.Name + " (");
            lines.Add(");");
            // add columns
            lines.Add("COMMENT ON TABLE " + table.Name + " IS");
            lines.Add("'" + table.Comment);
            lines.Add("'Skript: " + config.Suffix + "';");
            lines.Add("");
            lines.Add("-- Ende des Skripts.");
            return String.Join("\r\n", lines);
        }

        private string getHsScript(Config config, Table table)
        {
            var sequenceName = "SEQ_" + table.Name + "_NR";
            var lines = new List<string>();
            lines.Add("PROMPT SEQUENZ " + sequenceName);
            lines.Add("--                                                        SIGMA GmbH Erlangen");
            lines.Add("-- Projekt: " + config.Project);
            lines.Add("-- Inhalt: Erzeugung und Beschreibung der Sequenz " + sequenceName);
            lines.Add("---");
            lines.Add("-- Beschreibung:");
            lines.Add("-- Primärschlüsselversorgung der Tabelle " + table.Name);
            lines.Add("---");
            lines.Add("-- Historie:");
            lines.Add("---");
            lines.Add("-- " + DateTime.Today.ToString("dd.MM.yyyy") + " " + config.User);
            lines.Add("-----------------------------------------------------------------------------");
            lines.Add("DROP SEQUENCE " + sequenceName + ";");
            lines.Add("DECLARE");
            lines.Add("  nret NUMBER;");
            lines.Add("  vcmd VARCHAR2(200);");
            lines.Add("BEGIN");
            lines.Add("  SELECT NVL(MAX(NR),0)+1 INTO nret FROM " + table.Name + ";");
            lines.Add("  IF nret > 999999999999 OR nret < 1 THEN");
            lines.Add("    nret := 1;");
            lines.Add("  END IF;");
            lines.Add("  vcmd:= 'CREATE SEQUENCE &GRUNDMDT.." + sequenceName + " START WITH ' ");
            lines.Add("         || to_char(nret) || ' minvalue 1 maxvalue 999999999999 NOCYCLE NOCACHE';");
            lines.Add("  EXECUTE IMMEDIATE vcmd;");
            lines.Add("END; ");
            lines.Add("/ ");
            lines.Add("-- Ende des Skripts.");
            return String.Join("\r\n", lines);
        }


    }
}
