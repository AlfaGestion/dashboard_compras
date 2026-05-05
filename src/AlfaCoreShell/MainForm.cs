using System.Diagnostics;

namespace AlfaCoreShell;

internal sealed class MainForm : Form
{
    private readonly LauncherOptions _options;
    private readonly BackendLauncher _backendLauncher;
    private readonly Label _statusLabel;

    public MainForm(LauncherOptions options)
    {
        _options = options;
        _backendLauncher = new BackendLauncher(options);

        Text = options.Title;
        Size = new Size(420, 110);
        MinimumSize = new Size(420, 110);
        MaximumSize = new Size(420, 110);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        _statusLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Iniciando...",
            TextAlign = ContentAlignment.MiddleCenter,
            Padding = new Padding(12)
        };

        Controls.Add(_statusLabel);
        Load += MainForm_Load;
    }

    private async void MainForm_Load(object? sender, EventArgs e)
    {
        try
        {
            _statusLabel.Text = "Preparando dashboard...";
            await _backendLauncher.EnsureStartedAsync(CancellationToken.None);
            _statusLabel.Text = "Abriendo navegador...";
            Process.Start(new ProcessStartInfo(_options.TargetUri.ToString()) { UseShellExecute = true });
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"{ex.Message}{Environment.NewLine}{Environment.NewLine}Destino: {_options.TargetUri}",
                "AlfaCore",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            Close();
        }
    }
}
