using System.Windows;

namespace WinGuiMcp.WpfSample;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OpenDialog_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SampleDialog { Owner = this };
        if (dialog.ShowDialog() == true)
            StatusText.Text = $"Dialog result: {dialog.ResultText}";
    }

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();
}
