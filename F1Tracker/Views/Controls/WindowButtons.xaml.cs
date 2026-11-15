using System.Windows;
using System.Windows.Controls;

namespace F1Tracker.Views.Controls;

/// <summary>Свои кнопки окна вместо системной белой шапки.</summary>
public partial class WindowButtons : UserControl
{
    /// <summary>false — скрыть «развернуть» (для окна входа).</summary>
    public bool ShowMaximize { get; set; } = true;

    private Window? Host => Window.GetWindow(this);

    public WindowButtons()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            MaxBtn.Visibility = ShowMaximize ? Visibility.Visible : Visibility.Collapsed;
            if (Host is { } w)
            {
                w.StateChanged += (_, _) => UpdateIcon();
                UpdateIcon();
            }
        };
    }

    private void UpdateIcon()
    {
        if (Host is { } w)
            MaxBtn.Content = w.WindowState == WindowState.Maximized ? "\uE923" : "\uE922";
    }

    private void Min_Click(object sender, RoutedEventArgs e)
    {
        if (Host is { } w) w.WindowState = WindowState.Minimized;
    }

    private void Max_Click(object sender, RoutedEventArgs e)
    {
        if (Host is { } w)
            w.WindowState = w.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Host?.Close();
}