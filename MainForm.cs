using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Win32;
using System.Diagnostics;
using System.Reflection;

namespace AggrWin;

public sealed class MainForm : Form
{
    private const string StartupKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string StartupValueName = "AggrWin";
    private const string HomeUrl = "https://aggr.trade";

    private readonly WebView2 _browser = new();
    private readonly ToolStrip _toolbar = new();
    private readonly ToolStripButton _homeButton = new("Home");
    private readonly ToolStripButton _reloadButton = new("Reload");
    private readonly ToolStripButton _startupButton = new("Start with Windows");
    private readonly ToolStripButton _fullscreenButton = new("Fullscreen");
    private readonly ToolStripLabel _statusLabel = new("Starting…");
    private readonly NotifyIcon _trayIcon;
    private bool _allowExit;
    private bool _isFullscreen;
    private FormBorderStyle _previousBorderStyle;
    private FormWindowState _previousWindowState;
    private Rectangle _previousBounds;

    public MainForm()
    {
        Text = "AggrWin — ETHBTC";
        MinimumSize = new Size(900, 560);
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized;
        Icon = SystemIcons.Application;
        KeyPreview = true;

        BuildToolbar();
        BuildLayout();

        var trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("Open AggrWin", null, (_, _) => RestoreFromTray());
        trayMenu.Items.Add("Reload", null, (_, _) => _browser.Reload());
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add("Exit", null, (_, _) => ExitApplication());

        _trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "AggrWin — ETHBTC",
            ContextMenuStrip = trayMenu,
            Visible = true
        };
        _trayIcon.DoubleClick += (_, _) => RestoreFromTray();

        _homeButton.Click += (_, _) => NavigateHome();
        _reloadButton.Click += (_, _) => _browser.Reload();
        _startupButton.Click += (_, _) => ToggleStartup();
        _fullscreenButton.Click += (_, _) => ToggleFullscreen();
        _browser.NavigationStarting += (_, _) => SetStatus("Loading…");
        _browser.NavigationCompleted += (_, e) =>
            SetStatus(e.IsSuccess ? "Connected" : $"Navigation failed: {e.WebErrorStatus}");
        _browser.CoreWebView2InitializationCompleted += (_, e) =>
        {
            if (!e.IsSuccess)
                SetStatus("WebView2 initialization failed");
        };

        Shown += async (_, _) => await InitializeBrowserAsync();
        FormClosing += HandleFormClosing;
        Resize += (_, _) =>
        {
            if (WindowState == FormWindowState.Minimized)
                HideToTray();
        };
        KeyDown += HandleKeyboardShortcuts;

