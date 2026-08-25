using GreaseMate.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Toolkit.Uwp.Notifications;

namespace GreaseMate.Services;

public sealed class DesktopNotificationService
{
    public int SendDueNotifications(bool includeTest = false)
    {
        if (includeTest)
        {
            ShowToast("GreaseMate notifications are ready", "Scheduled maintenance alerts will appear here.");
            return 1;
        }

        using var db = new GreaseMateDbContext();
        db.EnsureSchema();
        var settings = db.ReminderSettings.Find(1) ?? new Models.ReminderSettings();
        var today = DateTime.Today;
        var reminders = db.MaintenanceReminders.Include(r => r.Vehicle).ToList();
        var sent = 0;

        foreach (var reminder in reminders)
        {
            var dueByDate = reminder.DueDate.HasValue &&
                            reminder.DueDate.Value.Date <= today.AddDays(settings.LeadDays);
            var dueByMileage = reminder.DueMileage.HasValue && reminder.Vehicle is not null &&
                               reminder.DueMileage.Value <= reminder.Vehicle.Mileage + settings.LeadMileage;
            if ((!dueByDate && !dueByMileage) || reminder.LastNotificationDate?.Date == today) continue;

            var target = reminder.IsOverdue
                ? "This maintenance item is overdue."
                : $"Due {reminder.DueDateDisplay} or at {reminder.DueMileageDisplay}.";
            ShowToast($"{reminder.ServiceType} — {reminder.VehicleDisplay}", target);
            reminder.LastNotificationDate = DateTime.Now;
            sent++;
        }

        if (sent > 0) db.SaveChanges();
        return sent;
    }

    private static void ShowToast(string title, string message)
    {
        new ToastContentBuilder()
            .AddArgument("action", "openReminders")
            .AddText(title)
            .AddText(message)
            .Show();
    }
}
