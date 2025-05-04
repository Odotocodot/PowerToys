// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.CmdPal.Ext.OneNote.Commands;
using Microsoft.CmdPal.Ext.OneNote.Pages;
using Microsoft.CmdPal.Ext.OneNote.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Odotocodot.OneNote.Linq;

namespace Microsoft.CmdPal.Ext.OneNote.Components;

public static class ResultCreator
{
    private static readonly CompositeFormat LastModified = CompositeFormat.Parse(Resources.LastModified);
    private static readonly string _oldSeparator = OneNoteApplication.RelativePathSeparator.ToString();

    public static string GetNicePath(string path) => path.Replace(_oldSeparator, " > ");

    public static IListItem[] CreateResults(IEnumerable<IOneNoteItem> items)
    {
        return items.Select(CreateResult).ToArray();
    }

    private static ListItem CreateResult(IOneNoteItem item)
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
            Subtitle = string.Format(CultureInfo.CurrentCulture, LastModified, item.LastModified), // Humanize?
            TextToSuggest = item.Name,
        };

        var tags = new List<Tag>();
        tags.AddConditionally(item.IsUnread, Resources.Unread);

        // tags.AddConditionally(item.IsInRecycleBin(), Resources.RecycleBin);
        if (item is OneNoteSection section)
        {
            tags.AddConditionally(section.Encrypted, Resources.Encrypted);
            tags.AddConditionally(section.Locked, Resources.Locked);
        }

        result.Tags = tags.ToArray();
        return result;
    }

    public static IListItem[] GetRecentPageResults(IEnumerable<IOneNoteItem> items)
    {
        return items.Select(item =>
        {
            var result = CreateResult(item);
            result.Icon = IconProvider.RecentPage;
            result.Subtitle = GetNicePath(item.RelativePath);

            // TODO: add last time it was edited to tag maybe
            return result;
        }).ToArray();
    }

    // TODO: Cache tags and use icons when applicable
    private static void AddConditionally(this List<Tag> tags, bool condition, string name)
    {
        if (condition)
        {
            tags.Add(new Tag(name));
        }
    }
}
