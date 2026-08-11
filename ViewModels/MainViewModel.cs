using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreaseMate.Data;
using GreaseMate.Models;
using System.Collections.ObjectModel;
using System.Windows;
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
    [NotifyCanExecuteChangedFor(nameof(RemoveVehicleCommand))]
    private Vehicle? selectedVehicle;

    [ObservableProperty]
    private string vehicleFormError = "";

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
        VehicleFormError = ValidateVehicle();
        if (!string.IsNullOrEmpty(VehicleFormError))
        {
            return;
        }

        using var db = new GreaseMateDbContext();

        var vehicle = new Vehicle
        {
            Vin = Vin.Trim().ToUpperInvariant(),
            Make = Make.Trim(),
            Model = Model.Trim(),
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
        VehicleFormError = "";
    }

    private string ValidateVehicle()
    {
        if (string.IsNullOrWhiteSpace(Make) || string.IsNullOrWhiteSpace(Model))
        {
            return "Make and model are required.";
        }

        if (Year < 1886 || Year > DateTime.Now.Year + 1)
        {
            return $"Enter a year between 1886 and {DateTime.Now.Year + 1}.";
        }

        if (Mileage < 0)
        {
            return "Mileage cannot be negative.";
        }

        var normalizedVin = Vin.Trim();
        if (normalizedVin.Length > 0 && normalizedVin.Length != 17)
        {
            return "VIN must contain 17 characters, or it can be left blank.";
        }

        return "";
    }

    private bool CanRemoveVehicle() => SelectedVehicle is not null;

    [RelayCommand(CanExecute = nameof(CanRemoveVehicle))]
    private void RemoveVehicle()
    {
        if (SelectedVehicle is null)
        {
            return;
        }

        var result = MessageBox.Show(
            $"Remove the {SelectedVehicle.Year} {SelectedVehicle.Make} {SelectedVehicle.Model}?",
            "Remove vehicle",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        using var db = new GreaseMateDbContext();
        var vehicle = db.Vehicles.Find(SelectedVehicle.Id);

        if (vehicle is not null)
        {
            db.Vehicles.Remove(vehicle);
            db.SaveChanges();
        }

        SelectedVehicle = null;
        LoadVehicles();
    }
}
