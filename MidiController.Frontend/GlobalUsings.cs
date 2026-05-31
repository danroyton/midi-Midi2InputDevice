// Globale using-Aliase um Mehrdeutigkeiten zwischen WPF und WinForms aufzulösen.
// Notwendig, da UseWindowsForms=true für NotifyIcon (TrayService) aktiviert wurde.
global using Application  = System.Windows.Application;
global using UserControl  = System.Windows.Controls.UserControl;
global using DataGrid     = System.Windows.Controls.DataGrid;
global using Brush        = System.Windows.Media.Brush;
global using Brushes      = System.Windows.Media.Brushes;
global using KeyEventArgs = System.Windows.Input.KeyEventArgs;
