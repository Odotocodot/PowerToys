// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CmdPal.Ext.OneNote.Components;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Odotocodot.OneNote.Linq;

namespace Microsoft.CmdPal.Ext.OneNote.Pages;

// For OneNote hierarchy items that have children, i.e. everything but a OneNote Page
// TODO: if no children, display empty content
// TODO: support for scoped and scoped title searching
public partial class HierarchyItemPage : ListPage
{
    private readonly IOneNoteItem _item;
    private readonly IListItem[] _results;

    public HierarchyItemPage(string path, IEnumerable<IOneNoteItem> items)
    {
        _results = ResultCreator.CreateResults(items);
        Title = $"{Constants.PluginName} - {ResultCreator.GetNicePath(path)}";
        Name = "Enter";
    }

    public HierarchyItemPage(IOneNoteItem item)
        : this(item.RelativePath, item.Children)
    {
        _item = item;
    }

    public static HierarchyItemPage Root() => new("Notebooks", OneNoteApplication.GetNotebooks());

    public override IListItem[] GetItems() => _results;
}
