using GreaseMate.ViewModels;
using System.Windows.Controls;

namespace GreaseMate.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();

        DataContext = new MainViewModel();
    }
}