using MidiControllerFrontend.ViewModels;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;

namespace MidiControllerFrontend.Services;

/// <summary>
/// Verwaltet das System-Tray-Icon. Bindet sich an <see cref="MainViewModel.GateValue"/>
/// und ändert die Farbe/das Blinken entsprechend dem Gate-Status.
/// </summary>
public sealed class TrayService : IDisposable
{
    private readonly NotifyIcon     _notifyIcon;
    private readonly MainViewModel  _vm;
    private readonly DispatcherTimer _activityTimer;   // löscht den Activity-Blink nach kurzer Zeit

    // Gate-Farben: 0=Aktiv(grün), 1=Pausiert(gelb), 2=Gesperrt(orange), -1=Getrennt(rot)
    private static readonly Color ColorActive       = Color.LimeGreen;
    private static readonly Color ColorActivityBlink = Color.Cyan;
    private static readonly Color ColorPaused       = Color.Gold;
    private static readonly Color ColorBlocked      = Color.OrangeRed;
    private static readonly Color ColorDisconnected = Color.Red;

    public TrayService(MainViewModel vm, MainWindow mainWindow)
    {
        _vm = vm;

        _notifyIcon = new NotifyIcon
        {
            Text    = "MidiController",
            Visible = true,
            Icon    = CreateIcon(ColorDisconnected)
        };

        _notifyIcon.DoubleClick += (_, _) => ShowWindow(mainWindow);
        _notifyIcon.ContextMenuStrip = BuildContextMenu(vm, mainWindow);

        // Timer zum Zurücksetzen des Activity-Blinks (einmalig feuern)
        _activityTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _activityTimer.Tick += (_, _) =>
        {
            _activityTimer.Stop();
            vm.MidiActivity = false;   // setzt Property zurück → UpdateIcon()
        };

        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(MainViewModel.GateValue)
                               or nameof(MainViewModel.IsConnected))
                UpdateIcon();

            if (e.PropertyName == nameof(MainViewModel.MidiActivity) && vm.MidiActivity)
            {
                // kurzer Aufblink, Timer neu starten
                SetIcon(ColorActivityBlink);
                _activityTimer.Stop();
                _activityTimer.Start();
            }

            if (e.PropertyName == nameof(MainViewModel.MidiActivity) && !vm.MidiActivity)
                UpdateIcon();   // zurück zur normalen Gate-Farbe
        };

        UpdateIcon();
    }

    // ── Kontextmenü ──────────────────────────────────────────────────────────

    private static ContextMenuStrip BuildContextMenu(MainViewModel vm, MainWindow mainWindow)
    {
        var menu = new ContextMenuStrip();

        var itemShow = new ToolStripMenuItem("Fenster anzeigen");
        itemShow.Click += (_, _) => ShowWindow(mainWindow);

        var itemActive  = new ToolStripMenuItem("Aktivieren (A=0)");
        itemActive.Click  += (_, _) => vm.SetActiveCommand.Execute(null);

        var itemPaused  = new ToolStripMenuItem("Pause (A=1)");
        itemPaused.Click  += (_, _) => vm.SetPausedCommand.Execute(null);

        var itemBlocked = new ToolStripMenuItem("Sperren (A=2)");
        itemBlocked.Click += (_, _) => vm.SetBlockedCommand.Execute(null);

        var itemExit    = new ToolStripMenuItem("Beenden");
        itemExit.Click  += (_, _) =>
        {
            System.Windows.Application.Current.Shutdown();
        };

        menu.Items.Add(itemShow);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(itemActive);
        menu.Items.Add(itemPaused);
        menu.Items.Add(itemBlocked);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(itemExit);

        return menu;
    }

    // ── Icon-Erzeugung ───────────────────────────────────────────────────────

    private void UpdateIcon()
    {
        if (!_vm.IsConnected)
        {
            SetIcon(ColorDisconnected);
            _notifyIcon.Text = "MidiController – Getrennt";
            return;
        }

        switch (_vm.GateValue)
        {
            case 0:
                SetIcon(ColorActive);
                _notifyIcon.Text = "MidiController – Aktiv";
                break;
            case 1:
                SetIcon(ColorPaused);
                _notifyIcon.Text = "MidiController – Pausiert";
                break;
            case 2:
                SetIcon(ColorBlocked);
                _notifyIcon.Text = "MidiController – Gesperrt";
                break;
            default:
                SetIcon(ColorDisconnected);
                _notifyIcon.Text = "MidiController";
                break;
        }
    }

    private void SetIcon(Color color)
    {
        var oldIcon = _notifyIcon.Icon;
        _notifyIcon.Icon = CreateIcon(color);
        oldIcon?.Dispose();
    }

    /// <summary>Erzeugt ein 16×16-Icon mit ausgefülltem Kreis in der angegebenen Farbe.</summary>
    private static Icon CreateIcon(Color color)
    {
        using var bmp = new Bitmap(16, 16);
        using var g   = Graphics.FromImage(bmp);
        g.Clear(Color.Transparent);
        using var brush = new SolidBrush(color);
        g.FillEllipse(brush, 1, 1, 13, 13);
        // Dünner dunklerer Rand
        using var pen = new Pen(Color.FromArgb(120, 0, 0, 0), 1f);
        g.DrawEllipse(pen, 1, 1, 13, 13);
        var hIcon = bmp.GetHicon();
        var icon  = Icon.FromHandle(hIcon);
        return icon;
    }

    // ── Fenster-Steuerung ────────────────────────────────────────────────────

    private static void ShowWindow(MainWindow window)
    {
        window.Show();
        window.WindowState = System.Windows.WindowState.Normal;
        window.Activate();
    }

    public void Dispose()
    {
        _activityTimer.Stop();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
