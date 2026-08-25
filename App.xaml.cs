using System.Windows;

using GreaseMate.Services;
using System.Windows.Threading;

namespace GreaseMate;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private readonly DesktopNotificationService notificationService = new();
    private DispatcherTimer? notificationTimer;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        using var db = new Data.GreaseMateDbContext();
        db.EnsureSchema();

        TrySendNotifications();
        notificationTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(15) };
        notificationTimer.Tick += (_, _) => TrySendNotifications();
        notificationTimer.Start();
    }

    private void TrySendNotifications()
    {
        try
        {
            notificationService.SendDueNotifications();
        }
        catch
        {
            // Notifications should never prevent the maintenance app from opening.
        }
    }

}
