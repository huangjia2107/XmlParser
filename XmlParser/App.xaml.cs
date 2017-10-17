using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

namespace XmlParser
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args != null && e.Args.Length > 0)
                (new MainWindow(e.Args[0])).Show();
            else
                (new MainWindow(null)).Show();
        }
    }
}
