namespace GreaseMate.Data;

using GreaseMate.Models;
using Microsoft.EntityFrameworkCore;

public class GreaseMateDbContext : DbContext
{
    public DbSet<Vehicle> Vehicles { get; set; }
    public DbSet<MaintenanceRecord> MaintenanceRecords { get; set; }
    public DbSet<MaintenanceReminder> MaintenanceReminders { get; set; }
    public DbSet<ReminderSettings> ReminderSettings { get; set; }

    protected override void OnConfiguring(
        DbContextOptionsBuilder options)
    {
        options.UseSqlite("Data Source=greasemate.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Vehicle>().Ignore(vehicle => vehicle.UpcomingMaintenance);
        modelBuilder.Entity<Vehicle>().Ignore(vehicle => vehicle.UpcomingMaintenanceCount);
        modelBuilder.Entity<Vehicle>().Ignore(vehicle => vehicle.HasMoreThanThreeUpcoming);
        modelBuilder.Entity<Vehicle>().Ignore(vehicle => vehicle.DisplayName);
        modelBuilder.Entity<Vehicle>().Ignore(vehicle => vehicle.PhotoPath);
        modelBuilder.Entity<Vehicle>().Ignore(vehicle => vehicle.HasPhoto);
        modelBuilder.Entity<Vehicle>().Ignore(vehicle => vehicle.PhotoImage);

        modelBuilder.Entity<MaintenanceRecord>()
            .HasOne(record => record.Vehicle)
            .WithMany(vehicle => vehicle.MaintenanceRecords)
            .HasForeignKey(record => record.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MaintenanceReminder>()
            .HasOne(reminder => reminder.Vehicle)
            .WithMany(vehicle => vehicle.MaintenanceReminders)
            .HasForeignKey(reminder => reminder.VehicleId)
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

            CREATE TABLE IF NOT EXISTS "MaintenanceReminders" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_MaintenanceReminders" PRIMARY KEY AUTOINCREMENT,
                "VehicleId" INTEGER NOT NULL,
                "ServiceType" TEXT NOT NULL,
                "DueDate" TEXT NULL,
                "DueMileage" INTEGER NULL,
                "RepeatMonths" INTEGER NULL,
                "RepeatMileage" INTEGER NULL,
                "Notes" TEXT NOT NULL,
                "LastNotificationDate" TEXT NULL,
                CONSTRAINT "FK_MaintenanceReminders_Vehicles_VehicleId"
                    FOREIGN KEY ("VehicleId") REFERENCES "Vehicles" ("Id") ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS "IX_MaintenanceReminders_VehicleId"
                ON "MaintenanceReminders" ("VehicleId");

            CREATE TABLE IF NOT EXISTS "ReminderSettings" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_ReminderSettings" PRIMARY KEY,
                "LeadDays" INTEGER NOT NULL,
                "LeadMileage" INTEGER NOT NULL
            );
            INSERT OR IGNORE INTO "ReminderSettings" ("Id", "LeadDays", "LeadMileage")
                VALUES (1, 30, 1000);
            """);

        var columns = Database.SqlQueryRaw<string>(
            "SELECT name AS Value FROM pragma_table_info('MaintenanceReminders')").ToList();
        if (!columns.Contains("LastNotificationDate"))
            Database.ExecuteSqlRaw(
                "ALTER TABLE MaintenanceReminders ADD COLUMN LastNotificationDate TEXT NULL");

        var vehicleColumns = Database.SqlQueryRaw<string>(
            "SELECT name AS Value FROM pragma_table_info('Vehicles')").ToList();
        if (!vehicleColumns.Contains("PhotoFileName"))
            Database.ExecuteSqlRaw(
                "ALTER TABLE Vehicles ADD COLUMN PhotoFileName TEXT NULL");
    }
}
