// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Odotocodot.OneNote.Linq;

namespace Microsoft.CmdPal.Ext.OneNote.Commands;

public partial class OpenSyncNotebooksCommand : InvokableCommand
{
    public override CommandResult Invoke()
    {
        foreach (var notebook in OneNoteApplication.GetNotebooks())
        {
            notebook.Sync();
        }

        var recent = OneNoteApplication.GetNotebooks()
                                       .GetPages()
                                       .Where(i => !i.IsInRecycleBin)
                                       .OrderByDescending(pg => pg.LastModified)
                                       .FirstOrDefault();

        if (recent != null)
        {
            OpenInOneNoteCommand.Invoke(recent);
        }

        return CommandResult.Dismiss();
    }
}
