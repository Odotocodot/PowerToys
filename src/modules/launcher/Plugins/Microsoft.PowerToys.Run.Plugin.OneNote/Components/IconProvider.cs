// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using ManagedCommon;
using Odotocodot.OneNote.Linq;
using Wox.Infrastructure.Image;
using Wox.Plugin;
using Wox.Plugin.Logger;

namespace Microsoft.PowerToys.Run.Plugin.OneNote.Components
{
    public class IconProvider
    {
        private readonly PluginInitContext _context;
        private readonly OneNoteSettings _settings;
        private readonly string _imagesDirectory;
        private readonly string _generatedImagesDirectory;
        private readonly ConcurrentDictionary<string, BitmapSource> _imageCache = new();

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

            _imagesDirectory = $"{_context.CurrentPluginMetadata.PluginDirectory}/Images/";
            _generatedImagesDirectory = $"{_context.CurrentPluginMetadata.PluginDirectory}/Images/Generated/";

            Directory.CreateDirectory(_generatedImagesDirectory);

            foreach (var imagePath in Directory.EnumerateFiles(_generatedImagesDirectory))
            {
                _imageCache.TryAdd(Path.GetFileNameWithoutExtension(imagePath), Path2Bitmap(imagePath));
            }

            UpdateTheme(_context.API.GetCurrentTheme());
        }

        private void OnColoredIconSettingChanged(object? sender, bool coloredIcons) => _deleteColoredIconsOnCleanup = !coloredIcons;

        private void OnThemeChanged(Theme oldTheme, Theme newTheme) => UpdateTheme(newTheme);

        private void UpdateTheme(Theme theme) => _theme = theme == Theme.Light || theme == Theme.HighContrastWhite ? "light" : "dark";

        private string GetIconType(bool hasColoredVersion = false) => hasColoredVersion && _settings.ColoredIcons ? "color" : _theme;

        internal System.Windows.Media.ImageSource GetIcon(IOneNoteItem item)
        {
            string key;
            switch (item)
            {
                case OneNoteNotebook notebook:
                    if (!_settings.ColoredIcons || notebook.Color is null)
                    {
                        key = $"{nameof(notebook)}.{_theme}";
                        break;
                    }
                    else
                    {
                        return GetColoredIcon(nameof(notebook), notebook.Color.Value);
                    }

                case OneNoteSectionGroup sectionGroup:
                    key = sectionGroup.IsRecycleBin ? $"recycle_bin.{_theme}" : $"section_group.{GetIconType(true)}";
                    break;

                case OneNoteSection section:
                    if (!_settings.ColoredIcons || section.Color is null)
                    {
                        key = $"{nameof(section)}.{_theme}";
                        break;
                    }
                    else
                    {
                        return GetColoredIcon(nameof(section), section.Color.Value);
                    }

                case OneNotePage:
                    key = Path.GetFileNameWithoutExtension(Page);
                    break;

                default:
                    throw new NotImplementedException();
            }

            return _imageCache.GetOrAdd(key, key => Path2Bitmap($"{_imagesDirectory}{key}.png"));
        }

        private BitmapSource GetColoredIcon(string itemType, Color itemColor)
        {
            var key = $"{itemType}.{itemColor.ToArgb()}";

            return _imageCache.GetOrAdd(key, key =>
            {
                var color = itemColor;
                using var bitmap = new Bitmap($"{_imagesDirectory}{itemType}.dark.png");
                BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);

                int bytesPerPixel = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;
                byte[] pixels = new byte[bitmapData.Stride * bitmap.Height];
                IntPtr pointer = bitmapData.Scan0;
                Marshal.Copy(pointer, pixels, 0, pixels.Length);
                int bytesWidth = bitmapData.Width * bytesPerPixel;

                for (int j = 0; j < bitmapData.Height; j++)
                {
                    int line = j * bitmapData.Stride;
                    for (int i = 0; i < bytesWidth; i += bytesPerPixel)
                    {
                        pixels[line + i] = color.B;
                        pixels[line + i + 1] = color.G;
                        pixels[line + i + 2] = color.R;
                    }
                }

                Marshal.Copy(pixels, 0, pointer, pixels.Length);
                bitmap.UnlockBits(bitmapData);

                var filePath = $"{_generatedImagesDirectory}{key}.png";
                bitmap.Save(filePath, ImageFormat.Png);
                return Path2Bitmap(filePath);
            });
        }

        private BitmapSource Path2Bitmap(string path) => WindowsThumbnailProvider.GetThumbnail(path, Constant.ThumbnailSize, Constant.ThumbnailSize, ThumbnailOptions.ThumbnailOnly);

        internal void Cleanup()
        {
            _imageCache.Clear();
            if (_deleteColoredIconsOnCleanup)
            {
                foreach (var file in new DirectoryInfo(_generatedImagesDirectory).EnumerateFiles())
                {
                    try
                    {
                        file.Delete();
                    }
                    catch (Exception ex) when (ex is DirectoryNotFoundException || ex is IOException)
                    {
                        Log.Error($"Failed to delete icon at \"{file}\"", GetType());
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
