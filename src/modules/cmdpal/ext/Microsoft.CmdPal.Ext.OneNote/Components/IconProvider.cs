// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using Microsoft.CommandPalette.Extensions.Toolkit;
using Odotocodot.OneNote.Linq;

namespace Microsoft.CmdPal.Ext.OneNote.Components;

public class IconProvider
{
    public static readonly IconInfo Logo = IconHelpers.FromRelativePath("Assets\\OneNote.png");

    public static IconInfo NotebookExplorer { get; internal set; }

    public static IconInfo QuickNote { get; internal set; }

    public static IconInfo RecentPage { get; internal set; }

    public static IconInfo SyncNotebooks { get; internal set; }

    public static IconInfo GetIcon(IOneNoteItem item) => null;
}
