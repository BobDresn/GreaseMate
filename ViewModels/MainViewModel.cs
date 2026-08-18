using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreaseMate.Data;
using GreaseMate.Models;
using GreaseMate.Views;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace GreaseMate.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private string vin = "";
    [ObservableProperty] private string make = "";
    [ObservableProperty] private string model = "";
    [ObservableProperty] private int year;
    [ObservableProperty] private int mileage;
    [ObservableProperty] private string vehicleFormError = "";
    [ObservableProperty] private bool isEditingVehicle;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveVehicleCommand))]
    [NotifyCanExecuteChangedFor(nameof(EditVehicleCommand))]
    private Vehicle? selectedVehicle;

    [ObservableProperty] private Vehicle? maintenanceVehicle;
    [ObservableProperty] private string serviceType = "";
    [ObservableProperty] private DateTime? serviceDate = DateTime.Today;
    [ObservableProperty] private int serviceMileage;
    [ObservableProperty] private decimal serviceCost;
    [ObservableProperty] private string serviceNotes = "";
    [ObservableProperty] private DateTime? nextServiceDate;
    [ObservableProperty] private int? nextServiceMileage;
    [ObservableProperty] private string maintenanceFormError = "";
    [ObservableProperty] private bool isEditingMaintenance;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveMaintenanceCommand))]
    [NotifyCanExecuteChangedFor(nameof(EditMaintenanceCommand))]
    private MaintenanceRecord? selectedMaintenanceRecord;

    [ObservableProperty] private UserControl currentView;

    public ObservableCollection<Vehicle> Vehicles { get; } = new();
    public ObservableCollection<MaintenanceRecord> MaintenanceRecords { get; } = new();
    public int VehicleCount => Vehicles.Count;

    [ObservableProperty] private int maintenanceCount;
    [ObservableProperty] private int upcomingMaintenanceCount;

    public MainViewModel()
    {
        using (var db = new GreaseMateDbContext()) db.EnsureSchema();
        CurrentView = new DashboardView();
        LoadVehicles();
        LoadMaintenance();
    }

    [RelayCommand] private void ShowDashboard() => CurrentView = new DashboardView();
    [RelayCommand] private void ShowVehicles() => CurrentView = new VehiclesView();
    [RelayCommand] private void ShowMaintenance() => CurrentView = new MaintenanceView();
    [RelayCommand] private void ShowReminders() => CurrentView = new RemindersView();
    [RelayCommand] private void ShowReports() => CurrentView = new ReportsView();

    [RelayCommand]
    private void LoadVehicles()
    {
        using var db = new GreaseMateDbContext();
        Vehicles.Clear();
        foreach (var vehicle in db.Vehicles.OrderByDescending(v => v.Year).ThenBy(v => v.Make))
            Vehicles.Add(vehicle);
        OnPropertyChanged(nameof(VehicleCount));
    }

    [RelayCommand]
    private void AddVehicle()
    {
        VehicleFormError = ValidateVehicle();
        if (!string.IsNullOrEmpty(VehicleFormError)) return;

        using var db = new GreaseMateDbContext();
        db.Vehicles.Add(BuildVehicle());
        db.SaveChanges();
        FinishVehicleSave();
    }

    private bool CanEditVehicle() => SelectedVehicle is not null;

    [RelayCommand(CanExecute = nameof(CanEditVehicle))]
    private void EditVehicle()
    {
        if (SelectedVehicle is null) return;
        Vin = SelectedVehicle.Vin;
        Make = SelectedVehicle.Make;
        Model = SelectedVehicle.Model;
        Year = SelectedVehicle.Year;
        Mileage = SelectedVehicle.Mileage;
        VehicleFormError = "";
        IsEditingVehicle = true;
    }

    [RelayCommand]
    private void UpdateVehicle()
    {
        if (SelectedVehicle is null)
        {
            VehicleFormError = "Select a vehicle before saving changes.";
            return;
        }

        VehicleFormError = ValidateVehicle(SelectedVehicle.Id);
        if (!string.IsNullOrEmpty(VehicleFormError)) return;

        using var db = new GreaseMateDbContext();
        var vehicle = db.Vehicles.Find(SelectedVehicle.Id);
        if (vehicle is null) return;
        vehicle.Vin = Vin.Trim().ToUpperInvariant();
        vehicle.Make = Make.Trim();
        vehicle.Model = Model.Trim();
        vehicle.Year = Year;
        vehicle.Mileage = Mileage;
        db.SaveChanges();
        FinishVehicleSave();
        LoadMaintenance();
    }

    [RelayCommand]
    private void CancelVehicleEdit()
    {
        ClearVehicleForm();
        SelectedVehicle = null;
    }

    private Vehicle BuildVehicle() => new()
    {
        Vin = Vin.Trim().ToUpperInvariant(),
        Make = Make.Trim(),
        Model = Model.Trim(),
        Year = Year,
        Mileage = Mileage
    };

    private string ValidateVehicle(int? currentVehicleId = null)
    {
        if (string.IsNullOrWhiteSpace(Make) || string.IsNullOrWhiteSpace(Model))
            return "Make and model are required.";
        if (Year < 1886 || Year > DateTime.Now.Year + 1)
            return $"Enter a year between 1886 and {DateTime.Now.Year + 1}.";
        if (Mileage < 0) return "Mileage cannot be negative.";

        var normalizedVin = Vin.Trim().ToUpperInvariant();
        if (normalizedVin.Length > 0 && normalizedVin.Length != 17)
            return "VIN must contain 17 characters, or it can be left blank.";
        if (normalizedVin.Length == 17)
        {
            using var db = new GreaseMateDbContext();
            if (db.Vehicles.Any(v => v.Vin == normalizedVin && v.Id != currentVehicleId))
                return "That VIN is already assigned to another vehicle.";
        }
        return "";
    }

    private bool CanRemoveVehicle() => SelectedVehicle is not null;

    [RelayCommand(CanExecute = nameof(CanRemoveVehicle))]
    private void RemoveVehicle()
    {
        if (SelectedVehicle is null) return;
        var result = MessageBox.Show(
            $"Remove the {SelectedVehicle.DisplayName} and all of its maintenance records?",
            "Remove vehicle", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        using var db = new GreaseMateDbContext();
        var vehicle = db.Vehicles.Find(SelectedVehicle.Id);
        if (vehicle is not null)
        {
            db.Vehicles.Remove(vehicle);
            db.SaveChanges();
        }
        SelectedVehicle = null;
        ClearVehicleForm();
        LoadVehicles();
        LoadMaintenance();
    }

    private void FinishVehicleSave()
    {
        SelectedVehicle = null;
        ClearVehicleForm();
        LoadVehicles();
    }

    private void ClearVehicleForm()
    {
        Vin = Make = Model = "";
        Year = Mileage = 0;
        VehicleFormError = "";
        IsEditingVehicle = false;
    }

    [RelayCommand]
    private void LoadMaintenance()
    {
        using var db = new GreaseMateDbContext();
        db.EnsureSchema();
        MaintenanceRecords.Clear();
        foreach (var record in db.MaintenanceRecords.Include(r => r.Vehicle)
                     .OrderByDescending(r => r.ServiceDate).ThenByDescending(r => r.Id))
            MaintenanceRecords.Add(record);

        MaintenanceCount = MaintenanceRecords.Count(r => r.ServiceDate.Year == DateTime.Today.Year);
        UpcomingMaintenanceCount = MaintenanceRecords.Count(r =>
            (r.NextServiceDate.HasValue && r.NextServiceDate.Value.Date >= DateTime.Today &&
             r.NextServiceDate.Value.Date <= DateTime.Today.AddDays(30)) ||
            (r.NextServiceMileage.HasValue && r.Vehicle is not null &&
             r.NextServiceMileage.Value >= r.Vehicle.Mileage &&
             r.NextServiceMileage.Value <= r.Vehicle.Mileage + 1000));
    }

    [RelayCommand]
    private void AddMaintenance()
    {
        MaintenanceFormError = ValidateMaintenance();
        if (!string.IsNullOrEmpty(MaintenanceFormError)) return;
        using var db = new GreaseMateDbContext();
        db.MaintenanceRecords.Add(BuildMaintenanceRecord());
        db.SaveChanges();
        FinishMaintenanceSave();
    }

    private bool CanEditMaintenance() => SelectedMaintenanceRecord is not null;

    [RelayCommand(CanExecute = nameof(CanEditMaintenance))]
    private void EditMaintenance()
    {
        if (SelectedMaintenanceRecord is null) return;
        MaintenanceVehicle = Vehicles.FirstOrDefault(v => v.Id == SelectedMaintenanceRecord.VehicleId);
        ServiceType = SelectedMaintenanceRecord.ServiceType;
        ServiceDate = SelectedMaintenanceRecord.ServiceDate;
        ServiceMileage = SelectedMaintenanceRecord.Mileage;
        ServiceCost = SelectedMaintenanceRecord.Cost;
        ServiceNotes = SelectedMaintenanceRecord.Notes;
        NextServiceDate = SelectedMaintenanceRecord.NextServiceDate;
        NextServiceMileage = SelectedMaintenanceRecord.NextServiceMileage;
        MaintenanceFormError = "";
        IsEditingMaintenance = true;
    }

    [RelayCommand]
    private void UpdateMaintenance()
    {
        if (SelectedMaintenanceRecord is null)
        {
            MaintenanceFormError = "Select a maintenance record before saving changes.";
            return;
        }
        MaintenanceFormError = ValidateMaintenance();
        if (!string.IsNullOrEmpty(MaintenanceFormError)) return;

        using var db = new GreaseMateDbContext();
        var record = db.MaintenanceRecords.Find(SelectedMaintenanceRecord.Id);
        if (record is null) return;
        CopyMaintenanceFields(record);
        db.SaveChanges();
        FinishMaintenanceSave();
    }

    private MaintenanceRecord BuildMaintenanceRecord()
    {
        var record = new MaintenanceRecord();
        CopyMaintenanceFields(record);
        return record;
    }

    private void CopyMaintenanceFields(MaintenanceRecord record)
    {
        record.VehicleId = MaintenanceVehicle!.Id;
        record.ServiceType = ServiceType.Trim();
        record.ServiceDate = ServiceDate!.Value.Date;
        record.Mileage = ServiceMileage;
        record.Cost = ServiceCost;
        record.Notes = ServiceNotes.Trim();
        record.NextServiceDate = NextServiceDate?.Date;
        record.NextServiceMileage = NextServiceMileage;
    }

    private string ValidateMaintenance()
    {
        if (MaintenanceVehicle is null) return "Select a vehicle.";
        if (string.IsNullOrWhiteSpace(ServiceType)) return "Service type is required.";
        if (!ServiceDate.HasValue) return "Service date is required.";
        if (ServiceDate.Value.Date > DateTime.Today) return "Service date cannot be in the future.";
        if (ServiceMileage < 0) return "Mileage cannot be negative.";
        if (ServiceCost < 0) return "Cost cannot be negative.";
        if (NextServiceMileage.HasValue && NextServiceMileage.Value < ServiceMileage)
            return "Next-service mileage cannot be below the service mileage.";
        if (NextServiceDate.HasValue && NextServiceDate.Value.Date < ServiceDate.Value.Date)
            return "Next-service date cannot be before the service date.";
        return "";
    }

    private bool CanRemoveMaintenance() => SelectedMaintenanceRecord is not null;

    [RelayCommand(CanExecute = nameof(CanRemoveMaintenance))]
    private void RemoveMaintenance()
    {
        if (SelectedMaintenanceRecord is null) return;
        var result = MessageBox.Show($"Remove the {SelectedMaintenanceRecord.ServiceType} record?",
            "Remove maintenance record", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        using var db = new GreaseMateDbContext();
        var record = db.MaintenanceRecords.Find(SelectedMaintenanceRecord.Id);
        if (record is not null)
        {
            db.MaintenanceRecords.Remove(record);
            db.SaveChanges();
        }
        FinishMaintenanceSave();
    }

    [RelayCommand]
    private void CancelMaintenanceEdit()
    {
        SelectedMaintenanceRecord = null;
        ClearMaintenanceForm();
    }

    private void FinishMaintenanceSave()
    {
        SelectedMaintenanceRecord = null;
        ClearMaintenanceForm();
        LoadMaintenance();
    }

    private void ClearMaintenanceForm()
    {
        MaintenanceVehicle = null;
        ServiceType = ServiceNotes = "";
        ServiceDate = DateTime.Today;
        ServiceMileage = 0;
        ServiceCost = 0;
        NextServiceDate = null;
        NextServiceMileage = null;
        MaintenanceFormError = "";
        IsEditingMaintenance = false;
    }
}