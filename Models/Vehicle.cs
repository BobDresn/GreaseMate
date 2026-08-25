using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Windows.Media.Imaging;

namespace GreaseMate.Models;

public class Vehicle
{
    public int Id { get; set; }

    public string Vin { get; set; } = "";

    public string Make { get; set; } = "";

    public string Model { get; set; } = "";

    public int Year { get; set; }

    public int Mileage { get; set; }

    public string? PhotoFileName { get; set; }

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

    [NotMapped]
    public string? PhotoPath => string.IsNullOrWhiteSpace(PhotoFileName)
        ? null
        : Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GreaseMate", "VehiclePhotos", PhotoFileName);

    [NotMapped]
    public bool HasPhoto => PhotoPath is not null && File.Exists(PhotoPath);

    [NotMapped]
    public BitmapImage? PhotoImage
    {
        get
        {
            if (!HasPhoto) return null;
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(PhotoPath!, UriKind.Absolute);
            image.EndInit();
            image.Freeze();
            return image;
        }
    }
}
