// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Microsoft.CmdPal.Ext.OneNote.Helpers;

[SuppressMessage("Interoperability", "CA1401:P/Invokes should not be visible", Justification = "We want plugins to share this NativeMethods class, instead of each one creating its own.")]
public static class NativeMethods
{
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hwnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ShowWindow(IntPtr hwnd, ShowWindowCommand nCmdShow);

    [DllImport("user32.dll")]
    public static extern bool FlashWindow(IntPtr hwnd, bool bInvert);

    [DllImport("user32.dll")]
    public static extern int SendMessage(IntPtr hwnd, int msg, int wParam);

    [DllImport("user32.dll")]
    public static extern bool IsIconic(IntPtr hwnd);

    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:Field names should not contain underscore", Justification = "These are the names used by win32.")]
    public static class Win32Constants
    {
        /// <summary>
        /// A window receives this message when the user chooses a command from the Window menu (formerly known as the system or control menu)
        /// or when the user chooses the maximize button, minimize button, restore button, or close button.
        /// </summary>
        public const int WM_SYSCOMMAND = 0x0112;

        /// <summary>
        /// Restores the window to its normal position and size.
        /// </summary>
        public const int SC_RESTORE = 0xf120;
    }

    /// <summary>
    /// Show Window Enums
    /// </summary>
    public enum ShowWindowCommand
    {
        /// <summary>
        /// Activates and displays the window. If the window is minimized or
        /// maximized, the system restores it to its original size and position.
        /// An application should specify this flag when restoring a minimized window.
        /// </summary>
        Restore = 9,
    }
}