        UpdateStartupButton();
    }

    private void BuildToolbar()
    {
        _toolbar.GripStyle = ToolStripGripStyle.Hidden;
        _toolbar.Dock = DockStyle.Top;
        _toolbar.Padding = new Padding(7, 5, 7, 5);
        _toolbar.BackColor = Color.FromArgb(30, 34, 42);
        _toolbar.ForeColor = Color.White;
        _toolbar.Renderer = new ToolStripProfessionalRenderer(new DarkColorTable());

        foreach (var button in new[] { _homeButton, _reloadButton, _startupButton, _fullscreenButton })
        {
            button.DisplayStyle = ToolStripItemDisplayStyle.Text;
            button.ForeColor = Color.White;
            button.Margin = new Padding(3, 1, 3, 1);
            _toolbar.Items.Add(button);
        }

        _statusLabel.Alignment = ToolStripItemAlignment.Right;
        _statusLabel.ForeColor = Color.Gainsboro;
        _toolbar.Items.Add(_statusLabel);
    }

    private void BuildLayout()
    {
        _browser.Dock = DockStyle.Fill;
        _browser.CreationProperties = new CoreWebView2CreationProperties();
        Controls.Add(_browser);
        Controls.Add(_toolbar);
    }

    private async Task InitializeBrowserAsync()
    {
        try
        {
            SetStatus("Starting browser…");
            string userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AggrWin", "WebView2");

            Directory.CreateDirectory(userDataFolder);
            var environment = await CoreWebView2Environment.CreateAsync(
                browserExecutableFolder: null,
                userDataFolder: userDataFolder);

            await _browser.EnsureCoreWebView2Async(environment);

            var settings = _browser.CoreWebView2.Settings;
            settings.AreDefaultContextMenusEnabled = true;
            settings.AreDevToolsEnabled = true;
            settings.IsStatusBarEnabled = false;
            settings.IsZoomControlEnabled = true;
            settings.IsWebMessageEnabled = false;

            _browser.CoreWebView2.NewWindowRequested += (_, e) =>
            {
                // Keep website popups and target=_blank links in the same app window.
                e.Handled = true;
                if (Uri.TryCreate(e.Uri, UriKind.Absolute, out var uri) &&
                    (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp))
                {
                    _browser.CoreWebView2.Navigate(uri.AbsoluteUri);
                }
            };

            _browser.CoreWebView2.ProcessFailed += (_, _) =>
                SetStatus("Browser process stopped — restart AggrWin if it does not recover");

            _browser.Source = new Uri(HomeUrl);
            SetStatus("Connected");
        }
        catch (WebView2RuntimeNotFoundException)
        {
            SetStatus("Microsoft Edge WebView2 Runtime is missing");
            MessageBox.Show(
                this,
                "AggrWin requires the Microsoft Edge WebView2 Runtime. Install the Evergreen Runtime, then reopen AggrWin.\n\nhttps://developer.microsoft.com/microsoft-edge/webview2/",
                "WebView2 Runtime Required",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            SetStatus("Browser startup failed");
            MessageBox.Show(this, ex.Message, "AggrWin startup error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void NavigateHome()
    {
        if (_browser.CoreWebView2 is not null)
            _browser.CoreWebView2.Navigate(HomeUrl);
    }

    private void ToggleStartup()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupKeyPath, writable: true)
                ?? Registry.CurrentUser.CreateSubKey(StartupKeyPath, writable: true);

            if (IsStartupEnabled())
                key.DeleteValue(StartupValueName, throwOnMissingValue: false);
            else
                key.SetValue(StartupValueName, $"\\"{Application.ExecutablePath}\\"");

            UpdateStartupButton();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not update Windows startup settings.\n\n{ex.Message}",
                "AggrWin", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static bool IsStartupEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(StartupKeyPath);
        return key?.GetValue(StartupValueName) is string;
    }

    private void UpdateStartupButton()
    {
        if (_startupButton is null)
            return;

        _startupButton.Checked = IsStartupEnabled();
        _startupButton.Text = _startupButton.Checked
            ? "✓ Start with Windows"
            : "Start with Windows";
        _startupButton.ToolTipText = "Toggle automatic launch when you sign in to Windows";
    }

    private void ToggleFullscreen()
    {
        if (!_isFullscreen)
        {
            _previousBorderStyle = FormBorderStyle;
            _previousWindowState = WindowState;
            _previousBounds = Bounds;
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Normal;
            Bounds = Screen.FromControl(this).Bounds;
            _toolbar.Visible = false;
            _isFullscreen = true;
        }
        else
        {
            FormBorderStyle = _previousBorderStyle;
            WindowState = FormWindowState.Normal;
            Bounds = _previousBounds;
            WindowState = _previousWindowState;
            _toolbar.Visible = true;
            _isFullscreen = false;
        }
    }

    private void HandleKeyboardShortcuts(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F11)
        {
            ToggleFullscreen();
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.F5)
        {
            _browser.Reload();
            e.Handled = true;
        }
        else if (e.Alt && e.KeyCode == Keys.Left && _browser.CoreWebView2?.CanGoBack == true)
        {
            _browser.CoreWebView2.GoBack();
            e.Handled = true;
        }
        else if (e.Control && e.KeyCode == Keys.R)
        {
            _browser.Reload();
            e.Handled = true;
        }
    }

    private void HandleFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!_allowExit && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            HideToTray();
        }
        else
        {
            _trayIcon.Visible = false;
        }
    }

    private void HideToTray()
    {
        Hide();
        ShowInTaskbar = false;
        _trayIcon.BalloonTipTitle = "AggrWin is still running";
        _trayIcon.BalloonTipText = "Double-click the tray icon to reopen the market window.";
        _trayIcon.ShowBalloonTip(1500);
    }

    private void RestoreFromTray()
    {
        Show();
        ShowInTaskbar = true;
        WindowState = FormWindowState.Maximized;
        Activate();
    }

    private void ExitApplication()
    {
        _allowExit = true;
        _trayIcon.Visible = false;
        Close();
    }

    private void SetStatus(string message)
    {
        if (IsDisposed || Disposing)
            return;

        if (InvokeRequired)
        {
            BeginInvoke(() => SetStatus(message));
            return;
        }

        _statusLabel.Text = message;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _trayIcon.Dispose();
            _browser.Dispose();
            _toolbar.Dispose();
        }
        base.Dispose(disposing);
    }

    private sealed class DarkColorTable : ProfessionalColorTable
    {
        public override Color ToolStripGradientBegin => Color.FromArgb(30, 34, 42);
        public override Color ToolStripGradientMiddle => Color.FromArgb(30, 34, 42);
        public override Color ToolStripGradientEnd => Color.FromArgb(30, 34, 42);
        public override Color ButtonSelectedGradientBegin => Color.FromArgb(55, 62, 74);
        public override Color ButtonSelectedGradientEnd => Color.FromArgb(55, 62, 74);
        public override Color ButtonPressedGradientBegin => Color.FromArgb(65, 75, 90);
        public override Color ButtonPressedGradientEnd => Color.FromArgb(65, 75, 90);
        public override Color ToolStripBorder => Color.FromArgb(30, 34, 42);
    }
}
