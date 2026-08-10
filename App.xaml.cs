using System.Windows;

namespace GreaseMate;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        using var db = new Data.GreaseMateDbContext();
        db.Database.EnsureCreated();
    }

}
