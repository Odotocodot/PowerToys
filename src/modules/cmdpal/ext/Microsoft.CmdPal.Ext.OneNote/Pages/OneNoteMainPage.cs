// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CmdPal.Ext.OneNote.Commands;
using Microsoft.CmdPal.Ext.OneNote.Components;
using Microsoft.CmdPal.Ext.OneNote.Helpers;
using Microsoft.CmdPal.Ext.OneNote.Pages;
using Microsoft.CmdPal.Ext.OneNote.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Odotocodot.OneNote.Linq;

namespace Microsoft.CmdPal.Ext.OneNote;

internal sealed partial class OneNoteMainPage : DynamicListPage
{
    private readonly ListItem[] _commands;
    private IListItem[] _results;

    public OneNoteMainPage()
    {
        Icon = IconProvider.Logo;
        Title = Constants.PluginName;
        PlaceholderText = Resources.SearchOneNotePages;
        EmptyContent = EmptyContentType.Default(null);
        _commands = [
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
        _results = _commands;
    }

    public ListItem[] Query(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return _commands;
        }

        if (!char.IsLetterOrDigit(query[0]))
        {
            return NoResults(EmptyContentType.Invalid);
        }

        IsLoading = true;
        var pages = OneNoteApplication.FindPages(query);
        IsLoading = false;

        return pages.Any() ? ResultHelper.CreateResults(pages, true).ToArray() : NoResults(EmptyContentType.NoMatchesFound);
    }

    public ListItem[] NoResults(CommandItem emptyContent)
    {
        EmptyContent = emptyContent;
        return [];
    }

    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        _results = Query(SearchText);
        RaiseItemsChanged();
    }

    public override IListItem[] GetItems() => _results;
}
