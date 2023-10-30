// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Windows.Input;
using Odotocodot.OneNote.Linq;
using Wox.Infrastructure;
using Wox.Plugin;

namespace Microsoft.PowerToys.Run.Plugin.OneNote.Components
{
    public class SearchManager
    {
        private readonly PluginInitContext _context;
        private readonly OneNoteSettings _settings;
        private readonly ResultCreator _resultCreator;

        internal SearchManager(PluginInitContext context, OneNoteSettings settings, ResultCreator resultCreator)
        {
            _context = context;
            _settings = settings;
            _resultCreator = resultCreator;
        }

        internal List<Result> Query(Query query)
        {
            // Three scenarios:
            // Global on,  ActionKeyword used       -> fancy stuffs
            // Global on,  ActionKeyword not used   -> query.Search
            // Global off, ActionKeyword used       -> query.Search
            string search = query.Search;
            if (_context.CurrentPluginMetadata.IsGlobal && query.RawUserQuery.StartsWith(query.ActionKeyword, StringComparison.Ordinal))
            {
                search = query.RawUserQuery[query.ActionKeyword.Length..].TrimStart();
            }

            return search switch
            {
                string s when s.StartsWith(Keywords.RecentPages, StringComparison.Ordinal)
                    => RecentPages(s),
                string s when s.StartsWith(Keywords.NotebookExplorer, StringComparison.Ordinal)
                    => NotebookExplorer(query),
                string s when s.StartsWith(Keywords.TitleSearch, StringComparison.Ordinal)
                    => TitleSearch(string.Join(' ', query.Terms), OneNoteApplication.GetNotebooks()),
                _ => DefaultSearch(query.Search),
            };
        }

