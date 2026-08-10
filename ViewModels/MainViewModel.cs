using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreaseMate.Data;
using GreaseMate.Models;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using GreaseMate.Views;

namespace GreaseMate.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string vin = "";

    [ObservableProperty]
    private string make = "";

    [ObservableProperty]
    private string model = "";

    [ObservableProperty]
    private int year;

    [ObservableProperty]
    private int mileage;

    [ObservableProperty]
    private UserControl currentView;

    public ObservableCollection<Vehicle> Vehicles { get; } = new();

    public int VehicleCount => Vehicles.Count;

    [ObservableProperty]
    private int maintenanceCount = 0;

    [ObservableProperty]
    private int upcomingMaintenanceCount = 0;

    public MainViewModel()
    {
        CurrentView = new DashboardView();

        LoadVehicles();
    }

    [RelayCommand]
    private void ShowDashboard()
    {
        CurrentView = new DashboardView();
    }

    [RelayCommand]
    private void ShowVehicles()
    {
        CurrentView = new VehiclesView();
    }

    [RelayCommand]
    private void ShowMaintenance()
    {
        CurrentView = new MaintenanceView();
    }

    [RelayCommand]
    private void ShowReminders()
    {
        CurrentView = new RemindersView();
    }

    [RelayCommand]
    private void ShowReports()
    {
        CurrentView = new ReportsView();
    }

    [RelayCommand]
    private void LoadVehicles()
    {
        using var db = new GreaseMateDbContext();

        Vehicles.Clear();

        foreach (var vehicle in db.Vehicles.ToList())
        {
            Vehicles.Add(vehicle);
        }

        OnPropertyChanged(nameof(VehicleCount));
    }

    [RelayCommand]
    private void AddVehicle()
    {
        using var db = new GreaseMateDbContext();

        var vehicle = new Vehicle
        {
            Vin = Vin,
            Make = Make,
            Model = Model,
            Year = Year,
            Mileage = Mileage
        };

        db.Vehicles.Add(vehicle);
        db.SaveChanges();

        LoadVehicles();

        Vin = "";
        Make = "";
        Model = "";
        Year = 0;
        Mileage = 0;
    }
}