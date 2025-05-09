// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CmdPal.Ext.OneNote.Helpers;
using Microsoft.CmdPal.Ext.OneNote.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Odotocodot.OneNote.Linq;

namespace Microsoft.CmdPal.Ext.OneNote.Commands;

public partial class OpenInOneNoteCommand : InvokableCommand
{
    private readonly IOneNoteItem _item;

    public OpenInOneNoteCommand(IOneNoteItem item)
    {
        _item = item;
        Name = Resources.OpenXInOneNote;
    }

    public override ICommandResult Invoke()
    {
        Invoke(_item);
        return CommandResult.Dismiss();
    }

    public static void Invoke(IOneNoteItem item)
    {
        try
        {
            item.OpenInOneNote();
        }
        catch (COMException)
        {
            // The item longer exists, ignore and do nothing.
            return;
        }

        using var process = Process.GetProcessesByName("onenote").FirstOrDefault();
        var mainHwnd = process?.MainWindowHandle;
        if (mainHwnd.HasValue)
        {
            var hwnd = mainHwnd.Value;
            if (NativeMethods.IsIconic(hwnd))
            {
                if (NativeMethods.ShowWindow(hwnd, NativeMethods.ShowWindowCommand.Restore))
                {
                    _ = NativeMethods.SendMessage(hwnd, NativeMethods.Win32Constants.WM_SYSCOMMAND, NativeMethods.Win32Constants.SC_RESTORE);
                }
            }
            else
            {
                NativeMethods.SetForegroundWindow(hwnd);
            }

            NativeMethods.FlashWindow(hwnd, true);
        }
    }
}