        internal List<Result> EmptyQuery(Query? query)
        {
            if (_context.CurrentPluginMetadata.IsGlobal && query?.RawUserQuery.StartsWith(query.ActionKeyword, StringComparison.Ordinal) != true)
            {
                return new List<Result>();
            }

            if (query is null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            return new List<Result>
            {
                new Result
                {
                    Title = "Search OneNote pages",
                    QueryTextDisplay = string.Empty,
                    IcoPath = IconProvider.Logo,
                    Score = 5000,
                },
                new Result
                {
                    Title = "View notebook explorer",
                    SubTitle = $"Type \"{Keywords.NotebookExplorer}\" or select this option to search by notebook structure ",
                    QueryTextDisplay = $"{Keywords.NotebookExplorer}",
                    IcoPath = IconProvider.Notebook,
                    Score = 2000,
                    Action = ResultCreator.ResultAction(() =>
                    {
                        _context.API.ChangeQuery($"{_context.CurrentPluginMetadata.ActionKeyword} {Keywords.NotebookExplorer}", true);
                        return false;
                    }),
                },
                new Result
                {
                    Title = "See recent pages",
                    SubTitle = $"Type \"{Keywords.RecentPages}\" or select this option to see recently modified pages",
                    QueryTextDisplay = $"{Keywords.RecentPages}",
                    IcoPath = IconProvider.Recent,
                    Score = -1000,
                    Action = ResultCreator.ResultAction(() =>
                    {
                        _context.API.ChangeQuery($"{_context.CurrentPluginMetadata.ActionKeyword} {Keywords.RecentPages}", true);
                        return false;
                    }),
                },
                new Result
                {
                    Title = "New quick note",
                    IcoPath = IconProvider.NewPage,
                    Score = -4000,
                    Action = ResultCreator.ResultAction(() =>
                    {
                        OneNoteApplication.CreateQuickNote(true);
                        return true;
                    }),
                },
                new Result
                {
                    Title = "Open and sync notebooks",
                    IcoPath = IconProvider.Sync,
                    Score = int.MinValue,
                    Action = ResultCreator.ResultAction(() =>
                    {
                        foreach (var notebook in OneNoteApplication.GetNotebooks())
                        {
                            notebook.Sync();
                        }

                        OneNoteApplication.GetNotebooks()
                                          .GetPages()
                                          .Where(i => !i.IsInRecycleBin)
                                          .OrderByDescending(pg => pg.LastModified)
                                          .First()
                                          .OpenItemInOneNote();
                        return true;
                    }),
                },
            };
        }

        private List<Result> NotebookExplorer(Query query)
        {
            var results = new List<Result>();

            string fullSearch = query.Search.Remove(query.Search.IndexOf(Keywords.NotebookExplorer, StringComparison.Ordinal), Keywords.NotebookExplorer.Length);

            IOneNoteItem? parent = null;
            IEnumerable<IOneNoteItem> collection = OneNoteApplication.GetNotebooks();

            string[] searches = fullSearch.Split(Keywords.NotebookExplorerSeparator, StringSplitOptions.None);

            for (int i = -1; i < searches.Length - 1; i++)
            {
                if (i < 0)
                {
                    continue;
                }

                parent = collection.FirstOrDefault(item => item.Name.Equals(searches[i], StringComparison.Ordinal));
                if (parent == null)
                {
                    return results;
                }

                collection = parent.Children;
            }

            string lastSearch = searches[^1];

            results = lastSearch switch
            {
                // Empty search so show all in collection
                string search when string.IsNullOrWhiteSpace(search)
                    => NotebookEmptySearch(parent, collection),

                // Search by title
                string search when search.StartsWith(Keywords.TitleSearch, StringComparison.Ordinal) && parent is not OneNotePage
                    => TitleSearch(search, collection, parent),

                // Scoped search
                string search when search.StartsWith(Keywords.ScopedSearch, StringComparison.Ordinal) && (parent is OneNoteNotebook || parent is OneNoteSectionGroup)
                    => ScopedSearch(search, parent),

                // Default search
                _ => NotebookDefaultSearch(collection, lastSearch),
            };

            if (parent != null)
            {
                var result = _resultCreator.CreateOneNoteItemResult(parent, false, score: 4000);
                result.Title = $"Open \"{parent.Name}\" in OneNote";
                result.SubTitle = lastSearch switch
                {
                    string search when search.StartsWith(Keywords.TitleSearch, StringComparison.Ordinal)
                        => $"Now search by title in \"{parent.Name}\"",

                    string search when search.StartsWith(Keywords.ScopedSearch, StringComparison.Ordinal)
                        => $"Now searching all pages in \"{parent.Name}\"",

                    _ => $"Use \'{Keywords.ScopedSearch}\' to search this item. Use \'{Keywords.TitleSearch}\' to search by title in this item",
                };

                results.Add(result);
            }

            return results;
        }

        private List<Result> NotebookDefaultSearch(IEnumerable<IOneNoteItem> collection, string lastSearch)
        {
            List<int>? highlightData = null;
            int score = 0;

            var results = collection.Where(SettingsCheck)
                                    .Where(item => FuzzySearch(item.Name, lastSearch, out highlightData, out score))
                                    .Select(item => _resultCreator.CreateOneNoteItemResult(item, true, highlightData, score))
                                    .ToList();

            AddCreateNewOneNoteItemResults(results, null, lastSearch);
            return results;
        }

        private List<Result> NotebookEmptySearch(IOneNoteItem? parent, IEnumerable<IOneNoteItem> collection)
        {
            List<Result> results = collection.Where(SettingsCheck)
                                             .Select(item => _resultCreator.CreateOneNoteItemResult(item, true))
                                             .ToList();
            if (!results.Any())
            {
                // parent can be null if the collection only contains notebooks.
                switch (parent)
                {
                    case OneNoteNotebook:
                    case OneNoteSectionGroup:
                        // Can create section/section group
                        results.Add(NoItemsInCollectionResult("section", IconProvider.NewSection, "(unencrypted) section"));
                        results.Add(NoItemsInCollectionResult("section group", IconProvider.NewSectionGroup));
                        break;
                    case OneNoteSection section:
                        // Can create page
                        if (!section.Locked)
                        {
                            results.Add(NoItemsInCollectionResult("page", IconProvider.NewPage));
                        }

                        break;
                    default:
                        break;
                }
            }

            return results;

            static Result NoItemsInCollectionResult(string title, string iconPath, string? subTitle = null)
            {
                return new Result
                {
                    Title = $"Create {title}: \"\"",
                    SubTitle = $"No {subTitle ?? title}s found. Type a valid title to create one",
                    IcoPath = iconPath,
                };
            }
        }

        private List<Result> ScopedSearch(string query, IOneNoteItem parent)
        {
            if (query.Length == Keywords.ScopedSearch.Length)
            {
                return ResultCreator.NoMatchesFound();
            }

            if (!char.IsLetterOrDigit(query[Keywords.ScopedSearch.Length]))
            {
                return ResultCreator.InvalidQuery();
            }

            string currentSearch = query[Keywords.TitleSearch.Length..];
            var results = new List<Result>();

            results = OneNoteApplication.FindPages(currentSearch, parent)
                                        .Select(pg => _resultCreator.CreatePageResult(pg, currentSearch))
                                        .ToList();

            if (!results.Any())
            {
                results = ResultCreator.NoMatchesFound();
            }

            return results;
        }

        private void AddCreateNewOneNoteItemResults(List<Result> results, IOneNoteItem? parent, string query)
        {
            if (!results.Any(result => string.Equals(query.Trim(), result.Title, StringComparison.OrdinalIgnoreCase)))
            {
                if (parent?.IsInRecycleBin() == true)
                {
                    return;
                }

                switch (parent)
                {
                    case null:
                        results.Add(_resultCreator.CreateNewNotebookResult(query));
                        break;
                    case OneNoteNotebook:
                    case OneNoteSectionGroup:
                        results.Add(_resultCreator.CreateNewSectionResult(query, parent));
                        results.Add(_resultCreator.CreateNewSectionGroupResult(query, parent));
                        break;
                    case OneNoteSection section:
                        if (!section.Locked)
                        {
                            results.Add(ResultCreator.CreateNewPageResult(query, section));
                        }

                        break;
                    default:
                        break;
                }
            }
        }

        private List<Result> DefaultSearch(string query)
        {
            // Check for invalid start of query i.e. symbols
            if (!char.IsLetterOrDigit(query[0]))
            {
                return ResultCreator.InvalidQuery();
            }

            var results = OneNoteApplication.FindPages(query)
                                            .Select(pg => _resultCreator.CreatePageResult(pg, query));

            return results.Any() ? results.ToList() : ResultCreator.NoMatchesFound();
        }

        private List<Result> TitleSearch(string query, IEnumerable<IOneNoteItem> currentCollection, IOneNoteItem? parent = null)
        {
            if (query.Length == Keywords.TitleSearch.Length && parent == null)
            {
                return ResultCreator.SingleResult($"Now searching by title.", null, IconProvider.Search);
            }

            List<int>? highlightData = null;
            int score = 0;

            var currentSearch = query[Keywords.TitleSearch.Length..];

            var results = currentCollection.Traverse(item => SettingsCheck(item) && FuzzySearch(item.Name, currentSearch, out highlightData, out score))
                                           .Select(item => _resultCreator.CreateOneNoteItemResult(item, false, highlightData, score))
                                           .ToList();

            if (!results.Any())
            {
                results = ResultCreator.NoMatchesFound();
            }

            return results;
        }

        private List<Result> RecentPages(string query)
        {
            int count = 10; // TODO: Ideally this should match PowerToysRunSettings.MaxResultsToShow
/*            var settingsUtils = new SettingsUtils();
            var generalSettings = settingsUtils.GetSettings<GeneralSettings>();*/
            if (query.Length > Keywords.RecentPages.Length && int.TryParse(query[Keywords.RecentPages.Length..], out int userChosenCount))
            {
                count = userChosenCount;
            }

            return OneNoteApplication.GetNotebooks()
                                     .GetPages()
                                     .Where(SettingsCheck)
                                     .OrderByDescending(pg => pg.LastModified)
                                     .Take(count)
                                     .Select(pg =>
                                     {
                                         Result result = _resultCreator.CreatePageResult(pg);
                                         result.SubTitle = $"{GetLastEdited(DateTime.Now - pg.LastModified)}\t{result.SubTitle}";
                                         result.IcoPath = IconProvider.RecentPage;
                                         return result;
                                     })
                                     .ToList();
        }

        public List<ContextMenuResult> LoadContextMenu(Result selectedResult)
        {
            var results = new List<ContextMenuResult>();
            if (selectedResult.ContextData is IOneNoteItem item)
            {
                results.Add(new ContextMenuResult
                {
                    PluginName = Assembly.GetExecutingAssembly().GetName().Name,
                    Title = "Open and sync",
                    Glyph = "\xE8A7",
                    FontFamily = "Segoe MDL2 Assets",
                    AcceleratorKey = Key.Enter,
                    AcceleratorModifiers = ModifierKeys.Shift,
                    Action = ResultCreator.ResultAction(() =>
                    {
                        item.Sync();
                        item.OpenItemInOneNote();
                        return true;
                    }),
                });

                if (item is not OneNotePage)
                {
                    results.Add(new ContextMenuResult
                    {
                        PluginName = Assembly.GetExecutingAssembly().GetName().Name,
                        Title = "Open in notebook explorer",
                        Glyph = "\xEC50",
                        FontFamily = "Segoe MDL2 Assets",
                        AcceleratorKey = Key.Enter,
                        AcceleratorModifiers = ModifierKeys.Control | ModifierKeys.Shift,
                        Action = ResultCreator.ResultAction(() =>
                        {
                            _context.API.ChangeQuery(selectedResult.QueryTextDisplay, true);
                            return false;
                        }),
                    });
                }
            }

            return results;
        }

        private static string GetLastEdited(TimeSpan diff)
        {
            string lastEdited = "Last edited ";
            if (PluralCheck(diff.TotalDays, "day", ref lastEdited)
             || PluralCheck(diff.TotalHours, "hour", ref lastEdited)
             || PluralCheck(diff.TotalMinutes, "min", ref lastEdited)
             || PluralCheck(diff.TotalSeconds, "sec", ref lastEdited))
            {
                return lastEdited;
            }
            else
            {
                return lastEdited += "Now.";
            }

            static bool PluralCheck(double totalTime, string timeType, ref string lastEdited)
            {
                var roundedTime = (int)Math.Round(totalTime);
                if (roundedTime > 0)
                {
                    string plural = roundedTime == 1 ? string.Empty : "s";
                    lastEdited += $"{roundedTime} {timeType}{plural} ago.";
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private bool FuzzySearch(string itemName, string search, out List<int> highlightData, out int score)
        {
            var matchResult = StringMatcher.FuzzySearch(search, itemName);
            highlightData = matchResult.MatchData;
            score = matchResult.Score;
            return matchResult.IsSearchPrecisionScoreMet();
        }

        private bool SettingsCheck(IOneNoteItem item)
        {
            bool success = true;
            if (!_settings.ShowEncryptedSections && item is OneNoteSection section)
            {
                success = !section.Encrypted;
            }

            if (!_settings.ShowRecycleBins && item.IsInRecycleBin())
            {
                success = false;
            }

            return success;
        }
    }
}
