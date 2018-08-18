using MeowDSIO.DataTypes.FLVER;
using Microsoft.Xna.Framework;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
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
        private List<object> DefaultModelListXamlGeneratedComponents = new List<object>();

        public bool RequestExit = false;
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

        public Dictionary<int, CheckBox> CheckMap_Model = new Dictionary<int, CheckBox>();

        public Dictionary<int, CheckBox> CheckMap_Dummy = new Dictionary<int, CheckBox>();

        public Dictionary<int, CheckBox> CheckMap_Bone = new Dictionary<int, CheckBox>();

        public int NumSubmeshes = 0;

        public List<int> DummyIDs = new List<int>();

        public List<Microsoft.Xna.Framework.Color> DummyColors = new List<Microsoft.Xna.Framework.Color>();

        public List<string> BoneNames = new List<string>();
        public List<string> SubmeshNames = new List<string>();
        public List<FlverVector3> BoneScales = new List<FlverVector3>();
        public List<int> BoneIndents = new List<int>();


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
                CheckBox c = new CheckBox()
                {
                    IsThreeState = false,
                    IsChecked = true,
                    Content = new TextBlock()
                    {
                        Text = SubmeshNames[i] ?? $"(Submesh #{(i + 1)})",
                    }
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
                    VerticalContentAlignment = VerticalAlignment.Center
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
    }
}