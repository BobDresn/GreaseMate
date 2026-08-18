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

    public string DisplayName => $"{Year} {Make} {Model}";
}