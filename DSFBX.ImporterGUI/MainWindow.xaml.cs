using DSFBX;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Threading;

namespace DSFBX_GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DSFBXImporter Importer = new DSFBXImporter();

        //ScaleTransform paragraphScale = new ScaleTransform(0.74, 0.74);

        public MainWindow()
        {
            InitializeComponent();

            if (File.Exists("DSFBX_Config.json"))
            {
                LoadConfig();
            }
            else
            {
                SaveConfig();
            }

            Importer.InfoTextOutputted += Importer_InfoTextOutputted;
            Importer.WarningTextOutputted += Importer_WarningTextOutputted;
            Importer.ErrorTextOutputted += Importer_ErrorTextOutputted;

            Importer.ImportStarted += Importer_ImportStarted;
            Importer.ImportEnding += Importer_ImportEnding;
        }

        private void Importer_ImportEnding(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                MainGrid.IsEnabled = true;
                Mouse.OverrideCursor = null;
            });
        }

        private void Importer_ImportStarted(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                MainGrid.IsEnabled = false;
                Mouse.OverrideCursor = Cursors.Wait;
            });

            Dispatcher.Invoke(() =>
            {
                SaveConfig();
            });
        }

        private void AddRunToConsole(string text, Color? boxColor = null, Color? color = null, bool isBold = false)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var r = new Run()
                {
                    Text = text,
                    FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal,
                };

                if (color.HasValue)
                {
                    r.Foreground = new SolidColorBrush(color.Value);
                }

                var p = new Paragraph(r);

                if (boxColor.HasValue)
                {
                    p.Margin = new Thickness(4);
                    p.Padding = new Thickness(4);
                    p.BorderBrush = SystemColors.ActiveBorderBrush;
                    p.BorderThickness = new Thickness(1);
                    p.Background = new SolidColorBrush(boxColor.Value);
                }
                else
                {
                    p.Margin = new Thickness(0);
                    p.Padding = new Thickness(0);
                }

                ConsoleOutputDocument.Blocks.Add(p);

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    TextBoxConsoleOutput.ScrollToEnd();
                }), DispatcherPriority.Background);

            }), DispatcherPriority.Background);
        }

        private void Importer_ErrorTextOutputted(object sender, DSFBXGenericEventArgs<string> e)
        {
            AddRunToConsole(e.Parameter, Colors.Red, Colors.White, true);
        }

        private void Importer_WarningTextOutputted(object sender, DSFBXGenericEventArgs<string> e)
        {
            AddRunToConsole(e.Parameter, Colors.Yellow, Colors.Black);
        }

        private void Importer_InfoTextOutputted(object sender, DSFBXGenericEventArgs<string> e)
        {
            AddRunToConsole(e.Parameter);
        }

        void LoadConfig()
        {
            string json = File.ReadAllText("DSFBX_Config.json");
            context.Config = JsonConvert.DeserializeObject<DSFBXConfig>(json);
        }

        void SaveConfig()
        {
            string json = JsonConvert.SerializeObject(context.Config, Formatting.Indented);
            File.WriteAllText("DSFBX_Config.json", json);
        }

        private async void ButtonImport_Click(object sender, RoutedEventArgs e)
        {
            if (context.Config.AutoClearOutput)
            {
                ConsoleOutputDocument.Blocks.Clear();
            }

            if (!File.Exists(context.Config.InputFBX))
            {
                MessageBox.Show("Selected input FBX file does not exist.");
                return;
            }

            if (!File.Exists(context.Config.OutputBND))
            {
                MessageBox.Show("Selected output entity BND file does not exist.");
                return;
            }

            if (context.Config.ImportSkeletonEnable && !File.Exists(context.Config.ImportSkeletonPath))
            {
                MessageBox.Show("Selected skeleton import source file does not exist.");
                return;
            }

            Importer.FbxPath = context.Config.InputFBX;
            Importer.EntityBndPath = context.Config.OutputBND;
            Importer.EntityModelIndex = context.Config.ModelIndex;
            Importer.ScalePercent = context.Config.ScalePercent;
            Importer.ImportSkeletonPath = 
                context.Config.ImportSkeletonEnable 
                ? context.Config.ImportSkeletonPath : null;

            var success = await Importer.BeginImport();

            if (!success)
            {
                MessageBox.Show("Import failed. See output log for more information.", 
                    "Import Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (context.Config.LaunchModelViewerAfterImport)
            {
                System.Diagnostics.Process.Start(GetModelViewerExecutable(), $"\"{context.Config.OutputBND}\"");
            }
        }

        private static string GetModelViewerExecutable()
        {
            return (new FileInfo(typeof(MainWindow).Assembly.Location).DirectoryName) + @"\ModelViewer\DS1MDV.exe";
        }

        static readonly char[] pathSep = new char[] { '\\', '/' };

        private void ButtonInputBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog()
            {
                Filter = "FBX Scene Files (*.FBX)|*.FBX",
                CheckFileExists = true,
                CheckPathExists = true,
                Title = "Select Input FBX File..."
            };

            try
            {
                var defFileInfo = new FileInfo(context.Config.InputFBX);

                string defDir = defFileInfo.DirectoryName;

                if (Directory.Exists(defDir))
                {
                    dlg.InitialDirectory = defDir;
                }

                if (File.Exists(defFileInfo.FullName))
                {
                    dlg.FileName = defFileInfo.Name;
                }
            }
            catch
            {
                dlg.InitialDirectory = Environment.CurrentDirectory;
                dlg.FileName = "";
            }

            if (dlg.ShowDialog() == true)
            {
                context.Config.InputFBX = dlg.FileName;
                SaveConfig();
            }
        }

        private void ButtonOutputBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog()
            {
                Filter = "Parts/Obj/Chr BND (*.*bnd)|*.*bnd|" +
                    "Parts BND Files (*.partsbnd)|*.partsbnd|" +
                    "Object BND Files (*.objbnd)|*.objbnd|" +
                    "Character BND Files (*.chrbnd)|*.chrbnd",
                CheckFileExists = true,
                CheckPathExists = true,
                Title = "Select Entity BND File..."
            };

            try
            {
                var defFileInfo = new FileInfo(context.Config.OutputBND);

                string defDir = defFileInfo.DirectoryName;

                if (Directory.Exists(defDir))
                {
                    dlg.InitialDirectory = defDir;
                }

                if (File.Exists(defFileInfo.FullName))
                {
                    dlg.FileName = defFileInfo.Name;
                }
            }
            catch
            {
                dlg.InitialDirectory = Environment.CurrentDirectory;
                dlg.FileName = "";
            }

            if (dlg.ShowDialog() == true)
            {
                context.Config.OutputBND = dlg.FileName;
                SaveConfig();
            }
        }

        private void RadioIndex0_Click(object sender, RoutedEventArgs e)
        {
            context.Config.ModelIndex = 0;
        }

        private void RadioIndex1_Click(object sender, RoutedEventArgs e)
        {
            context.Config.ModelIndex = 1;
        }

        private void RadioIndex2_Click(object sender, RoutedEventArgs e)
        {
            context.Config.ModelIndex = 2;
        }

        private void RadioIndex3_Click(object sender, RoutedEventArgs e)
        {
            context.Config.ModelIndex = 3;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveConfig();
        }

        private void ButtonSkeletonBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog()
            {
                Filter = "Parts/Obj/Chr BND (*.*bnd)|*.*bnd|" +
                    "Parts BND Files (*.partsbnd)|*.partsbnd|" +
                    "Object BND Files (*.objbnd)|*.objbnd|" +
                    "Character BND Files (*.chrbnd)|*.chrbnd",
                CheckFileExists = true,
                CheckPathExists = true,
                Title = "Select Entity BND File..."
            };

            try
            {
                var defFileInfo = new FileInfo(context.Config.ImportSkeletonPath);

                string defDir = defFileInfo.DirectoryName;

                if (Directory.Exists(defDir))
                {
                    dlg.InitialDirectory = defDir;
                }

                if (File.Exists(defFileInfo.FullName))
                {
                    dlg.FileName = defFileInfo.Name;
                }
            }
            catch
            {
                dlg.InitialDirectory = Environment.CurrentDirectory;
                dlg.FileName = "";
            }

            if (dlg.ShowDialog() == true)
            {
                context.Config.ImportSkeletonPath = dlg.FileName;
                SaveConfig();
            }
        }

        private void ButtonClearOutput_Click(object sender, RoutedEventArgs e)
        {
            ConsoleOutputDocument.Blocks.Clear();
        }
    }
}
