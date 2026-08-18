using System;
using System.Windows;
using System.Windows.Controls;

namespace GreaseMate.Views;

public partial class VehiclesView : UserControl
{
    public static readonly DependencyProperty VehicleCardWidthProperty =
        DependencyProperty.Register(
            nameof(VehicleCardWidth),
            typeof(double),
            typeof(VehiclesView),
            new PropertyMetadata(260d));

    public double VehicleCardWidth
    {
        get => (double)GetValue(VehicleCardWidthProperty);
        set => SetValue(VehicleCardWidthProperty, value);
    }

    public VehiclesView()
    {
        InitializeComponent();
    }

    private void VehiclesList_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // One column for tablet-sized content, two for a large laptop,
        // and no more than three columns for wide desktop or TV displays.
        var availableWidth = Math.Max(260, e.NewSize.Width - 20);

        var columns = availableWidth switch
        {
            < 760 => 1,
            < 1120 => 2,
            _ => 3
        };

        const double cardSpacing = 14;
        VehicleCardWidth = Math.Max(
            240,
            Math.Floor(availableWidth / columns) - cardSpacing);
    }
}