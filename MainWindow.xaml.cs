using System.Windows;
using RoslynCompiler.csharp;

namespace RoslynCompiler
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            string ipAddress = IpAddressTextBox.Text;
            int port = int.Parse(PortTextBox.Text);

            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Executable Files (*.exe)|*.exe",
                DefaultExt = "exe",
                FileName = "ClientSideBuilt"
            };

            bool? dialogResult = saveFileDialog.ShowDialog();

            if (dialogResult == true)
            {
                string outputPath = saveFileDialog.FileName;
                ClientCodeGenerator.GenerateAndSaveClientExecutable(ipAddress, port, outputPath);
                MessageBox.Show("Client generated successfully at " + outputPath);
            }
        }
    }
}