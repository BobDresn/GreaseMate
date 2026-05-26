using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GreaseMate.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string vinInput = "";

    [ObservableProperty]
    private string make = "";

    [ObservableProperty]
    private string model = "";

    [ObservableProperty]
    private int year;

    [RelayCommand]
    private void DecodeVin()
    {
        // Temporary fake data
        Make = "Ford";
        Model = "Mustang";
        Year = 2020;
    }
}