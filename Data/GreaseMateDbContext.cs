using GreaseMate.Models;
using Microsoft.EntityFrameworkCore;

public class GreaseMateDbContext : DbContext
{
    public DbSet<Vehicle> Vehicles { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlite("Data Source=greasemate.db");
    }
}