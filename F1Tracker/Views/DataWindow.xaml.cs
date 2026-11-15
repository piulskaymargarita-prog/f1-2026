using System.Windows;

namespace F1Tracker.Views;

public partial class DataWindow : Window
{
    public string InitialTab { get; set; } = "teams";
    public string InitialFilter { get; set; } = "";

    public DataWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => StubText.Text = $"Вкладка: {InitialTab} · Фильтр: «{InitialFilter}»";
    }
}