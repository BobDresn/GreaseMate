namespace GreaseMate.Models;

public class MaintenanceReminder
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public string ServiceType { get; set; } = "";
    public DateTime? DueDate { get; set; }
    public int? DueMileage { get; set; }
    public int? RepeatMonths { get; set; }
    public int? RepeatMileage { get; set; }
    public string Notes { get; set; } = "";
    public DateTime? LastNotificationDate { get; set; }

    public Vehicle? Vehicle { get; set; }

    public string VehicleDisplay => Vehicle?.DisplayName ?? "Unknown vehicle";
    public string DueDateDisplay => DueDate?.ToString("MMM d, yyyy") ?? "No date target";
    public string DueMileageDisplay => DueMileage.HasValue
        ? $"{DueMileage.Value:N0} mi"
        : "No mileage target";

    public bool IsOverdue =>
        (DueDate.HasValue && DueDate.Value.Date < DateTime.Today) ||
        (DueMileage.HasValue && Vehicle is not null && DueMileage.Value < Vehicle.Mileage);

    public string StatusDisplay => IsOverdue ? "OVERDUE" : "UPCOMING";
}
