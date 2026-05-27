using System.Windows;
using System.Windows.Controls;

namespace GreaseMate.Views;

public partial class MainView : UserControl
{
    private MainViewModel vm = new();

    public MainView()
    {
        InitializeComponent();
        DataContext = vm;

        vm.LoadVehicles();
    }

    private void AddVehicle_Click(object sender, RoutedEventArgs e)
    {
        vm.AddVehicle();
    }
}