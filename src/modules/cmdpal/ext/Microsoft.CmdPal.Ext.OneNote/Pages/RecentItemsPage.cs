// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CmdPal.Ext.OneNote.Components;
using Microsoft.CmdPal.Ext.OneNote.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Odotocodot.OneNote.Linq;

namespace Microsoft.CmdPal.Ext.OneNote.Pages;

public partial class RecentItemsPage : ListPage
{
    private readonly IListItem[] _results;

    public RecentItemsPage(int count)
    {
        var pages = OneNoteApplication.GetNotebooks()
                                      .GetPages()
                                      .OrderByDescending(pg => pg.LastModified)
                                      .Take(count);

        _results = ResultHelper.CreateResults(pages, (pg, result) =>
        {
            result.Icon = IconProvider.RecentPage;
            result.Subtitle = ResultHelper.GetNicePath(pg.RelativePath);

            // TODO: add last time it was edited to tag maybe
        }).ToArray();
    }

    public override IListItem[] GetItems() => _results;
}
