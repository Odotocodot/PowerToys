// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CmdPal.Ext.OneNote.Helpers;
using Microsoft.CmdPal.Ext.OneNote.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Odotocodot.OneNote.Linq;

namespace Microsoft.CmdPal.Ext.OneNote.Pages;

// For OneNote hierarchy items that have children, i.e. everything but a OneNote Page
public partial class HierarchyItemPage : DynamicListPage
{
    private readonly Lazy<List<IOneNoteItem>> _children;
    private readonly IOneNoteItem _item;
    private List<ListItem> _results = [];

    private List<IOneNoteItem> Children => _children.Value;

    public static HierarchyItemPage Root() => new(null, OneNoteApplication.GetNotebooks, Resources.Notebooks);

    private HierarchyItemPage(IOneNoteItem item, Func<IEnumerable<IOneNoteItem>> itemsGetter, string path)
    {
        _item = item;
        _children = new Lazy<List<IOneNoteItem>>(() =>
        {
            IsLoading = true;
            var items = itemsGetter();
            IsLoading = false;
            return items.ToList();
        });
        Title = $"{Constants.PluginName} - {ResultHelper.GetNicePath(path)}";
        Name = Resources.Enter;
        EmptyContent = EmptyContentType.Default(_item);
        UpdateSearchText(string.Empty, string.Empty);
    }

    public HierarchyItemPage(IOneNoteItem item)
        : this(item, () => item.Children, item.RelativePath)
    {
    }

    private List<ListItem> Query(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Children.Count != 0 ? ResultHelper.CreateResults(Children, false).ToList() : NoResults(EmptyContentType.NoMatchesFound);
        }

        // Title search, searches all descendants by title
        if (query.StartsWith(Constants.Keywords.TitleSearch, StringComparison.Ordinal))
        {
            var search = query[Constants.Keywords.TitleSearch.Length..];
            if (search.Length == 0)
            {
                return NoResults(EmptyContentType.SearchByTitle(_item));
            }

            IsLoading = true;
            var filtered = ListHelpers.FilterList(Children.Traverse(), search, (q, item) => StringMatcher.FuzzySearch(q, item.Name).Score);
            IsLoading = false;

            return filtered.Any() ? ResultHelper.CreateResults(filtered, true).ToList() : NoResults(EmptyContentType.NoMatchesFound);
        }

        // Scoped search, searches all descendants, only pages
        if (query.StartsWith(Constants.Keywords.ScopedSearch, StringComparison.Ordinal) && _item != null)
        {
            var search = query[Constants.Keywords.TitleSearch.Length..];
            if (search.Length == 0)
            {
                return NoResults(EmptyContentType.ScopeSearch(_item));
            }

            if (!char.IsLetterOrDigit(search[0]))
            {
                return NoResults(EmptyContentType.Invalid);
            }

            IsLoading = true;
            var scopeResults = ResultHelper.CreateResults(OneNoteApplication.FindPages(search, _item), true).ToList();
            IsLoading = false;
            return scopeResults.Count != 0 ? scopeResults : NoResults(EmptyContentType.NoMatchesFound);
        }

        if (!char.IsLetterOrDigit(query[0]))
        {
            return NoResults(EmptyContentType.Invalid);
        }

        // Search current children
        var results = ListHelpers.FilterList(ResultHelper.CreateResults(Children, false), query).Cast<ListItem>().ToList();

        if (Children.Any(item => string.Equals(query.Trim(), item.Name, StringComparison.Ordinal)))
        {
            return results;
        }

        if (_item?.IsInRecycleBin() == true)
        {
            return results;
        }

        switch (_item)
        {
            // The item that can be created depends on _item i.e. the parent of the Children
            case null:
                results.Add(NewOneNoteItemHelper.NewNotebook(query));
                break;
            case OneNoteNotebook:
            case OneNoteSectionGroup:
                results.Add(NewOneNoteItemHelper.NewSection(query, _item));
                results.Add(NewOneNoteItemHelper.NewSectionGroup(query, _item));
                break;
            case OneNoteSection section when !section.Locked:
                results.Add(NewOneNoteItemHelper.NewPage(query, section));
                break;
        }

        return results;
    }

    public List<ListItem> NoResults(CommandItem emptyContent)
    {
        EmptyContent = emptyContent;
        return [];
    }

    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        _results = Query(SearchText);
        RaiseItemsChanged();
    }

    public override IListItem[] GetItems() => _results.ToArray();
}
