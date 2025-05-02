// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CmdPal.Ext.OneNote.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace Microsoft.CmdPal.Ext.OneNote;

public partial class OneNoteCommandsProvider : CommandProvider
{
    private readonly ICommandItem[] _commands;

    public OneNoteCommandsProvider()
    {
        DisplayName = "OneNote";
        Id = "OneNote";
        _commands = [
            new CommandItem(new OneNoteMainPage())
            {
                Title = DisplayName,
                Subtitle = Resources.PluginDescription,
            },
        ];
    }

    public override ICommandItem[] TopLevelCommands() => _commands;
}
