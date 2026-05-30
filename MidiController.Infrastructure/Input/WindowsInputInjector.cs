using MidiController.Domain.Interfaces;
using MidiController.Infrastructure.Interop;
using Microsoft.Extensions.Logging;

namespace MidiController.Infrastructure.Input;

/// <summary>
/// Implementiert <see cref="IInputInjector"/> via Win32 SendInput (P/Invoke).
/// </summary>
public sealed class WindowsInputInjector : IInputInjector
{
    private readonly ILogger<WindowsInputInjector> _logger;

    public WindowsInputInjector(ILogger<WindowsInputInjector> logger)
    {
        _logger = logger;
    }

    public void SendKeyCombination(string[] keys, int repeat, int holdDurationMs, int pauseAfterMs)
    {
        if (repeat <= 0 || keys.Length == 0)
            return;

        ushort[] vks;
        try
        {
            vks = keys.Select(VirtualKeyMapper.Resolve).ToArray();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Unbekannter Tasten-Name – Tastendruck wird übersprungen.");
            return;
        }

        for (int i = 0; i < repeat; i++)
        {
            PressKeys(vks, holdDurationMs);

            if (pauseAfterMs > 0)
                Thread.Sleep(pauseAfterMs);
        }
    }

    private static void PressKeys(ushort[] vks, int holdDurationMs)
    {
        // Key-Down für alle Tasten
        var downInputs = vks.Select(vk => MakeKeyInput(vk, keyUp: false)).ToArray();
        NativeMethods.SendInput((uint)downInputs.Length, downInputs, System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.INPUT>());

        if (holdDurationMs > 0)
            Thread.Sleep(holdDurationMs);

        // Key-Up in umgekehrter Reihenfolge
        var upInputs = vks.Reverse().Select(vk => MakeKeyInput(vk, keyUp: true)).ToArray();
        NativeMethods.SendInput((uint)upInputs.Length, upInputs, System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.INPUT>());
    }

    private static NativeMethods.INPUT MakeKeyInput(ushort vk, bool keyUp) => new()
    {
        type = NativeMethods.INPUT_KEYBOARD,
        u = new NativeMethods.InputUnion
        {
            ki = new NativeMethods.KEYBDINPUT
            {
                wVk = vk,
                wScan = 0,
                dwFlags = keyUp ? NativeMethods.KEYEVENTF_KEYUP : 0,
                time = 0,
                dwExtraInfo = 0
            }
        }
    };
}
