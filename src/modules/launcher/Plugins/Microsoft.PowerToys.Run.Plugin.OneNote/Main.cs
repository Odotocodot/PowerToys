// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using LazyCache;
using ManagedCommon;
using Microsoft.PowerToys.Run.Plugin.OneNote.Components;
using Microsoft.PowerToys.Run.Plugin.OneNote.Properties;
using Microsoft.PowerToys.Settings.UI.Library;
using Odotocodot.OneNote.Linq;
using Wox.Plugin;

namespace Microsoft.PowerToys.Run.Plugin.OneNote
{
    /// <summary>
    /// A power launcher plugin to search across time zones.
    /// </summary>
    public class Main : IPlugin, IDelayedExecutionPlugin, IPluginI18n, ISettingProvider, IContextMenu
    {
        /// <summary>
        /// A value indicating if the OneNote interop library was able to successfully initialize.
        /// </summary>
        private bool _oneNoteInstalled;

        /// <summary>
        /// LazyCache CachingService instance to speed up repeated queries.
        /// </summary>
        private CachingService? _cache;

        /// <summary>
        /// The initial context for this plugin (contains API and meta-data)
        /// </summary>
        private PluginInitContext? _context;

        /// <summary>
        /// The path to the icon for each result
        /// </summary>
        private string? _iconPath;

        private SearchManager? _searchManager;

        private Lazy<OneNoteSettings> _settings = new();

        /// <summary>
        /// Gets the localized name.
        /// </summary>
        public string Name => Resources.PluginTitle;

        /// <summary>
        /// Gets the localized description.
        /// </summary>
        public string Description => Resources.PluginDescription;

        /// <summary>
        /// Gets the plugin ID for validation
        /// </summary>
        public static string PluginID => "0778F0C264114FEC8A3DF59447CF0A74";

        public IEnumerable<PluginAdditionalOption> AdditionalOptions => Settings.AdditionalOptions;

        internal OneNoteSettings Settings => _settings.Value;

        /// <summary>
        /// Initialize the plugin with the given <see cref="PluginInitContext"/>
        /// </summary>
        /// <param name="context">The <see cref="PluginInitContext"/> for this plugin</param>
        public void Init(PluginInitContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            try
            {
                OneNoteApplication.InitComObject();
                _oneNoteInstalled = OneNoteApplication.HasComObject;
                OneNoteApplication.ReleaseComObject();

                if (_oneNoteInstalled)
                {
                    _cache = GetCache();
                }
            }
            catch (COMException)
            {
                // OneNote isn't installed, plugin won't do anything.
                _oneNoteInstalled = false;
            }

            var resultCreator = new ResultCreator(context, Settings);
            _searchManager = new SearchManager(context, Settings, resultCreator);

            _context.API.ThemeChanged += OnThemeChanged;
            UpdateIconPath(_context.API.GetCurrentTheme());
        }

        private CachingService GetCache()
        {
            var cache = new CachingService();
            cache.DefaultCachePolicy.DefaultCacheDurationSeconds = (int)TimeSpan.FromDays(1).TotalSeconds;
            return cache;
        }

        /// <summary>
        /// Return a filtered list, based on the given query
        /// </summary>
        /// <param name="query">The query to filter the list</param>
        /// <returns>A filtered list, can be empty when nothing was found</returns>
        public List<Result> Query(Query query)
        {
            if (!_oneNoteInstalled)
            {
                return ResultCreator.OneNoteNotInstalled();
            }

            if (string.IsNullOrWhiteSpace(query?.Search))
            {
                return _searchManager!.EmptyQuery(query);
            }

            _cache ??= GetCache();

            // If there's cached results for this query, return immediately, otherwise wait for delayedExecution.
            var results = _cache.Get<List<Result>>(query.Search);
            return results ?? Query(query, false);
        }

        /// <summary>
        /// Return a filtered list, based on the given query
        /// </summary>
        /// <param name="query">The query to filter the list</param>
        /// <param name="delayedExecution">False if this is the first pass through plugins, true otherwise. Slow plugins should run delayed.</param>
        /// <returns>A filtered list, can be empty when nothing was found</returns>
        public List<Result> Query(Query query, bool delayedExecution)
        {
            if (!_oneNoteInstalled)
            {
                return ResultCreator.OneNoteNotInstalled();
            }

            if (string.IsNullOrWhiteSpace(query?.Search))
            {
                return _searchManager!.EmptyQuery(query);
            }

            _cache ??= GetCache();

            // Get results from cache if they already exist for this query, otherwise query OneNote. Results will be cached for 1 day.
            return _cache.GetOrAdd(query.Search, () => _searchManager!.Query(query));
        }

        /// <summary>
        /// Return the translated plugin title.
        /// </summary>
        /// <returns>A translated plugin title.</returns>
        public string GetTranslatedPluginTitle() => Resources.PluginTitle;

        /// <summary>
        /// Return the translated plugin description.
        /// </summary>
        /// <returns>A translated plugin description.</returns>
        public string GetTranslatedPluginDescription() => Resources.PluginDescription;

        private void OnThemeChanged(Theme currentTheme, Theme newTheme)
        {
            UpdateIconPath(newTheme);
        }

        [MemberNotNull(nameof(_iconPath))]
        private void UpdateIconPath(Theme theme)
        {
            _iconPath = theme == Theme.Light || theme == Theme.HighContrastWhite ? "Images/oneNote.light.png" : "Images/oneNote.dark.png";
            _iconPath = "Images/temp.svg";
        }

        public List<ContextMenuResult> LoadContextMenus(Result selectedResult)
        {
            return _searchManager!.LoadContextMenu(selectedResult);
        }

        public Control CreateSettingPanel()
        {
            throw new NotImplementedException();
        }

        public void UpdateSettings(PowerLauncherPluginSettings settings)
        {
            Settings.UpdateSettings(settings);
        }
    }
}
