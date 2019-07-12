using MeowDSIO.DataTypes.FLVER;
using Microsoft.Xna.Framework;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;

namespace DSFBX.ModelViewer
{
    public partial class ModelList : Window
    {
        bool DISABLE_BONE_WEIGHTS_COMBO_BOX_EVENTS_FATCAT = false;
        public int CurrentBoneWeightView = -1;

        private List<object> DefaultModelListXamlGeneratedComponents = new List<object>();

        public bool RequestExit = false;
        public event EventHandler CheckChangedOrSomething_FullMeshRebuild;
        public event EventHandler CheckChangedOrSomething;
        public event EventHandler CheckChangedOrSomething_Grid;
        //public event EventHandler DummySizeChanged;
        public event EventHandler RandomizedDummyColors;

        private void CheckChangeEvent()
        {
            EventHandler handler = this.CheckChangedOrSomething;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private void CheckChangeEvent_FullMeshRebuild()
        {
            EventHandler handler = this.CheckChangedOrSomething_FullMeshRebuild;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private void CheckChangeEvent_Grid()
        {
            EventHandler handler = this.CheckChangedOrSomething_Grid;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        //private void DummySizeChangeEvent()
        //{
        //    EventHandler handler = this.DummySizeChanged;
        //    if (handler != null)
        //    {
        //        handler(this, EventArgs.Empty);
        //    }
        //}

        private void RandomDummyColorsEvent()
        {
            EventHandler handler = this.RandomizedDummyColors;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        public bool TEMPORARILY_DISABLE_EVENTS_FATCAT = false;

        public string ModelName = null;

        public Dictionary<int, CheckBox> CheckMap_Model = new Dictionary<int, CheckBox>();

        public Dictionary<int, CheckBox> CheckMap_Dummy = new Dictionary<int, CheckBox>();

        public Dictionary<int, CheckBox> CheckMap_Bone = new Dictionary<int, CheckBox>();

        public int NumSubmeshes = 0;

        public List<int> DummyIDs = new List<int>();

        public List<FlverDummy> Dummies = new List<FlverDummy>();

        public List<Microsoft.Xna.Framework.Color> DummyColors = new List<Microsoft.Xna.Framework.Color>();

        public List<string> BoneNames = new List<string>();
        public List<FlverBone> ActualBones = new List<FlverBone>();
        public List<string> SubmeshNames = new List<string>();
        public List<string> SubmeshMaterialNames = new List<string>();
        public List<FlverVector3> BoneScales = new List<FlverVector3>();
        public List<int> BoneIndents = new List<int>();
        public List<FlverSubmesh> FlverSubmeshes = new List<FlverSubmesh>();

        public bool ShowBoneBoxes
        {
            get
            {
                bool? isChecked = this.ShowBoneBoxesCheckbox.IsChecked;
                return (isChecked.GetValueOrDefault() ? isChecked.HasValue : false);
            }
        }

        public bool ShowDummyDirectionalIndicators
        {
            get
            {
                bool? isChecked = this.ShowDummyDirections.IsChecked;
                return (isChecked.GetValueOrDefault() ? isChecked.HasValue : false);
            }
        }

        public ModelList()
        {
            this.InitializeComponent();

            DefaultModelListXamlGeneratedComponents = new List<object>();
            foreach (var defaultXamlGeneratedComponent in MainListView.Items)
            {
                DefaultModelListXamlGeneratedComponents.Add(defaultXamlGeneratedComponent);
            }
        }

        public bool? GetCheckBone(int i)
        {
            return this.CheckMap_Bone[i].IsChecked;
        }

        public bool GetCheckDummy(int typeID)
        {
            bool? isChecked = this.CheckMap_Dummy[typeID].IsChecked;
            return (isChecked.GetValueOrDefault() ? isChecked.HasValue : false);
        }

        public bool GetCheckModel(int i)
        {
            bool? isChecked = this.CheckMap_Model[i].IsChecked;
            return (isChecked.GetValueOrDefault() ? isChecked.HasValue : false);
        }

        public void InitCheckboxes()
        {
            MainListView.Items.Clear();
            foreach (var item in DefaultModelListXamlGeneratedComponents)
            {
                MainListView.Items.Add(item);
            }

            DISABLE_BONE_WEIGHTS_COMBO_BOX_EVENTS_FATCAT = true;

            ComboBoxBoneWeightView.Items.Clear();

            ComboBoxBoneWeightView.Items.Add("None");

            foreach (var b in BoneNames)
            {
                ComboBoxBoneWeightView.Items.Add(b);
            }

            if (CurrentBoneWeightView >= 0 && CurrentBoneWeightView < ComboBoxBoneWeightView.Items.Count - 1)
            {
                ComboBoxBoneWeightView.SelectedIndex = CurrentBoneWeightView + 1;
            }
            else
            {
                ComboBoxBoneWeightView.SelectedIndex = 0;
            }

            DISABLE_BONE_WEIGHTS_COMBO_BOX_EVENTS_FATCAT = false;

            CheckMap_Bone = new Dictionary<int, CheckBox>();
            CheckMap_Model = new Dictionary<int, CheckBox>();
            CheckMap_Dummy = new Dictionary<int, CheckBox>();

            this.MainListView.Items.Add(new Label()
            {
                Content = "SUBMESHES:",
                FontWeight = FontWeights.Bold,
                IsHitTestVisible = false,
            });

            for (int i = 0; i < this.NumSubmeshes; i++)
            {
                var meshTb = new TextBlock();

                meshTb.Inlines.Add(new System.Windows.Documents.Run()
                {
                    Text = SubmeshNames[i] ?? $"(Submesh #{(i + 1)})",
                    FontWeight = FontWeights.Bold,
                });

                meshTb.Inlines.Add(new System.Windows.Documents.Run()
                {
                    Text = "  " + (SubmeshMaterialNames[i] ?? $"(No Material Assigned)"),
                    Foreground = SystemColors.ControlDarkDarkBrush
                });

                CheckBox c = new CheckBox()
                {
                    IsThreeState = false,
                    IsChecked = true,
                    Content = meshTb,
                };
                c.Click += Checkbox_Click;
                this.MainListView.Items.Add(c);
                this.CheckMap_Model.Add(i, c);
            }

            this.MainListView.Items.Add(new Separator());
            this.MainListView.Items.Add(new Label()
            {
                Content = "DUMMIES:",
                FontWeight = FontWeights.Bold,
                IsHitTestVisible = false,
            });

            for (int i = 0; i < this.DummyIDs.Count; i++)
            {
                CheckBox checkBox = new CheckBox();
                checkBox.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb
                    (this.DummyColors[i].A,
                    this.DummyColors[i].R,
                    this.DummyColors[i].G,
                    this.DummyColors[i].B));
                checkBox.IsThreeState = false;
                checkBox.IsChecked = new bool?(true);
                CheckBox c = checkBox;
                c.Content = new TextBlock()
                {
                    Text = $"dmy{this.DummyIDs[i]}"
                };
                c.Click += Checkbox_Click;
                this.MainListView.Items.Add(c);
                this.CheckMap_Dummy.Add(this.DummyIDs[i], c);
            }

            this.MainListView.Items.Add(new Separator());
            this.MainListView.Items.Add(new Label()
            {
                Content = "BONES:",
                FontWeight = FontWeights.Bold,
                IsHitTestVisible = false,
            });

            for (int i = 0; i < this.BoneNames.Count; i++)
            {
                var tb = new TextBlock();
                var nameTb = new TextBlock();

                nameTb.Inlines.Add(new System.Windows.Documents.Run()
                {
                    Text = $"{this.BoneNames[i]}",
                    FontWeight = FontWeights.Bold,
                });

                nameTb.Inlines.Add(new System.Windows.Documents.Run()
                {
                    Text = $" <{this.BoneScales[i].X}, {this.BoneScales[i].Y}, {this.BoneScales[i].Z}>",
                    Foreground = SystemColors.ControlDarkDarkBrush
                });

                CheckBox c = new CheckBox()
                {
                    IsThreeState = true,
                    IsChecked = true,
                    Content = nameTb,
                    Height = 16,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    //Margin = new Thickness(BoneIndents[i] * 8, 0, 0, 0),
                };

                for (int j = 0; j < BoneIndents[i]; j++)
                {
                    tb.Inlines.Add(new System.Windows.Documents.InlineUIContainer(new Border()
                    {
                        Width = 2,
                        BorderThickness = new Thickness(2, 0, 2, 0),
                        Padding = new Thickness(0),
                        Margin = new Thickness(8, 0, 8, 0),
                        BorderBrush = SystemColors.ControlDarkDarkBrush,
                        VerticalAlignment = VerticalAlignment.Center,
                        Height = 16,
                    }));
                }

                tb.Inlines.Add(new System.Windows.Documents.InlineUIContainer(c));

                c.Click += Checkbox_Click;
                this.MainListView.Items.Add(tb);
                this.CheckMap_Bone.Add(i, c);
            }
        }

        private void Checkbox_Click(object sender, RoutedEventArgs e)
        {
            if (TEMPORARILY_DISABLE_EVENTS_FATCAT)
                return;

            this.CheckChangeEvent();
        }

        private void Checkbox_Click_FullMeshRebuild(object sender, RoutedEventArgs e)
        {
            if (TEMPORARILY_DISABLE_EVENTS_FATCAT)
                return;

            this.CheckChangeEvent_FullMeshRebuild();
        }

        public void UpdateDummyColors()
        {
            int i = 0;
            foreach (var kvp in CheckMap_Dummy)
            {
                kvp.Value.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb
                    (this.DummyColors[i].A,
                    this.DummyColors[i].R,
                    this.DummyColors[i].G,
                    this.DummyColors[i].B));
                i++;
            }

            CheckChangeEvent();
        }

        public void SetTopmost(bool topmost)
        {
            base.Topmost = topmost;
        }

        private void ButtonToggleAllBones_Click(object sender, RoutedEventArgs e)
        {
            TOGGLE_ALL_BONES();
        }

        private void ButtonToggleAllDummy_Click(object sender, RoutedEventArgs e)
        {
            TOGGLE_ALL_DUMMY();
        }

        public void TOGGLE_ALL_BONES()
        {
            Dispatcher.Invoke(() =>
            {
                TEMPORARILY_DISABLE_EVENTS_FATCAT = true;

                var allChecked = CheckMap_Bone.Values.All(x => x.IsChecked == true);

                if (allChecked)
                {
                    foreach (var kvp in CheckMap_Bone)
                    {
                        kvp.Value.IsChecked = null;
                    }
                }
                else
                {
                    var allBoxed = CheckMap_Bone.Values.All(x => x.IsChecked == null);

                    if (allBoxed)
                    {
                        foreach (var kvp in CheckMap_Bone)
                        {
                            kvp.Value.IsChecked = false;
                        }
                    }
                    else
                    {
                        foreach (var kvp in CheckMap_Bone)
                        {
                            kvp.Value.IsChecked = true;
                        }
                    }


                }

                TEMPORARILY_DISABLE_EVENTS_FATCAT = false;

                CheckChangeEvent();
            });
        }

        public void TOGGLE_ALL_DUMMY()
        {
            Dispatcher.Invoke(() =>
            {
                TEMPORARILY_DISABLE_EVENTS_FATCAT = true;

                var allChecked = CheckMap_Dummy.Values.All(x => x.IsChecked == true);

                if (allChecked)
                {
                    foreach (var kvp in CheckMap_Dummy)
                    {
                        kvp.Value.IsChecked = false;
                    }
                }
                else
                {
                    foreach (var kvp in CheckMap_Dummy)
                    {
                        kvp.Value.IsChecked = true;
                    }
                }

                TEMPORARILY_DISABLE_EVENTS_FATCAT = false;

                CheckChangeEvent();
            });
        }

        public void TOGGLE_ALL_SUBMESH()
        {
            Dispatcher.Invoke(() =>
            {
                TEMPORARILY_DISABLE_EVENTS_FATCAT = true;

                var allChecked = CheckMap_Model.Values.All(x => x.IsChecked == true);

                if (allChecked)
                {
                    foreach (var kvp in CheckMap_Model)
                    {
                        kvp.Value.IsChecked = false;
                    }
                }
                else
                {
                    foreach (var kvp in CheckMap_Model)
                    {
                        kvp.Value.IsChecked = true;
                    }
                }

                TEMPORARILY_DISABLE_EVENTS_FATCAT = false;

                CheckChangeEvent();
            });
        }

        public void REROLL_DUMMY_COLORS()
        {
            RandomDummyColorsEvent();
        }

        private void ButtonToggleAllSubmesh_Click(object sender, RoutedEventArgs e)
        {
            TOGGLE_ALL_SUBMESH();
        }

        private void ButtonRerollDummyColors_Click(object sender, RoutedEventArgs e)
        {
            REROLL_DUMMY_COLORS();
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            CheckChangeEvent();
        }

        private void Slider_ValueChanged_Grid(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            CheckChangeEvent_Grid();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = !RequestExit;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void ComboBoxBoneWeightView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DISABLE_BONE_WEIGHTS_COMBO_BOX_EVENTS_FATCAT)
                return;

            CurrentBoneWeightView = ComboBoxBoneWeightView.SelectedIndex - 1;
            CheckChangeEvent_FullMeshRebuild();
        }

        private void ContextMenuBone_CopyName_Click(object sender, RoutedEventArgs e)
        {
            var possibleChk = CheckMap_Bone.Where(kvp => kvp.Value == MainListView.SelectedItem);
            if (possibleChk.Any())
                Clipboard.SetText(BoneNames[possibleChk.First().Key], TextDataFormat.UnicodeText);
        }

        private void ViewAllModelInfo()
        {
            var sb = new StringBuilder();

            var faceSetFlagValues = (FlverFaceSetFlags[])Enum.GetValues(typeof(FlverFaceSetFlags));

            sb.AppendLine($"Model: {ModelName}");
            sb.AppendLine();
            sb.AppendLine("Submeshes:");
            for (int i = 0; i < FlverSubmeshes.Count; i++)
            {
                var sm = FlverSubmeshes[i];
                sb.AppendLine($"    {(sm.GetName() ?? $"<Unnamed Submesh #{(i + 1)}>")}");

                if (sm.Material == null)
                {
                    continue;
                }

                sb.AppendLine($"        {sm.Material.Name}");
                sb.AppendLine($"        {sm.Material.MTDName}");

                foreach (var p in sm.Material.Parameters)
                {
                    sb.AppendLine($"            {p.ToString()}");
                }

                sb.AppendLine($"        {sm.FaceSets.Count} Face Sets. Flags of Face Sets:");

                for (int j = 0; j < sm.FaceSets.Count; j++)
                {
                    List<FlverFaceSetFlags> flagsThisThingHas = new List<FlverFaceSetFlags>();
                    foreach (var flag in faceSetFlagValues)
                    {
                        if ((sm.FaceSets[j].Flags & flag) != 0)
                        {
                            flagsThisThingHas.Add(flag);
                        }
                    }
                    sb.AppendLine($"            Face Set {(j + 1)}: {string.Join(", ", flagsThisThingHas)}");
                }
            }
            sb.AppendLine();
            sb.AppendLine("Bones:");
            for (int i = 0; i < BoneNames.Count; i++)
            {
                sb.Append("    ");
                for (int j = 0; j < BoneIndents[i]; j++)
                {
                    sb.Append("  ");
                }
                sb.AppendLine("-" + BoneNames[i] + $"[POS<{ActualBones[i].Translation.X}, {ActualBones[i].Translation.Y}, {ActualBones[i].Translation.Z}> " +
                    $"ROT<{(ActualBones[i].EulerRadian.X / Math.PI * 180)}�, {(ActualBones[i].EulerRadian.Y / Math.PI * 180)}�, {(ActualBones[i].EulerRadian.Z / Math.PI * 180)}�> " +
                    $"SCL<{ActualBones[i].Scale.X}, {ActualBones[i].Scale.Y}, {ActualBones[i].Scale.Z}>]" +
                    (ActualBones[i].BoundingBoxMin != null ? $"BBm<{ActualBones[i].BoundingBoxMin.X}, {ActualBones[i].BoundingBoxMin.Y}, {ActualBones[i].BoundingBoxMin.Z}>" : "BBm<NULL>") +
                    (ActualBones[i].BoundingBoxMax != null ? $"BBM<{ActualBones[i].BoundingBoxMax.X}, {ActualBones[i].BoundingBoxMax.Y}, {ActualBones[i].BoundingBoxMax.Z}>" : "BBM<NULL>"));
            }

            sb.AppendLine();
            sb.AppendLine("Dummies:");

            foreach (var dmy in Dummies)
            {
                sb.AppendLine($"    {dmy.TypeID} [ParentNode:\"{(dmy.ParentBoneIndex >= 0 ? BoneNames[dmy.ParentBoneIndex] : "<None>")}\" " +
                    $"FollowBone:\"{(dmy.SomeSortOfParentIndex >= 0 ? BoneNames[dmy.SomeSortOfParentIndex] : "<None>")}\" " +
                    $"POS:<{dmy.Position.X}, {dmy.Position.Y}, {dmy.Position.Z}>" +
                    $"UP:<{dmy.Row2.X}, {dmy.Row2.Y}, {dmy.Row2.Z}>" +
                    $"FWD:<{dmy.Row3.X}, {dmy.Row3.Y}, {dmy.Row3.Z}>]");
            }

            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine();

            var infoWindow = new ModelListInfoPopup();
            infoWindow.TextBoxInfo.Text = sb.ToString();
            infoWindow.ShowDialog();
        }

        private void ContextMenuViewAllInfo_Click(object sender, RoutedEventArgs e)
        {
            ViewAllModelInfo();
        }

        private void ButtonViewAllModelInfo_Click(object sender, RoutedEventArgs e)
        {
            ViewAllModelInfo();
        }
    }
}