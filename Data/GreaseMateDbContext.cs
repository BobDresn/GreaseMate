namespace GreaseMate.Data;

using GreaseMate.Models;
using Microsoft.EntityFrameworkCore;

public class GreaseMateDbContext : DbContext
{
    public DbSet<Vehicle> Vehicles { get; set; }
    public DbSet<MaintenanceRecord> MaintenanceRecords { get; set; }

    protected override void OnConfiguring(
        DbContextOptionsBuilder options)
    {
        options.UseSqlite("Data Source=greasemate.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MaintenanceRecord>()
            .HasOne(record => record.Vehicle)
            .WithMany(vehicle => vehicle.MaintenanceRecords)
            .HasForeignKey(record => record.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    public void EnsureSchema()
    {
        Database.EnsureCreated();

        // EnsureCreated does not add new tables to an existing database.
        Database.ExecuteSqlRaw(
            """
            CREATE TABLE IF NOT EXISTS "MaintenanceRecords" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_MaintenanceRecords" PRIMARY KEY AUTOINCREMENT,
                "VehicleId" INTEGER NOT NULL,
                "ServiceType" TEXT NOT NULL,
                "ServiceDate" TEXT NOT NULL,
                "Mileage" INTEGER NOT NULL,
                "Cost" TEXT NOT NULL,
                "Notes" TEXT NOT NULL,
                "NextServiceDate" TEXT NULL,
                "NextServiceMileage" INTEGER NULL,
                CONSTRAINT "FK_MaintenanceRecords_Vehicles_VehicleId"
                    FOREIGN KEY ("VehicleId") REFERENCES "Vehicles" ("Id") ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS "IX_MaintenanceRecords_VehicleId"
                ON "MaintenanceRecords" ("VehicleId");
            """);
    }
}