// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CmdPal.Ext.OneNote.Commands;
using Microsoft.CmdPal.Ext.OneNote.Pages;
using Microsoft.CmdPal.Ext.OneNote.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Odotocodot.OneNote.Linq;

namespace Microsoft.CmdPal.Ext.OneNote.Components;

public static class ResultCreator
{
    public static IListItem[] CreateResults(IEnumerable<IOneNoteItem> items)
    {
        return items.Select(CreateResult).ToArray();
    }

    private static IListItem CreateResult(IOneNoteItem item)
    {
        ICommand command;
        IContextItem[] moreCommands;
        if (item is OneNotePage)
        {
            command = new OpenInOneNoteCommand(item);
            moreCommands = null;
        }
        else
        {
            command = new HierarchyItemPage(item);
            moreCommands = [new CommandContextItem(new OpenInOneNoteCommand(item))];
        }

        var result = new ListItem(command)
        {
            Title = item.Name,
            MoreCommands = moreCommands,
            Icon = IconProvider.GetIcon(item),
        };

        var tags = new List<Tag>();
        tags.AddConditionally(item.IsUnread, Resources.Unread);
        if (item is OneNoteSection section)
        {
            tags.AddConditionally(section.Encrypted, Resources.Encrypted);
            tags.AddConditionally(section.Locked, Resources.Locked);
        }

        result.Tags = tags.ToArray();
        return result;
    }

    private static void AddConditionally(this List<Tag> tags, bool condition, string name)
    {
        if (condition)
        {
            tags.Add(new Tag(name));
        }
    }
}
