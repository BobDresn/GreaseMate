using GreaseMate.Models;
using System.Collections.ObjectModel;

public class MainViewModel
{
    public ObservableCollection<Vehicle> Vehicles { get; set; } = new();

    public string Vin { get; set; }
    public string Make { get; set; }
    public string Model { get; set; }

    public void LoadVehicles()
    {
        using var db = new GreaseMateDbContext();

        Vehicles.Clear();

        foreach (var v in db.Vehicles.ToList())
            Vehicles.Add(v);
    }

    public void AddVehicle()
    {
        using var db = new GreaseMateDbContext();

        var vehicle = new Vehicle
        {
            Vin = Vin,
            Make = Make,
            Model = Model
        };

        db.Vehicles.Add(vehicle);
        db.SaveChanges();

        LoadVehicles();
    }
}