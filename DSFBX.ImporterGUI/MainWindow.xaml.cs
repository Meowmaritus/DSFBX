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

        DSFBX.ModelViewer.MyGame modelViewer = null;

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

            if (context.Config.Manual_LastModelTypeDropdownOption == "Character")
            {
                ModelTypeDropdown.SelectedItem = ModelTypeDropdown_Character;
            }
            else if (context.Config.Manual_LastModelTypeDropdownOption == "Object")
            {
                ModelTypeDropdown.SelectedItem = ModelTypeDropdown_Object;
            }
            else if (context.Config.Manual_LastModelTypeDropdownOption == "Weapon")
            {
                ModelTypeDropdown.SelectedItem = ModelTypeDropdown_Weapon;
            }
            else if (context.Config.Manual_LastModelTypeDropdownOption == "Armor")
            {
                ModelTypeDropdown.SelectedItem = ModelTypeDropdown_Armor;
            }

            if (context.Config.Manual_LastArmorExtensionTypeDropdownOption == "Human")
            {
                ArmorExtensionTypeDropdown.SelectedItem = ArmorExtension_Human;
            }
            else if (context.Config.Manual_LastArmorExtensionTypeDropdownOption == "Hollow")
            {
                ArmorExtensionTypeDropdown.SelectedItem = ArmorExtension_Hollow;
            }
            else if (context.Config.Manual_LastArmorExtensionTypeDropdownOption == "Lowpoly")
            {
                ArmorExtensionTypeDropdown.SelectedItem = ArmorExtension_Lowpoly;
            }
        }

        void SaveConfig()
        {
            if (ModelTypeDropdown.SelectedItem == ModelTypeDropdown_Character)
            {
                context.Config.Manual_LastModelTypeDropdownOption = "Character";
            }
            else if (ModelTypeDropdown.SelectedItem == ModelTypeDropdown_Object)
            {
                context.Config.Manual_LastModelTypeDropdownOption = "Object";
            }
            else if (ModelTypeDropdown.SelectedItem == ModelTypeDropdown_Weapon)
            {
                context.Config.Manual_LastModelTypeDropdownOption = "Weapon";
            }
            else if (ModelTypeDropdown.SelectedItem == ModelTypeDropdown_Armor)
            {
                context.Config.Manual_LastModelTypeDropdownOption = "Armor";
            }

            if (ArmorExtensionTypeDropdown.SelectedItem == ArmorExtension_Human)
            {
                context.Config.Manual_LastArmorExtensionTypeDropdownOption = "Human";
            }
            else if (ArmorExtensionTypeDropdown.SelectedItem == ArmorExtension_Hollow)
            {
                context.Config.Manual_LastArmorExtensionTypeDropdownOption = "Hollow";
            }
            else if (ArmorExtensionTypeDropdown.SelectedItem == ArmorExtension_Lowpoly)
            {
                context.Config.Manual_LastArmorExtensionTypeDropdownOption = "Lowpoly";
            }

            string json = JsonConvert.SerializeObject(context.Config, Formatting.Indented);
            File.WriteAllText("DSFBX_Config.json", json);
        }

        private async void ButtonImport_Click(object sender, RoutedEventArgs e)
        {
            try
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

                if (context.Config.ImportSkeletonEnable && !File.Exists(context.Config.ImportSkeletonPath))
                {
                    MessageBox.Show("Selected skeleton import source file does not exist.");
                    return;
                }

                Importer.FbxPath = context.Config.InputFBX;

                if (ModelTypeDropdown.SelectedItem == ModelTypeDropdown_Character)
                {
                    Importer.OutputType = DSFBXOutputType.Character;
                }
                else if (ModelTypeDropdown.SelectedItem == ModelTypeDropdown_Object)
                {
                    Importer.OutputType = DSFBXOutputType.Object;
                }
                else if (ModelTypeDropdown.SelectedItem == ModelTypeDropdown_Armor)
                {
                    Importer.OutputType = DSFBXOutputType.Armor;
                }
                else if (ModelTypeDropdown.SelectedItem == ModelTypeDropdown_Weapon)
                {
                    Importer.OutputType = DSFBXOutputType.Weapon;
                }

                Importer.ModelID = context.Config.EntityModelID;
                Importer.EntityModelIndex = context.Config.ModelIndex;
                Importer.ScalePercent = context.Config.ScalePercent;
                Importer.ImportSkeletonPath =
                    context.Config.ImportSkeletonEnable
                    ? context.Config.ImportSkeletonPath : null;
                Importer.IsDoubleSided = context.Config.ImportDoubleSided;
                Importer.GenerateBackup = context.Config.GenerateBackup;
                Importer.ImportedSkeletonScalePercent = context.Config.ImportedSkeletonScalePercent;
                Importer.SceneRotation.X = (float)((context.Config.SceneRotationX / 180) * Math.PI);
                Importer.SceneRotation.Y = (float)((context.Config.SceneRotationY / 180) * Math.PI);
                Importer.SceneRotation.Z = (float)((context.Config.SceneRotationZ / 180) * Math.PI);
                Importer.ArmorExtension = "";
                if (Importer.OutputType == DSFBXOutputType.Armor)
                {
                    Importer.ArmorExtension = ArmorExtension.Dict[ArmorExtensionTypeDropdown.SelectedItem.ToString()];
                }
                //Importer.ArmorCopyHumanToHollow = context.Config.ArmorCopyHumanToHollow;
                Importer.ArmorCopyMaleLegsToFemale = context.Config.ArmorCopyMaleLegsToFemale;
                Importer.ArmorFixBodyNormals = context.Config.ArmorFixBodyNormals;
                Importer.RotateNormalsBackward = context.Config.RotateNormalsBackward;
                Importer.ConvertNormalsAxis = context.Config.ConvertNormalsAxis;
                Importer.OutputtedFiles = new List<string>();

                var successPTDE = false;
                var successDS1R = false;

                var specifiedPTDE = false;
                var specifiedDS1R = false;

                if (!string.IsNullOrWhiteSpace(context.Config.DarkSoulsExePath))
                {
                    if (!File.Exists(context.Config.DarkSoulsExePath))
                    {
                        MessageBox.Show("Selected Dark Souls PTDE EXE file does not exist. Model will not be imported to Dark Souls PTDE.");
                    }
                    else
                    {
                        specifiedPTDE = true;

                        Importer.InterrootDir = new FileInfo(context.Config.DarkSoulsExePath).DirectoryName;
                        Importer.IsRemaster = false;

                        successPTDE = await Importer.BeginImport();
                    }
                }
                else
                {
                    successPTDE = true;
                }

                if (!string.IsNullOrWhiteSpace(context.Config.DarkSoulsRemasteredExePath))
                {
                    if (!File.Exists(context.Config.DarkSoulsRemasteredExePath))
                    {
                        MessageBox.Show("Selected Dark Souls Remastered EXE file does not exist. Model will not be imported to Dark Souls Remastered.");
                    }
                    else
                    {
                        specifiedDS1R = true;

                        Importer.InterrootDir = new FileInfo(context.Config.DarkSoulsRemasteredExePath).DirectoryName;
                        Importer.IsRemaster = true;

                        successDS1R = await Importer.BeginImport();
                    }
                }
                else
                {
                    successDS1R = true;
                }



                if (!(specifiedPTDE || specifiedDS1R))
                {
                    MessageBox.Show("Neither a Dark Souls PTDE or Dark Souls Remastered EXE was specified. Nowhere to import to.",
                        "Neither EXE Specified", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else if (successPTDE && !successDS1R)
                {
                    MessageBox.Show("Dark Souls Remastered import failed. See output log for more information.",
                        "Dark Souls Remastered Import Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else if (!successPTDE && successDS1R)
                {
                    MessageBox.Show("Dark Souls PTDE import failed. See output log for more information.",
                        "Dark Souls PTDE Import Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else if (!successPTDE && !successDS1R)
                {
                    MessageBox.Show("Dark Souls PTDE and Dark Souls Remastered imports both failed. See output log for more information.",
                        "Dark Souls PTDE and Remastered Imports Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    if (context.Config.LaunchModelViewerAfterImport)
                    {
                        //System.Diagnostics.Process.Start(GetModelViewerExecutable(), $"\"{context.Config.OutputBND}\"");
                        //DSFBX.ModelViewer.App.Main();
                        //DSFBX.ModelViewer.Program.Main(new string[] { context.Config.OutputBND });

                        string[] inputFiles = null;

                        if (specifiedPTDE && successPTDE)
                        {
                            inputFiles = Importer
                                .OutputtedFiles
                                .Where(x => !x.ToUpper().EndsWith(".DCX"))
                                .ToArray();
                        }
                        else if (specifiedDS1R && successDS1R)
                        {
                            inputFiles = Importer
                                .OutputtedFiles
                                .Where(x => x.ToUpper().EndsWith(".DCX"))
                                .ToArray();
                        }

                        if (inputFiles != null && inputFiles.Length > 0)
                        {
                            if (modelViewer != null)
                            {
                                modelViewer.LoadNewModels(inputFiles);
                            }
                            else
                            {
                                modelViewer = new DSFBX.ModelViewer.MyGame();
                                modelViewer.IsQuickRunFromModelImporter = true;
                                modelViewer.inputFiles = inputFiles;

                                modelViewer.Exiting += ModelViewer_Exiting;

                                modelViewer.Run(Microsoft.Xna.Framework.GameRunBehavior.Synchronous);
                            }
                        }
                    }

                    if (context.Config.ForceReloadCHR && checkboxForceReloadCHR.IsEnabled)
                    {
                        if (specifiedDS1R)
                            ForceReloadCHR_DS1R();

                        if (specifiedPTDE)
                            ForceReloadCHR_PTDE();
                    }

                    if (context.Config.ForceReloadPARTS && checkboxForceReloadPARTS.IsEnabled)
                    {
                        if (specifiedDS1R)
                            ForceReloadPARTS_DS1R();

                        if (specifiedPTDE)
                            ForceReloadPARTS_PTDE();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Emergency Error Box", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ModelViewer_Exiting(object sender, EventArgs e)
        {
            modelViewer?.ModelListWindow.Close();
            modelViewer?.Exit();
            modelViewer = null;
        }

        private static string GetModelViewerExecutable()
        {
            return (new FileInfo(typeof(MainWindow).Assembly.Location).DirectoryName) + @"\DSFBX.ModelViewer.exe";
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
                Filter = "Executable Files (*.EXE)|*.EXE",
                CheckFileExists = true,
                CheckPathExists = true,
                Title = "Select Dark Souls PTDE EXE File..."
            };

            try
            {
                var defFileInfo = new FileInfo(context.Config.DarkSoulsExePath);

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
                context.Config.DarkSoulsExePath = dlg.FileName;
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

            if (modelViewer != null)
            {
                modelViewer.Exit();
                modelViewer.Dispose();
                modelViewer = null;
            }
        }

        private void ButtonSkeletonBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog()
            {
                Filter = "All Files (*.*)|*.*",
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

        private void ModelTypeDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CheckBoxArmorCopyMaleLegsToFemale.IsEnabled = ModelTypeDropdown.SelectedItem == ModelTypeDropdown_Armor;
            //CheckBoxArmorCopyHumanToHollow.IsEnabled = ModelTypeDropdown.SelectedItem == ModelTypeDropdown_Armor;
            CheckBoxArmorFixBodyNormals.IsEnabled = ModelTypeDropdown.SelectedItem == ModelTypeDropdown_Armor;

            checkboxForceReloadPARTS.IsEnabled = ((ModelTypeDropdown.SelectedItem == ModelTypeDropdown_Armor 
                || ModelTypeDropdown.SelectedItem == ModelTypeDropdown_Weapon));

            checkboxForceReloadCHR.IsEnabled = (ModelTypeDropdown.SelectedItem == ModelTypeDropdown_Character);
        }

        private void ButtonBrowseDS1R_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog()
            {
                Filter = "Executable Files (*.EXE)|*.EXE",
                CheckFileExists = true,
                CheckPathExists = true,
                Title = "Select Dark Souls Remastered EXE File..."
            };

            try
            {
                var defFileInfo = new FileInfo(context.Config.DarkSoulsRemasteredExePath);

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
                context.Config.DarkSoulsRemasteredExePath = dlg.FileName;
                SaveConfig();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AddRunToConsole("NOTE: If it says it can't find " +
                "Microsoft.Xna.Framework.Content.Pipeline.FbxImporter.dll upon clicking the Import button, you need " +
                "to download and install VC2010 Redist" +
                " https://www.microsoft.com/en-us/download/details.aspx?id=5555", 
                Colors.Yellow, Colors.Black);
        }

        private void ForceReloadCHR_DS1R()
        {
            var info = new System.Diagnostics.ProcessStartInfo()
            {
                FileName = "ModelReloaderDS1R\\ModelReloaderDS1R.exe",
                Arguments = $"c{context.Config.EntityModelID:D4}",
                CreateNoWindow = true,
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
            };
            System.Diagnostics.Process.Start(info);
        }

        private void ForceReloadPARTS_DS1R()
        {
            var info = new System.Diagnostics.ProcessStartInfo()
            {
                FileName = "ModelReloaderDS1R\\ModelReloaderDS1R.exe",
                Arguments = $"PARTS",
                CreateNoWindow = true,
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
            };
            System.Diagnostics.Process.Start(info);
        }

        private void ForceReloadCHR_PTDE()
        {
            string chrName = $"c{context.Config.EntityModelID:D4}";

            if (!DarkSoulsScripting.Hook.DARKSOULS.Attached)
            {
                if (!DarkSoulsScripting.Hook.DARKSOULS.TryAttachToDarkSouls(out string errorMsg))
                {
                    MessageBox.Show($"Failed to hook to Dark Souls: PTDE\n\n{errorMsg}");
                    return;
                }
            }

            DarkSoulsScripting.Hook.WByte(0x013784F3, 1);

            var stringAlloc = new DarkSoulsScripting.Injection.Structures.SafeRemoteHandle(chrName.Length * 2);

            DarkSoulsScripting.Hook.WBytes(stringAlloc.GetHandle(), Encoding.Unicode.GetBytes(chrName));

            DarkSoulsScripting.Hook.CallCustomX86((asm) =>
            {
                asm.Mov32(Managed.X86.X86Register32.ECX, stringAlloc.GetHandle().ToInt32());
                asm.RawAsmBytes(new byte[] { 0x8B, 0x35, 0x44, 0xD6, 0x37, 0x01 }); //mov esi,[0x0137D644]
                asm.Push32(Managed.X86.X86Register32.ECX);
                asm.Call(new IntPtr(0x00E3F440));
                asm.Retn();
            });
        }

        private void ForceReloadPARTS_PTDE()
        {
            if (!DarkSoulsScripting.Hook.DARKSOULS.Attached)
            {
                if (!DarkSoulsScripting.Hook.DARKSOULS.TryAttachToDarkSouls(out string errorMsg))
                {
                    MessageBox.Show($"Failed to hook to Dark Souls: PTDE\n\n{errorMsg}");
                    return;
                }
            }

            var thingAddr = DarkSoulsScripting.Hook.RInt32(0x0137D644);
            DarkSoulsScripting.Hook.WFloat(thingAddr + 0x138C, 1.0f);
            DarkSoulsScripting.Hook.WFloat(thingAddr + 0x1390, 10.0f);
        }

        private void ButtonDONATE_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.paypal.me/Meowmaritus");
        }
    }
}
