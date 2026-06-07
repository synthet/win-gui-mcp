using System.Windows;

namespace WinGuiMcp.WpfSample;

public partial class SampleDialog : Window
{
    public string ResultText { get; private set; } = string.Empty;

    public SampleDialog()
    {
        InitializeComponent();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        ResultText = InputBox.Text;
        DialogResult = true;
    }
}
