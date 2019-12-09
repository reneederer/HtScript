using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;
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
using System.Xml;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Collections.Specialized;

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
        public string User { get; set; } = "";
        public string Project { get; set; } = "";
        public string SqlScriPath { get; set; } = "";
        public string UpdateScriptPaths { get; set; } = "";
        public string IndexScriptPath { get; set; } = "";
        public string ForeignKeyScriptPath { get; set; } = "";
        public string PrimaryKeyScriptPath { get; set; } = "";
        public string Suffix { get; set; } = "";
    }

    public class Table
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private string name = "";
        public string Name { get { return name; } set { name = value.Trim().ToUpper(); } }
        public string Comment { get; set; } = "";
        public BindingList<TableColumn> TableColumns { get; set; } = new BindingList<TableColumn>();
        protected void OnPropertyChanged(string name)
        {
        PropertyChangedEventHandler handler = PropertyChanged;
            PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
    }

    public class TableColumn : INotifyPropertyChanged
    {
        private string name = "";
        private bool primaryKey = false;
        private string type = "";
        private bool nullable = true;
        private string comment = "";
        private string foreignKeyTo = "";
        private IndexChoice createIndex = IndexChoice.NoIndex;
        public string Name {
            get { return name; }
            set {
                name = value.ToUpper().Trim();
                if(name == "NR")
                {
                    PrimaryKey = true;
                    Comment = "PK";
                    Type = "NUMBER(12)";
                    Nullable = true;
                    ForeignKeyTo = "";
                    CreateIndex = IndexChoice.NoIndex;
                }
                else if (name.EndsWith("NR"))
                {
                    PrimaryKey = false;
                    Type = "NUMBER(12)";
                    ForeignKeyTo = $"{name.Substring(0, name.EndsWith("_NR") ? name.Length - 3 : name.Length - 2) }.NR";
                    Nullable = true;
                    CreateIndex = IndexChoice.NonUniqueIndex;
                }
                else if(name == "AKTUELL")
                {
                    PrimaryKey = false;
                    Type = "VARCHAR2(1)";
                    Comment = "Aktuell? (j/n)";
                    Nullable = false;
                    CreateIndex = IndexChoice.NoIndex;
                }
                else if(name == "BEZEICH")
                {
                    PrimaryKey = false;
                    Type = "VARCHAR2(50)";
                    Comment = "Bezeichnung";
                    Nullable = false;
                    CreateIndex = IndexChoice.NoIndex;
                }
                else if(name == "NAME")
                {
                    PrimaryKey = false;
                    Type = "VARCHAR2(50)";
                    Comment = "Name";
                    Nullable = false;
                    CreateIndex = IndexChoice.NoIndex;
                }
                else if(name == "MANDANT_NR")
                {
                    PrimaryKey = false;
                    Type = "NUMBER(5)";
                    Comment = "Mandanten-Nummer (kenmdt.nr), für Policy-Mandant";
                    Nullable = true;
                    CreateIndex = IndexChoice.NoIndex;
                }
                else if(name == "SORT")
                {
                    PrimaryKey = false;
                    Type = "NUMBER(5)";
                    Comment = "Sortierung";
                    Nullable = false;
                    CreateIndex = IndexChoice.NoIndex;
                }
                else if(name == "KENNUNG")
                {
                    PrimaryKey = false;
                    Type = "VARCHAR2(1)";
                    Comment = "Kennung";
                    Nullable = false;
                    CreateIndex = IndexChoice.NoIndex;
                }
                else
                {
                    Type = "VARCHAR2(30)";
                }
                OnPropertyChanged("PrimaryKey");
                OnPropertyChanged("Type");
                OnPropertyChanged("Nullable");
                OnPropertyChanged("CreateIndex");
            }
        }
        public bool PrimaryKey { get { return primaryKey; } set { primaryKey = value; } }
        public string Type { get { return type; } set { type = value.ToUpper(); } }
        public bool Nullable { get { return nullable; } set { nullable = value; } }
        public string Comment {
            get { return comment; }
            set { comment = value;
                OnPropertyChanged("Comment");
            }
        }
        public string ForeignKeyTo {
            get { return foreignKeyTo; }
            set {
                foreignKeyTo = value.ToUpper();
                var dotIndex = foreignKeyTo.IndexOf(".");
                if (foreignKeyTo.IndexOf(".") >= 0)
                {
                    Comment = $"FK zu {foreignKeyTo.Substring(0, dotIndex)}({foreignKeyTo.Substring(dotIndex + 1)})";
                    var x = Comment;
                }
                OnPropertyChanged("ForeignKeyTo");
            }
        }
        public IndexChoice CreateIndex { get { return createIndex; } set { createIndex = value; } }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

    }

    public partial class MainWindow : Window
    {
        private IEnumerable<Config> loadConfigs()
        {
            XmlDocument doc = new XmlDocument();
            doc.Load("config.xml");
            var user = doc.SelectSingleNode("/config/user").InnerText;

            foreach (XmlNode row in doc.SelectNodes("/config/settings"))
            {
                yield return new Config
                {
                    User = user,
                    Project = row.SelectSingleNode(".//project").InnerText,
                    SqlScriPath = row.SelectSingleNode(".//sqlscriPath").InnerText,
                    UpdateScriptPaths = row.SelectSingleNode(".//updateScriptPaths").InnerText,
                    PrimaryKeyScriptPath = row.SelectSingleNode(".//primaryKeyScriptPath").InnerText,
                    IndexScriptPath = row.SelectSingleNode(".//indexScriptPath").InnerText,
                    ForeignKeyScriptPath = row.SelectSingleNode(".//foreignKeyScriptPath").InnerText,
                    Suffix = row.SelectSingleNode(".//suffix").InnerText
                };
            }
        }

        public Table Table { get; set; } = new Table();
        public List<Config> Configs { get; set; }
        public MainWindow()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var enc1252 = Encoding.GetEncoding(1252);


            for (int i = 0; i < 100; ++i)
            {
                Table.TableColumns.Add(new TableColumn());
            }
            InitializeComponent();
            Table.TableColumns.ListChanged += tableColumnsChanged;

            Configs = loadConfigs().ToList();
            Configs.ToList().ForEach(item => cmbProject.Items.Add(item.Project));
            DataContext = this;
            dgColumns.ItemsSource = Table.TableColumns;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
        private void tableColumnsChanged(object sender, ListChangedEventArgs e)
        {
            enableSubmitButton();
        }


        private int svnLock(string arg, bool unlock = false)
        {
            using (var process = new Process())
            {
                process.StartInfo.FileName = "svn.exe";
                process.StartInfo.Arguments = (unlock ? "un" : "") + "lock " + arg;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                process.WaitForExit();
                if (process.ExitCode != 0 && !unlock)
                {
                    svnLock(arg, true);
                    return process.ExitCode;
                }
                return process.ExitCode;
            }
        }
        private void svnAdd(string arg)
        {
            using (var process = new Process())
            {
                process.StartInfo.FileName = "svn.exe";
                process.StartInfo.Arguments = "add " + arg;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                process.WaitForExit();
                var x = process.ExitCode;
            }
        }

        private void btnCreateHtScript_Click(object sender, RoutedEventArgs e)
        {
            var config = Configs[cmbProject.SelectedIndex];
            var updateScriptPaths = config.UpdateScriptPaths.Split(';').Select(x => x.Trim());
            var quotedUpdateScripts = String.Join(' ', updateScriptPaths);
            svnLock(quotedUpdateScripts, true);
            if (svnLock(quotedUpdateScripts) != 0)
            {
                MessageBox.Show("Svn-Lock gescheitert für\r\n" + quotedUpdateScripts);
                return;
            }

            var foreignKeyStrings = getForeignKeyStrings(Table);
            var htScriptPath = System.IO.Path.Combine(config.SqlScriPath, ("ht" + Table.Name + config.Suffix + ".sql").ToLower());
            var hsScriptPath = System.IO.Path.Combine(config.SqlScriPath, ("hs" + Table.Name + config.Suffix + ".sql").ToLower());
            File.WriteAllText(htScriptPath, getHtScript(Configs[cmbProject.SelectedIndex], Table, System.IO.Path.GetFileName(htScriptPath)), Encoding.GetEncoding(1252));
            File.WriteAllText(hsScriptPath, getHsScript(Configs[cmbProject.SelectedIndex], Table), Encoding.GetEncoding(1252));
            var foreignKeyScriptLines = File.ReadAllLines(Configs[cmbProject.SelectedIndex].ForeignKeyScriptPath, Encoding.GetEncoding(1252));
            var foreignKeyScriptLinesBefore =
                foreignKeyScriptLines.TakeWhile(line => !line.ToUpper().StartsWith("ALTER TABLE ") || String.Compare(line.ToUpper(), "ALTER TABLE " + Table.Name) == -1);
            var newForeignKeyScriptLines =
                foreignKeyScriptLinesBefore.Concat(foreignKeyStrings.Append("")).Concat(foreignKeyScriptLines.Skip(foreignKeyScriptLinesBefore.Count()));
            if (foreignKeyStrings.Count() >= 1)
            {
                File.WriteAllLines(config.ForeignKeyScriptPath, newForeignKeyScriptLines, Encoding.GetEncoding(1252));
            }

            var primaryKeyString = getPrimaryKeyString(Table);
            var primaryKeyScriptLines = File.ReadAllLines(Configs[cmbProject.SelectedIndex].PrimaryKeyScriptPath, Encoding.GetEncoding(1252));
            var primaryKeyScriptLinesBefore =
                primaryKeyScriptLines.TakeWhile(line => !line.Trim().ToUpper().StartsWith("ALTER TABLE ") || String.Compare(line.ToUpper(), "ALTER TABLE " + Table.Name) == -1);
            var newPrimaryKeyScriptLines =
                primaryKeyScriptLinesBefore.Append(primaryKeyString).Append("").Concat(primaryKeyScriptLines.Skip(primaryKeyScriptLinesBefore.Count()));
            if (!String.IsNullOrWhiteSpace(primaryKeyString))
            {
                File.WriteAllLines(config.PrimaryKeyScriptPath, newPrimaryKeyScriptLines, Encoding.GetEncoding(1252));
            }

            var indexScriptLines = File.ReadAllLines(Configs[cmbProject.SelectedIndex].IndexScriptPath, Encoding.GetEncoding(1252));
            var indexStrings = getIndexStrings(Table);
            if (indexStrings.Count() >= 1)
            {
                File.WriteAllLines(config.IndexScriptPath, insertIndexLines(indexScriptLines, indexStrings), Encoding.GetEncoding(1252));
            }

            foreach(var updateScriptPath in updateScriptPaths)
            {
                var content = File.ReadAllLines(updateScriptPath, Encoding.GetEncoding(1252));
                var versionSeparatorLineNr = -1;
                var lastLineBeforeSeparatorLineNr = -1;

                for(int i = content.Length - 1; i >= 0; --i)
                {
                    if(content[i].Trim().ToUpper() == "-- VERSION-SEPARATOR")
                    {
                        versionSeparatorLineNr = i;
                        continue;
                    }
                    if(versionSeparatorLineNr != -1 && content[i].Trim() != "")
                    {
                        lastLineBeforeSeparatorLineNr = i;
                        break;
                    }
                }

                var newContent =
                    content
                      .Take(lastLineBeforeSeparatorLineNr + 1)
                      .Append("")
                      .Append("@" + System.IO.Path.GetFileName(htScriptPath))
                      .Append("@" + System.IO.Path.GetFileName(hsScriptPath))
                      .Append(primaryKeyString)
                      .Concat(foreignKeyStrings)
                      .Concat(indexStrings)
                      .Append("")
                      .Append("")
                      .Concat(content.Skip(versionSeparatorLineNr));
                File.WriteAllLines(updateScriptPath, newContent, Encoding.GetEncoding(1252));
            }

            svnAdd("\"" + htScriptPath + "\" \"" + hsScriptPath + "\"");
            MessageBox.Show("Fertig");
        }

        private IEnumerable<string> insertIndexLines(IEnumerable<string> oldLines, IEnumerable<string> newLines)
        {
            IEnumerable<string> modifiedLines = oldLines;
            for(int newLineIndex = 0; newLineIndex < newLines.Count(); ++newLineIndex)
            {
                var newLine = newLines.ToList()[newLineIndex];
                var before =
                    modifiedLines.TakeWhile(line => !line.Trim().ToUpper().StartsWith("CREATE ") || String.Compare(line.Substring(21).ToUpper(), newLine.Substring(21).ToUpper()) == -1);
                var after = modifiedLines.Skip(before.Count());
                modifiedLines =
                    before.Append(newLine);
                if (newLineIndex == newLines.Count() - 1)
                {
                    modifiedLines = modifiedLines.Append("");
                }
                modifiedLines = modifiedLines.Concat(after);
            }
            return modifiedLines;

        }

        private string getPrimaryKeyString(Table table)
        {
            var primaryKeyColumns = table.TableColumns.Where(x => x.PrimaryKey).Select(x => x.Name);
            if (primaryKeyColumns.Count() == 1)
            {
                var primaryKeyColumn = primaryKeyColumns.First();
                return "ALTER TABLE " + table.Name + " ADD CONSTRAINT PK_" + table.Name + " PRIMARY KEY(&MDTNR." + primaryKeyColumn + ") USING INDEX TABLESPACE SENSIN;";
            }
            else
            {
                return "";
            }
        }


        private IEnumerable<string> getForeignKeyStrings(Table table)
        {
            foreach(var column in table.TableColumns.Where(column => !String.IsNullOrEmpty(column.ForeignKeyTo)).OrderBy(column => column.Name))
            {
                yield return "ALTER TABLE " + table.Name + " ADD CONSTRAINT FK_" + table.Name + "_" + column.Name +
                             " FOREIGN KEY(&MDTNR." + column.Name + ") REFERENCES " + String.Concat(column.ForeignKeyTo.TakeWhile(c => c != '.')) + ";";

            }
        }
        private IEnumerable<string> getIndexStrings(Table table)
        {
            foreach(var column in table.TableColumns.Where(column => column.CreateIndex != IndexChoice.NoIndex).OrderBy(column => column.Name))
            {
                var isUnique = column.CreateIndex == IndexChoice.UniqueIndex;
                var indexName = (isUnique ? "U" : "I") + (table.Name + "_" + column.Name).PadRight(31);
                yield return "CREATE " + (isUnique ? "UNIQUE" : "      ") + " INDEX " + indexName +
                             " ON " + table.Name + "(&MDTNR." + column.Name + ") TABLESPACE SENSIN;";
            }
        }

        private string getHtScript(Config config, Table table, string htScriptFile)
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
            var columns = table.TableColumns.Where(x => !String.IsNullOrWhiteSpace(x.Name)).ToList();
            for(var columnIndex = 0; columnIndex < columns.Count; ++columnIndex)
            {
                var column = columns[columnIndex];
                if(String.IsNullOrWhiteSpace(column.Name)) { continue; }
                var comma = (columnIndex == columns.Count - 1) ? "" : ",";
                lines.Add(column.Name.PadRight(25) + column.Type.PadRight(25) +
                          (!column.Nullable && !column.PrimaryKey ? "NOT NULL" : "        ") + comma +  (column.Comment.StartsWith("--") ? "" : "-- ") + column.Comment);
            }
            lines.Add(");");
            lines.Add("COMMENT ON TABLE " + table.Name + " IS");
            lines.Add("'" + table.Comment);
            lines.Add("Skript: " + htScriptFile + "';");
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

        private void cmbProject_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            enableSubmitButton();
        }

        private void dgColumns_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var uiElement = e.OriginalSource as UIElement;
            if(uiElement == null)
            {
                return;
            }
            else if (e.Key == Key.Enter || e.Key == Key.Down)
            {
                e.Handled = true;
                uiElement.MoveFocus(new TraversalRequest(FocusNavigationDirection.Down));
            }
            else if (e.Key == Key.Up)
            {
                e.Handled = true;
                uiElement.MoveFocus(new TraversalRequest(FocusNavigationDirection.Up));
            }
            else if (e.Key == Key.Tab && (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
            {
                e.Handled = true;
                uiElement.MoveFocus(new TraversalRequest(FocusNavigationDirection.Left));
            }
        }

        private void btnEditConfig_Click(object sender, RoutedEventArgs e)
        {
            using(var p = new Process())
            {
                p.StartInfo.FileName = @"C:\Program Files\Notepad++\notepad++.exe";
                p.StartInfo.Arguments = System.IO.Path.Combine(System.AppContext.BaseDirectory, "config.xml");
                MessageBox.Show("Änderungen an der Konfigurations-Datei werden erst übernommen, nachdem die Applikation neu gestartet wurde.", "Hinweis");
                p.Start();
            }
        }

        private void tbtableName_TextChanged(object sender, TextChangedEventArgs e)
        {
            enableSubmitButton();
        }

        private void enableSubmitButton()
        {
            btnCreateHtScript.IsEnabled =
                cmbProject.SelectedIndex >= 0
                && !String.IsNullOrWhiteSpace(tbtableName.Text)
                && Table.TableColumns.Where(x => !String.IsNullOrWhiteSpace(x.Name)).Count() >= 1;
        }
    }




}
