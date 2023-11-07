// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Drawing;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using ManagedCommon;
using Odotocodot.OneNote.Linq;
using Wox.Plugin;
using Wox.Plugin.Logger;

namespace Microsoft.PowerToys.Run.Plugin.OneNote.Components
{
    public class IconProvider
    {
        private readonly PluginInitContext _context;
        private readonly OneNoteSettings _settings;
        private readonly string _generatedIconsDirectory;
        private readonly ConcurrentDictionary<string, string> _coloredImageCached = new();

        private bool _deleteColoredIconsOnCleanup;

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

        internal string QuickNote => $"Images/page_quick_note.{GetIconType()}.png";

        internal IconProvider(PluginInitContext context, OneNoteSettings settings)
        {
            _settings = settings;
            _context = context;
            _settings.ColoredIconSettingChanged += OnColoredIconSettingChanged;
            _context.API.ThemeChanged += OnThemeChanged;

            _generatedIconsDirectory = $"{_context.CurrentPluginMetadata.PluginDirectory}/Images/Generated/";

            Directory.CreateDirectory(_generatedIconsDirectory);

            foreach (var icon in Directory.EnumerateFiles(_generatedIconsDirectory))
            {
                _coloredImageCached.TryAdd(Path.GetFileNameWithoutExtension(icon), icon);
            }

            UpdateTheme(_context.API.GetCurrentTheme());
        }

        private void OnColoredIconSettingChanged(object? sender, bool coloredIcons) => _deleteColoredIconsOnCleanup = !coloredIcons;

        private void OnThemeChanged(Theme oldTheme, Theme newTheme) => UpdateTheme(newTheme);

        private void UpdateTheme(Theme theme) => _theme = theme == Theme.Light || theme == Theme.HighContrastWhite ? "light" : "dark";

        private string GetIconType(bool hasColoredVersion = false) => hasColoredVersion && _settings.ColoredIcons ? "color" : _theme;

        private string TryGetColoredIcon(string itemType, Color? itemColor)
        {
            if (itemColor is null || !_settings.ColoredIcons)
            {
                return $"Images/{itemType}.{_theme}.png";
            }

            var color = ColorToHex(itemColor.Value);
            var key = $"{itemType}.{color}";

            return _coloredImageCached.GetOrAdd(key, key =>
            {
                // Get base image
                using var reader = XmlReader.Create($"{_context.CurrentPluginMetadata.PluginDirectory}/Images/{itemType}.svg");
                var doc = XDocument.Load(reader);
                var attributes = doc.Root!.Elements()
                                          .Skip(1)
                                          .Elements()
                                          .Skip(1)
                                          .Select(x => x.Attribute("fill"));

                // Change color
                foreach (var attribute in attributes)
                {
                    attribute!.Value = color;
                }

                var filePath = $"{_generatedIconsDirectory}{key}.svg";
                doc.Save(filePath);
                reader.Dispose();
                return filePath;
            });

            static string ColorToHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        internal string GetIcon(IOneNoteItem item) => item switch
        {
            OneNoteNotebook notebook => TryGetColoredIcon(nameof(notebook), notebook.Color),
            OneNoteSectionGroup sectionGroup => sectionGroup.IsRecycleBin ? $"Images/recycleBin.{_theme}.png" : $"Images/section_group.{GetIconType(true)}.png",
            OneNoteSection section => TryGetColoredIcon(nameof(section), section.Color),
            OneNotePage => Page,
            _ => string.Empty,
        };

        internal void Cleanup()
        {
            if (_deleteColoredIconsOnCleanup)
            {
                _coloredImageCached.Clear();
                foreach (var icon in new DirectoryInfo(_generatedIconsDirectory).EnumerateFiles())
                {
                    try
                    {
                        icon.Delete();
                    }
                    catch (Exception ex) when (ex is DirectoryNotFoundException || ex is IOException)
                    {
                        Log.Error($"Failed to delete icon at \"{icon}\"", GetType());
                    }
                }
            }

            if (_settings != null)
            {
                _settings.ColoredIconSettingChanged -= OnColoredIconSettingChanged;
            }

            if (_context != null && _context.API != null)
            {
                _context.API.ThemeChanged -= OnThemeChanged;
            }
        }
    }
}
