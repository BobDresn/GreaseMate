using GreaseMate.ViewModels;
using System.Windows;

namespace GreaseMate;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        DataContext = new MainViewModel();
    }
}