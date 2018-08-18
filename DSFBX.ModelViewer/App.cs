using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Windows;

namespace DSFBX.ModelViewer
{
    public class App : Application
    {
        public App()
        {
        }

        [DebuggerNonUserCode]
        [GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
        public void InitializeComponent()
        {
            base.StartupUri = new Uri("MainWindow.xaml", UriKind.Relative);
        }

        [DebuggerNonUserCode]
        [GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
        [STAThread]
        public static void Main()
        {
            App app = new App();
            app.InitializeComponent();
            app.Run();
        }
    }
}