namespace GreaseMate.Models;

public class MaintenanceRecord
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public string ServiceType { get; set; } = "";
    public DateTime ServiceDate { get; set; } = DateTime.Today;
    public int Mileage { get; set; }
    public decimal Cost { get; set; }
    public string Notes { get; set; } = "";
    public DateTime? NextServiceDate { get; set; }
    public int? NextServiceMileage { get; set; }

    public Vehicle? Vehicle { get; set; }

    public string VehicleDisplay => Vehicle is null
        ? "Unknown vehicle"
        : $"{Vehicle.Year} {Vehicle.Make} {Vehicle.Model}";
}
