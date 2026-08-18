using System.ComponentModel.DataAnnotations.Schema;

namespace GreaseMate.Models;

public class Vehicle
{
    public int Id { get; set; }

    public string Vin { get; set; } = "";

    public string Make { get; set; } = "";

    public string Model { get; set; } = "";

    public int Year { get; set; }

    public int Mileage { get; set; }

    public ICollection<MaintenanceRecord> MaintenanceRecords { get; set; } =
        new List<MaintenanceRecord>();

    public ICollection<MaintenanceReminder> MaintenanceReminders { get; set; } =
        new List<MaintenanceReminder>();

    [NotMapped]
    public IEnumerable<MaintenanceReminder> UpcomingMaintenance =>
        MaintenanceReminders
            .OrderBy(reminder => reminder.DueDate == null)
            .ThenBy(reminder => reminder.DueDate)
            .ThenBy(reminder => reminder.DueMileage)
            .Take(3);

    [NotMapped]
    public int UpcomingMaintenanceCount => MaintenanceReminders.Count;

    [NotMapped]
    public bool HasMoreThanThreeUpcoming => UpcomingMaintenanceCount > 3;

    [NotMapped]
    public string DisplayName => $"{Year} {Make} {Model}";
}