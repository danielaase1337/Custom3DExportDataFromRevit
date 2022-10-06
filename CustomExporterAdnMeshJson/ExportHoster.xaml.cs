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
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CustomExporterAdnMeshJson
{
    /// <summary>
    /// Interaction logic for ExportHoster.xaml
    /// </summary>
    public partial class ExportHoster : Window
    {
        public ExportHoster(string path)
        {
            InitializeComponent();
            if (File.Exists(path))
            {
                ConnectionView.SetGmlFile(path);
            }
        }
    }
}
