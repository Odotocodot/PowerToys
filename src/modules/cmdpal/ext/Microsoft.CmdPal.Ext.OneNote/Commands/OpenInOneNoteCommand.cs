// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        _item.OpenInOneNote();
        return CommandResult.Dismiss();
    }
}
