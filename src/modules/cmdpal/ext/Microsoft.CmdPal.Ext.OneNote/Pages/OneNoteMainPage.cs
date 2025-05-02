// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CmdPal.Ext.OneNote.Components;
using Microsoft.CmdPal.Ext.OneNote.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace Microsoft.CmdPal.Ext.OneNote;

internal sealed partial class OneNoteMainPage : ListPage
{
    public OneNoteMainPage()
    {
        Icon = IconProvider.Logo;
        Title = "OneNote";
    }

    public override string PlaceholderText => Resources.SearchOneNotePages;

    public override IListItem[] GetItems()
    {
        return [
            new ListItem(new NoOpCommand())
            {
                Title = Resources.ViewNotebookExplorer,
                Subtitle = Resources.ViewNotebookExplorerDescription,
            },
            new ListItem(new NoOpCommand())
            {
                Title = Resources.ViewRecentPages,
                Subtitle = Resources.ViewRecentPagesDescription,
            },
            new ListItem(new NoOpCommand())
            {
                Title = Resources.NewQuickNote,
            },
            new ListItem(new NoOpCommand())
            {
                Title = Resources.OpenSyncNotebooks,
            }
        ];
    }
}
