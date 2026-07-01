using MouseToVJoy.ViewModels;
using MouseToVJoy.Views;
using System.Windows;

namespace MouseToVJoy
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Această linie leagă elementele din XAML ({Binding ...}) de proprietățile din MainViewModel
            var viewModel = new MainViewModel();
            this.DataContext = viewModel;
            viewModel.AttachToWindow(this);
        }
        private void HelpAboutButton_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow aboutWindow = new AboutWindow();
            aboutWindow.Owner = this; // Setează fereastra principală ca părinte
            aboutWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            aboutWindow.ShowDialog();
        }
    }
}
