// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Text;
using Microsoft.CmdPal.Ext.OneNote.Components;
using Microsoft.CmdPal.Ext.OneNote.Properties;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Odotocodot.OneNote.Linq;

namespace Microsoft.CmdPal.Ext.OneNote.Helpers;

public static class EmptyContentType
{
    private static readonly CompositeFormat SearchingByTitleInX = CompositeFormat.Parse(Resources.SearchingByTitleInX);
    private static readonly CompositeFormat SearchingPagesInX = CompositeFormat.Parse(Resources.SearchingPagesInX);
    private static readonly CompositeFormat TitleSearchDescription = CompositeFormat.Parse(Resources.TitleSearchDescription);
    private static readonly CompositeFormat ScopedSearchDescription = CompositeFormat.Parse(Resources.ScopedSearchDescription);

    public static CommandItem Default(IOneNoteItem item)
    {
        var subtitle = $"{Resources.Tip} {string.Format(CultureInfo.CurrentCulture, TitleSearchDescription, Constants.Keywords.TitleSearch)}";
        if (item != null)
        {
            subtitle += $"\n {string.Format(CultureInfo.CurrentCulture, ScopedSearchDescription, Constants.Keywords.ScopedSearch)}";
        }

        return new CommandItem
        {
            Title = Resources.SearchOneNotePages,
            Subtitle = subtitle,
            Icon = IconProvider.Search,
        };
    }

    public static readonly CommandItem Invalid = new()
    {
        Title = Resources.InvalidQuery,
        Subtitle = Resources.InvalidQueryDescription,
        Icon = IconProvider.InvalidQuery,
    };

    public static readonly CommandItem NoMatchesFound = new()
    {
        Title = Resources.NoMatchesFound,
        Subtitle = Resources.NoMatchesFoundDescription,
        Icon = IconProvider.Search,
    };

    public static CommandItem SearchByTitle(IOneNoteItem item)
    {
        return new CommandItem
        {
            Title = item == null ? Resources.SearchingByTitle : string.Format(CultureInfo.CurrentCulture, SearchingByTitleInX, item.Name),
            Icon = IconProvider.Search,
        };
    }

    public static CommandItem ScopeSearch(IOneNoteItem item)
    {
        return new CommandItem
        {
            Title = item == null ? Resources.SearchOneNotePages : string.Format(CultureInfo.CurrentCulture, SearchingPagesInX, item.Name),
            Icon = IconProvider.Search,
        };
    }
}
