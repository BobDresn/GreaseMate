using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreaseMate.Data;
using GreaseMate.Models;
using GreaseMate.Views;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using GreaseMate.Services;
using Microsoft.Win32;
using System.Globalization;
using System.IO;
using System.Text;

namespace GreaseMate.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly VehiclePhotoService vehiclePhotoService = new();
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

    [ObservableProperty] private Vehicle? reminderVehicle;
    [ObservableProperty] private string reminderServiceType = "";
    [ObservableProperty] private DateTime? reminderDueDate;
    [ObservableProperty] private int? reminderDueMileage;
    [ObservableProperty] private int? reminderRepeatMonths;
    [ObservableProperty] private int? reminderRepeatMileage;
    [ObservableProperty] private string reminderNotes = "";
    [ObservableProperty] private string reminderFormError = "";
    [ObservableProperty] private bool isEditingReminder;
    [ObservableProperty] private int defaultReminderDays = 30;
    [ObservableProperty] private int defaultReminderMiles = 1000;
    [ObservableProperty] private string reminderSettingsMessage = "";
    [ObservableProperty] private Vehicle? reportVehicle;
    [ObservableProperty] private DateTime? reportStartDate = new(DateTime.Today.Year, 1, 1);
    [ObservableProperty] private DateTime? reportEndDate = DateTime.Today;
    [ObservableProperty] private decimal reportTotalCost;
    [ObservableProperty] private decimal reportAverageCost;
    [ObservableProperty] private int reportServiceCount;
    [ObservableProperty] private string reportStatusMessage = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditReminderCommand))]
    [NotifyCanExecuteChangedFor(nameof(RemoveReminderCommand))]
    [NotifyCanExecuteChangedFor(nameof(CompleteReminderCommand))]
    private MaintenanceReminder? selectedReminder;

    [ObservableProperty] private UserControl currentView;

    public ObservableCollection<Vehicle> Vehicles { get; } = new();
    public ObservableCollection<MaintenanceRecord> MaintenanceRecords { get; } = new();
    public ObservableCollection<MaintenanceReminder> MaintenanceReminders { get; } = new();
    public ObservableCollection<MaintenanceRecord> ReportRecords { get; } = new();
    public int VehicleCount => Vehicles.Count;

    [ObservableProperty] private int maintenanceCount;
    [ObservableProperty] private int upcomingMaintenanceCount;

    public MainViewModel()
    {
        using (var db = new GreaseMateDbContext()) db.EnsureSchema();
        CurrentView = new DashboardView();
        LoadVehicles();
        LoadMaintenance();
        LoadReminders();
        LoadReminderSettings();
    }

    [RelayCommand] private void ShowDashboard() => CurrentView = new DashboardView();
    [RelayCommand] private void ShowVehicles() => CurrentView = new VehiclesView();
    [RelayCommand]
    private void ShowMaintenance()
    {
        LoadMaintenance();
        LoadReminders();
        CurrentView = new MaintenanceView();
    }

    [RelayCommand]
    private void ShowReminders()
    {
        LoadReminders();
        CurrentView = new RemindersView();
    }
    [RelayCommand]
    private void ShowReports()
    {
        LoadReport();
        CurrentView = new ReportsView();
    }

    partial void OnReportVehicleChanged(Vehicle? value) => LoadReport();
    partial void OnReportStartDateChanged(DateTime? value) => LoadReport();
    partial void OnReportEndDateChanged(DateTime? value) => LoadReport();

    [RelayCommand] private void ClearReportVehicle() => ReportVehicle = null;

    [RelayCommand]
    private void LoadReport()
    {
        if (ReportStartDate.HasValue && ReportEndDate.HasValue &&
            ReportStartDate.Value.Date > ReportEndDate.Value.Date)
        {
            ReportStatusMessage = "The start date must be before the end date.";
            ReportRecords.Clear();
            UpdateReportTotals();
            return;
        }

        using var db = new GreaseMateDbContext();
        var query = db.MaintenanceRecords.Include(r => r.Vehicle).AsQueryable();
        if (ReportVehicle is not null) query = query.Where(r => r.VehicleId == ReportVehicle.Id);
        if (ReportStartDate.HasValue) query = query.Where(r => r.ServiceDate >= ReportStartDate.Value.Date);
        if (ReportEndDate.HasValue)
        {
            var exclusiveEnd = ReportEndDate.Value.Date.AddDays(1);
            query = query.Where(r => r.ServiceDate < exclusiveEnd);
        }

        ReportRecords.Clear();
        foreach (var record in query.OrderByDescending(r => r.ServiceDate).ThenByDescending(r => r.Id))
            ReportRecords.Add(record);
        ReportStatusMessage = ReportRecords.Count == 0 ? "No records match these filters." : "";
        UpdateReportTotals();
    }

    private void UpdateReportTotals()
    {
        ReportServiceCount = ReportRecords.Count;
        ReportTotalCost = ReportRecords.Sum(r => r.Cost);
        ReportAverageCost = ReportServiceCount == 0 ? 0 : ReportTotalCost / ReportServiceCount;
    }

    [RelayCommand]
    private void ExportReportCsv()
    {
        if (ReportRecords.Count == 0)
        {
            ReportStatusMessage = "There are no report rows to export.";
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Export maintenance report",
            Filter = "CSV file (*.csv)|*.csv",
            FileName = $"GreaseMate-Report-{DateTime.Today:yyyy-MM-dd}.csv",
            DefaultExt = ".csv"
        };
        if (dialog.ShowDialog() != true) return;

        static string Csv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
        var lines = new List<string> { "Vehicle,Service Date,Service Type,Mileage,Cost,Notes" };
        lines.AddRange(ReportRecords.Select(r => string.Join(",",
            Csv(r.VehicleDisplay), r.ServiceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Csv(r.ServiceType), r.Mileage.ToString(CultureInfo.InvariantCulture),
            r.Cost.ToString("0.00", CultureInfo.InvariantCulture), Csv(r.Notes))));
        File.WriteAllLines(dialog.FileName, lines, new UTF8Encoding(true));
        ReportStatusMessage = $"Exported {ReportRecords.Count} records.";
    }

    [RelayCommand]
    private void SendTestNotification()
    {
        try
        {
            new DesktopNotificationService().SendDueNotifications(includeTest: true);
            ReminderSettingsMessage = "Test notification sent.";
        }
        catch (Exception ex)
        {
            ReminderSettingsMessage = $"Notification failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private void LoadVehicles()
    {
        using var db = new GreaseMateDbContext();
        Vehicles.Clear();
        foreach (var vehicle in db.Vehicles
                     .Include(v => v.MaintenanceReminders)
                     .OrderByDescending(v => v.Year)
                     .ThenBy(v => v.Make))
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

        if (vehicle.Mileage != Mileage)
        {
            var directionWarning = Mileage < vehicle.Mileage
                ? "\n\nThe new mileage is lower than the currently saved mileage."
                : "";
            var result = MessageBox.Show(
                $"Update {vehicle.DisplayName} from {vehicle.Mileage:N0} miles to {Mileage:N0} miles?{directionWarning}",
                "Confirm mileage update", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;
        }

        vehicle.Vin = Vin.Trim().ToUpperInvariant();
        vehicle.Make = Make.Trim();
        vehicle.Model = Model.Trim();
        vehicle.Year = Year;
        vehicle.Mileage = Mileage;
        db.SaveChanges();
        FinishVehicleSave();
        LoadMaintenance();
        LoadReminders();
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

    [RelayCommand]
    private void UploadVehiclePhoto(Vehicle vehicle)
    {
        var dialog = new OpenFileDialog
        {
            Title = $"Choose a photo for {vehicle.DisplayName}",
            Filter = "Image files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp",
            Multiselect = false,
            CheckFileExists = true
        };
        if (dialog.ShowDialog() != true) return;

        string? importedFileName = null;
        try
        {
            importedFileName = vehiclePhotoService.Import(dialog.FileName);
            using var db = new GreaseMateDbContext();
            var savedVehicle = db.Vehicles.Find(vehicle.Id);
            if (savedVehicle is null)
            {
                vehiclePhotoService.Delete(importedFileName);
                return;
            }

            var previousFileName = savedVehicle.PhotoFileName;
            savedVehicle.PhotoFileName = importedFileName;
            db.SaveChanges();
            vehiclePhotoService.Delete(previousFileName);
            LoadVehicles();
        }
        catch (Exception ex)
        {
            vehiclePhotoService.Delete(importedFileName);
            MessageBox.Show($"The photo could not be saved.\n\n{ex.Message}",
                "Vehicle photo", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void RemoveVehiclePhoto(Vehicle vehicle)
    {
        if (string.IsNullOrWhiteSpace(vehicle.PhotoFileName)) return;
        var result = MessageBox.Show($"Remove the photo for {vehicle.DisplayName}?",
            "Remove vehicle photo", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;

        using var db = new GreaseMateDbContext();
        var savedVehicle = db.Vehicles.Find(vehicle.Id);
        if (savedVehicle is null) return;
        var fileName = savedVehicle.PhotoFileName;
        savedVehicle.PhotoFileName = null;
        db.SaveChanges();
        vehiclePhotoService.Delete(fileName);
        LoadVehicles();
    }

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
            var photoFileName = vehicle.PhotoFileName;
            db.Vehicles.Remove(vehicle);
            db.SaveChanges();
            vehiclePhotoService.Delete(photoFileName);
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
        UpcomingMaintenanceCount = MaintenanceReminders.Count;
        LoadVehicles();
    }

    [RelayCommand]
    private void AddMaintenance()
    {
        MaintenanceFormError = ValidateMaintenance();
        if (!string.IsNullOrEmpty(MaintenanceFormError)) return;
        using var db = new GreaseMateDbContext();

        // A future service date represents scheduled work, not completed history.
        if (ServiceDate!.Value.Date > DateTime.Today)
        {
            var currentVehicle = db.Vehicles.Find(MaintenanceVehicle!.Id);
            int? scheduledMileage = NextServiceMileage;
            if (!scheduledMileage.HasValue &&
                currentVehicle is not null &&
                ServiceMileage > currentVehicle.Mileage)
            {
                scheduledMileage = ServiceMileage;
            }

            db.MaintenanceReminders.Add(new MaintenanceReminder
            {
                VehicleId = MaintenanceVehicle.Id,
                ServiceType = ServiceType.Trim(),
                DueDate = ServiceDate.Value.Date,
                DueMileage = scheduledMileage,
                Notes = ServiceNotes.Trim()
            });
            db.SaveChanges();
            FinishMaintenanceSave();
            LoadReminders();
            return;
        }

        var record = BuildMaintenanceRecord();
        db.MaintenanceRecords.Add(record);

        if (NextServiceDate.HasValue || NextServiceMileage.HasValue)
        {
            db.MaintenanceReminders.Add(new MaintenanceReminder
            {
                VehicleId = MaintenanceVehicle!.Id,
                ServiceType = ServiceType.Trim(),
                DueDate = NextServiceDate?.Date,
                DueMileage = NextServiceMileage,
                Notes = "Created from completed maintenance."
            });
        }

        OfferVehicleMileageUpdate(db, MaintenanceVehicle!.Id, ServiceMileage);
        db.SaveChanges();
        FinishMaintenanceSave();
        LoadVehicles();
        LoadReminders();
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
        OfferVehicleMileageUpdate(db, MaintenanceVehicle!.Id, ServiceMileage);
        db.SaveChanges();
        FinishMaintenanceSave();
        LoadVehicles();
        LoadReminders();
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
        if (IsEditingMaintenance && ServiceDate.Value.Date > DateTime.Today)
            return "A completed record cannot be moved into the future. Create a new upcoming item instead.";
        if (ServiceMileage < 0) return "Mileage cannot be negative.";
        if (ServiceCost < 0) return "Cost cannot be negative.";
        if (NextServiceMileage.HasValue && NextServiceMileage.Value < ServiceMileage)
            return "Next-service mileage cannot be below the service mileage.";
        if (NextServiceDate.HasValue && NextServiceDate.Value.Date < ServiceDate.Value.Date)
            return "Next-service date cannot be before the service date.";
        return "";
    }

    private static void OfferVehicleMileageUpdate(
        GreaseMateDbContext db,
        int vehicleId,
        int maintenanceMileage)
    {
        var vehicle = db.Vehicles.Find(vehicleId);
        if (vehicle is null || maintenanceMileage <= vehicle.Mileage) return;

        var result = MessageBox.Show(
            $"This service was recorded at {maintenanceMileage:N0} miles, but {vehicle.DisplayName} is saved at " +
            $"{vehicle.Mileage:N0} miles.\n\nUpdate the vehicle mileage to {maintenanceMileage:N0}?",
            "Update vehicle mileage", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes) vehicle.Mileage = maintenanceMileage;
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

    [RelayCommand]
    private void LoadReminders()
    {
        using var db = new GreaseMateDbContext();
        db.EnsureSchema();
        MaintenanceReminders.Clear();
        foreach (var reminder in db.MaintenanceReminders.Include(r => r.Vehicle)
                     .OrderBy(r => r.DueDate == null)
                     .ThenBy(r => r.DueDate)
                     .ThenBy(r => r.DueMileage))
            MaintenanceReminders.Add(reminder);

        UpcomingMaintenanceCount = MaintenanceReminders.Count;
        LoadVehicles();
    }

    [RelayCommand]
    private void AddReminder()
    {
        ReminderFormError = ValidateReminder();
        if (!string.IsNullOrEmpty(ReminderFormError)) return;

        using var db = new GreaseMateDbContext();
        db.MaintenanceReminders.Add(BuildReminder());
        db.SaveChanges();
        FinishReminderSave();
    }

    private bool CanEditReminder() => SelectedReminder is not null;

    [RelayCommand(CanExecute = nameof(CanEditReminder))]
    private void EditReminder()
    {
        if (SelectedReminder is null) return;
        ReminderVehicle = Vehicles.FirstOrDefault(v => v.Id == SelectedReminder.VehicleId);
        ReminderServiceType = SelectedReminder.ServiceType;
        ReminderDueDate = SelectedReminder.DueDate;
        ReminderDueMileage = SelectedReminder.DueMileage;
        ReminderRepeatMonths = SelectedReminder.RepeatMonths;
        ReminderRepeatMileage = SelectedReminder.RepeatMileage;
        ReminderNotes = SelectedReminder.Notes;
        ReminderFormError = "";
        IsEditingReminder = true;
    }

    [RelayCommand]
    private void UpdateReminder()
    {
        if (SelectedReminder is null)
        {
            ReminderFormError = "Select a reminder before saving changes.";
            return;
        }

        ReminderFormError = ValidateReminder();
        if (!string.IsNullOrEmpty(ReminderFormError)) return;

        using var db = new GreaseMateDbContext();
        var reminder = db.MaintenanceReminders.Find(SelectedReminder.Id);
        if (reminder is null) return;
        CopyReminderFields(reminder);
        db.SaveChanges();
        FinishReminderSave();
    }

    private MaintenanceReminder BuildReminder()
    {
        var reminder = new MaintenanceReminder();
        CopyReminderFields(reminder);
        return reminder;
    }

    private void CopyReminderFields(MaintenanceReminder reminder)
    {
        reminder.VehicleId = ReminderVehicle!.Id;
        reminder.ServiceType = ReminderServiceType.Trim();
        reminder.DueDate = ReminderDueDate?.Date;
        reminder.DueMileage = ReminderDueMileage;
        reminder.RepeatMonths = ReminderRepeatMonths;
        reminder.RepeatMileage = ReminderRepeatMileage;
        reminder.Notes = ReminderNotes.Trim();
    }

    private string ValidateReminder()
    {
        if (ReminderVehicle is null) return "Select a vehicle.";
        if (string.IsNullOrWhiteSpace(ReminderServiceType)) return "Service type is required.";
        if (!ReminderDueDate.HasValue && !ReminderDueMileage.HasValue)
            return "Enter a due date, due mileage, or both.";
        if (ReminderDueMileage.HasValue && ReminderDueMileage.Value < 0)
            return "Due mileage cannot be negative.";
        if (ReminderRepeatMonths.HasValue && ReminderRepeatMonths.Value <= 0)
            return "Repeat months must be greater than zero.";
        if (ReminderRepeatMileage.HasValue && ReminderRepeatMileage.Value <= 0)
            return "Repeat mileage must be greater than zero.";
        return "";
    }

    private bool CanRemoveReminder() => SelectedReminder is not null;

    [RelayCommand(CanExecute = nameof(CanRemoveReminder))]
    private void RemoveReminder()
    {
        if (SelectedReminder is null) return;
        var result = MessageBox.Show($"Remove the {SelectedReminder.ServiceType} reminder?",
            "Remove reminder", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        using var db = new GreaseMateDbContext();
        var reminder = db.MaintenanceReminders.Find(SelectedReminder.Id);
        if (reminder is not null)
        {
            db.MaintenanceReminders.Remove(reminder);
            db.SaveChanges();
        }
        FinishReminderSave();
    }

    private bool CanCompleteReminder() => SelectedReminder is not null;

    [RelayCommand(CanExecute = nameof(CanCompleteReminder))]
    private void CompleteReminder()
    {
        if (SelectedReminder is null) return;
        var result = MessageBox.Show(
            $"Mark {SelectedReminder.ServiceType} complete today? You can edit cost and notes from Maintenance afterward.",
            "Complete maintenance", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;

        using var db = new GreaseMateDbContext();
        var reminder = db.MaintenanceReminders.Include(r => r.Vehicle)
            .FirstOrDefault(r => r.Id == SelectedReminder.Id);
        if (reminder is null) return;

        db.MaintenanceRecords.Add(new MaintenanceRecord
        {
            VehicleId = reminder.VehicleId,
            ServiceType = reminder.ServiceType,
            ServiceDate = DateTime.Today,
            Mileage = reminder.Vehicle?.Mileage ?? 0,
            Cost = 0,
            Notes = reminder.Notes
        });

        var repeats = reminder.RepeatMonths.HasValue || reminder.RepeatMileage.HasValue;
        if (repeats)
        {
            if (reminder.RepeatMonths.HasValue)
                reminder.DueDate = (reminder.DueDate ?? DateTime.Today).AddMonths(reminder.RepeatMonths.Value);
            if (reminder.RepeatMileage.HasValue)
                reminder.DueMileage = (reminder.DueMileage ?? reminder.Vehicle?.Mileage ?? 0) +
                                      reminder.RepeatMileage.Value;
        }
        else
        {
            db.MaintenanceReminders.Remove(reminder);
        }

        db.SaveChanges();
        FinishReminderSave();
        LoadMaintenance();
    }

    [RelayCommand]
    private void CancelReminderEdit()
    {
        SelectedReminder = null;
        ClearReminderForm();
    }

    private void FinishReminderSave()
    {
        SelectedReminder = null;
        ClearReminderForm();
        LoadReminders();
    }

    private void ClearReminderForm()
    {
        ReminderVehicle = null;
        ReminderServiceType = ReminderNotes = "";
        ReminderDueDate = null;
        ReminderDueMileage = null;
        ReminderRepeatMonths = null;
        ReminderRepeatMileage = null;
        ReminderFormError = "";
        IsEditingReminder = false;
    }

    private void LoadReminderSettings()
    {
        using var db = new GreaseMateDbContext();
        var settings = db.ReminderSettings.Find(1) ?? new ReminderSettings();
        DefaultReminderDays = settings.LeadDays;
        DefaultReminderMiles = settings.LeadMileage;
    }

    [RelayCommand]
    private void SetReminderLeadTime(string days)
    {
        if (int.TryParse(days, out var parsed)) DefaultReminderDays = parsed;
        ReminderSettingsMessage = "";
    }

    [RelayCommand]
    private void SaveReminderSettings()
    {
        if (DefaultReminderDays < 0 || DefaultReminderMiles < 0)
        {
            ReminderSettingsMessage = "Lead time and mileage cannot be negative.";
            return;
        }

        using var db = new GreaseMateDbContext();
        var settings = db.ReminderSettings.Find(1) ?? new ReminderSettings();
        settings.LeadDays = DefaultReminderDays;
        settings.LeadMileage = DefaultReminderMiles;
        if (db.Entry(settings).State == EntityState.Detached) db.ReminderSettings.Add(settings);
        db.SaveChanges();
        ReminderSettingsMessage = "Settings saved.";
    }
}
