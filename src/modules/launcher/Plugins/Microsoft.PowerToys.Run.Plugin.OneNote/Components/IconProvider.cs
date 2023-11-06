// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using ManagedCommon;
using Odotocodot.OneNote.Linq;
using Wox.Plugin;

namespace Microsoft.PowerToys.Run.Plugin.OneNote.Components
{
    public class IconProvider
    {
        private readonly PluginInitContext _context;
        private readonly OneNoteSettings _settings;

        private string _theme = "light";

        internal string NewPage => $"Images/page_new.{GetIconType(true)}.png";

        internal string NewSection => $"Images/section_new.{GetIconType()}.png";

        internal string NewSectionGroup => $"Images/section_group_new.{GetIconType(true)}.png";

        internal string NewNotebook => $"Images/notebook_new.{GetIconType()}.png";

        internal string Page => $"Images/page.{GetIconType(true)}.png";

        internal string Recent => $"Images/page_recent.{GetIconType()}.png";

        internal string Sync => $"Images/sync.{GetIconType()}.png";

        internal string Search => $"Images/search.{GetIconType()}.png";

        internal string NotebookExplorer => $"Images/notebook_explorer.{GetIconType()}.png";

        internal string Warning => $"Images/warning.{GetIconType()}.png";

        internal string QuickNote => $"Images/page_new.{GetIconType()}.png";

        internal IconProvider(PluginInitContext context, OneNoteSettings settings)
        {
            _settings = settings;
            _context = context;
            _context.API.ThemeChanged += OnThemeChanged;
            UpdateTheme(_context.API.GetCurrentTheme());
        }

        private void OnThemeChanged(Theme oldTheme, Theme newTheme)
        {
            UpdateTheme(newTheme);
        }

        private void UpdateTheme(Theme theme)
        {
            _theme = theme == Theme.Light || theme == Theme.HighContrastWhite ? "light" : "dark";
        }

        private string GetIconType(bool hasColoredVersion = false) => hasColoredVersion && _settings.ColoredIcons ? "color" : _theme;

        internal string GetIcon(IOneNoteItem item) => item switch
        {
            OneNoteNotebook => $"Images/notebook.{_theme}.png",
            OneNoteSectionGroup sectionGroup => sectionGroup.IsRecycleBin ? $"Images/recycleBin.{_theme}.png" : $"Images/section_group.{GetIconType(true)}.png",
            OneNoteSection => $"Images/section.{_theme}.png",
            OneNotePage => Page,
            _ => string.Empty,
        };
    }
}
