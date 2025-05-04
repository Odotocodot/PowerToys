// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Data;
using System.Linq;
using Microsoft.CmdPal.Ext.OneNote.Commands;
using Microsoft.CmdPal.Ext.OneNote.Components;
using Microsoft.CmdPal.Ext.OneNote.Pages;
using Microsoft.CmdPal.Ext.OneNote.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Odotocodot.OneNote.Linq;

namespace Microsoft.CmdPal.Ext.OneNote;

internal sealed partial class OneNoteMainPage : DynamicListPage
{
    private readonly ListItem[] _emptyContent;
    private IListItem[] _results;

    public OneNoteMainPage()
    {
        Icon = IconProvider.Logo;
        Title = Constants.PluginName;
        PlaceholderText = Resources.SearchOneNotePages;
        _emptyContent = [
            new ListItem(HierarchyItemPage.Root())
            {
                Title = Resources.ViewNotebookExplorer,
                Subtitle = Resources.ViewNotebookExplorerDescription,
                Icon = IconProvider.NotebookExplorer,
                Details = new Details() { Title = "Details", Body = "Look its some preview" },
            },
            new ListItem(new RecentItemsPage(20)) // TODO: This number will be set in the settings
            {
                Title = Resources.ViewRecentPages,
                Subtitle = Resources.ViewRecentPagesDescription,
                Icon = IconProvider.RecentPage,
            },
            new ListItem()
            {
                Title = Resources.NewQuickNote,
                Icon = IconProvider.QuickNote,
            },
            new ListItem(new OpenSyncNotebooksCommand())
            {
                Title = Resources.OpenSyncNotebooks,
                Icon = IconProvider.SyncNotebooks,
            },
        ];
        _results = _emptyContent;
    }

    public override IListItem[] GetItems()
    {
        return _results;
    }

    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        if (oldSearch == newSearch)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(newSearch))
        {
            UpdateResults(_emptyContent);
            return;
        }

        if (!char.IsLetterOrDigit(newSearch[0]))
        {
            UpdateResults(_emptyContent);
            return;
        }

        var results = OneNoteApplication.FindPages(newSearch)
            .Select(page => new ListItem() { Title = page.Name })
            .ToArray();

        UpdateResults(results);
    }

    public void UpdateResults(IListItem[] results)
    {
        _results = results;
        RaiseItemsChanged(results.Length);
    }
}
