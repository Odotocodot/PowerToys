// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Odotocodot.OneNote.Linq;
using Wox.Infrastructure;
using Wox.Plugin;

namespace Microsoft.PowerToys.Run.Plugin.OneNote.Components
{
    public class ResultCreator
    {
        private readonly PluginInitContext _context;
        private readonly OneNoteSettings _settings;

        private const string PathSeparator = " > ";
        private static readonly string _oldSeparator = OneNoteApplication.RelativePathSeparator.ToString();

        internal ResultCreator(PluginInitContext context, OneNoteSettings settings)
        {
            _settings = settings;
            _context = context;
        }

        private static string GetNicePath(IOneNoteItem item, string separator = PathSeparator)
        {
            return item.RelativePath.Replace(_oldSeparator, separator);
        }

        private string GetTitle(IOneNoteItem item, List<int>? highlightData)
        {
            string title = item.Name;
            if (item.IsUnread && _settings.ShowUnreadItems)
            {
                string unread = "\u2022  ";
                title = title.Insert(0, unread);

                if (highlightData != null)
                {
                    for (int i = 0; i < highlightData.Count; i++)
                    {
                        highlightData[i] += unread.Length;
                    }
                }
            }

            return title;
        }

        internal Result CreatePageResult(OneNotePage page, string? query = null)
        {
            return CreateOneNoteItemResult(page, false, string.IsNullOrWhiteSpace(query) ? null : StringMatcher.FuzzySearch(query, page.Name).MatchData);
        }

        internal Result CreateOneNoteItemResult(IOneNoteItem item, bool actionIsAutoComplete, List<int>? highlightData = null, int score = 0)
        {
            string title = GetTitle(item, highlightData);
            string toolTip = string.Empty;
            string subTitle = GetNicePath(item);
            string queryTextDisplay = $"{Keywords.NotebookExplorer}{GetNicePath(item, Keywords.NotebookExplorerSeparator)}";

            switch (item)
            {
                case OneNoteNotebook notebook:
                    toolTip =
                        $"Last Modified:\t{notebook.LastModified:F}\n" +
                        $"Sections:\t\t{notebook.Sections.Count()}\n" +
                        $"Sections Groups:\t{notebook.SectionGroups.Count()}";

                    subTitle = string.Empty;
                    queryTextDisplay += Keywords.NotebookExplorerSeparator;
                    break;
                case OneNoteSectionGroup sectionGroup:
                    toolTip =
                        $"Path:\t\t{subTitle}\n" +
                        $"Last Modified:\t{sectionGroup.LastModified:F}\n" +
                        $"Sections:\t\t{sectionGroup.Sections.Count()}\n" +
                        $"Sections Groups:\t{sectionGroup.SectionGroups.Count()}";

                    queryTextDisplay += Keywords.NotebookExplorerSeparator;
                    break;
                case OneNoteSection section:
                    if (section.Encrypted)
                    {
                        // TODO potential replace with glyphs if supported
                        title += " [Encrypted]";
                        if (section.Locked)
                        {
                            title += "[Locked]";
                        }
                        else
                        {
                            title += "[Unlocked]";
                        }
                    }

                    toolTip =
                        $"Path:\t\t{subTitle}\n" +
                        $"Last Modified:\t{section.LastModified}\n" +
                        $"Pages:\t\t{section.Pages.Count()}";

                    queryTextDisplay += Keywords.NotebookExplorerSeparator;
                    break;
                case OneNotePage page:
                    if (!actionIsAutoComplete)
                    {
                        queryTextDisplay = string.Empty;
                    }

                    actionIsAutoComplete = false;
                    subTitle = subTitle.Remove(subTitle.Length - (page.Name.Length + PathSeparator.Length));
                    toolTip =
                        $"Path:\t\t {subTitle} \n" +
                        $"Created:\t\t{page.Created:F}\n" +
                        $"Last Modified:\t{page.LastModified:F}";
                    break;
            }

            return new Result
            {
                Title = title,
                ToolTipData = new ToolTipData(item.Name, toolTip),
                TitleHighlightData = highlightData,
                QueryTextDisplay = queryTextDisplay,
                SubTitle = subTitle,
                Score = score,
                IcoPath = IconProvider.GetIcon(item),
                ContextData = item,
                Action = ResultAction(() =>
                {
                    if (actionIsAutoComplete)
                    {
                        _context.API.ChangeQuery($"{_context.CurrentPluginMetadata.ActionKeyword} {queryTextDisplay}", true);
                        return false;
                    }

                    item.Sync();
                    item.OpenItemInOneNote();
                    return true;
                }),
            };
        }

        internal static Result CreateNewPageResult(string pageTitle, OneNoteSection section)
        {
            pageTitle = pageTitle.Trim();
            return new Result
            {
                Title = $"Create page: \"{pageTitle}\"",
                SubTitle = $"Path: {GetNicePath(section)}{PathSeparator}{pageTitle}",
                IcoPath = IconProvider.NewPage,
                Action = ResultAction(() =>
                {
                    OneNoteApplication.CreatePage(section, pageTitle, true);
                    return true;
                }),
            };
        }

        internal Result CreateNewSectionResult(string sectionTitle, IOneNoteItem parent)
        {
            sectionTitle = sectionTitle.Trim();
            bool validTitle = OneNoteApplication.IsSectionNameValid(sectionTitle);

            return new Result
            {
                Title = $"Create section: \"{sectionTitle}\"",
                SubTitle = validTitle
                        ? $"Path: {GetNicePath(parent)}{PathSeparator}{sectionTitle}"
                        : $"Section names cannot contain: {string.Join(' ', OneNoteApplication.InvalidSectionChars)}",
                IcoPath = IconProvider.NewSection,
                Action = ResultAction(() =>
                {
                    if (!validTitle)
                    {
                        return false;
                    }

                    switch (parent) { case OneNoteNotebook notebook: OneNoteApplication.CreateSection(notebook, sectionTitle, true); break; case OneNoteSectionGroup sectionGroup: OneNoteApplication.CreateSection(sectionGroup, sectionTitle, true); break; default: break; }
                    _context.API.ChangeQuery(_context.CurrentPluginMetadata.ActionKeyword, true);
                    return true;
                }),
            };
        }

        internal Result CreateNewSectionGroupResult(string sectionGroupTitle, IOneNoteItem parent)
        {
            sectionGroupTitle = sectionGroupTitle.Trim();
            bool validTitle = OneNoteApplication.IsSectionGroupNameValid(sectionGroupTitle);

            return new Result
            {
                Title = $"Create section group: \"{sectionGroupTitle}\"",
                SubTitle = validTitle
                    ? $"Path: {GetNicePath(parent)}{PathSeparator}{sectionGroupTitle}"
                    : $"Section group names cannot contain: {string.Join(' ', OneNoteApplication.InvalidSectionGroupChars)}",
                IcoPath = IconProvider.NewSectionGroup,
                Action = ResultAction(() =>
                {
                    if (!validTitle)
                    {
                        return false;
                    }

                    switch (parent) { case OneNoteNotebook notebook: OneNoteApplication.CreateSectionGroup(notebook, sectionGroupTitle, true); break; case OneNoteSectionGroup sectionGroup: OneNoteApplication.CreateSectionGroup(sectionGroup, sectionGroupTitle, true); break; default: break; }
                    _context.API.ChangeQuery(_context.CurrentPluginMetadata.ActionKeyword, true);
                    return true;
                }),
            };
        }

        internal Result CreateNewNotebookResult(string notebookTitle)
        {
            notebookTitle = notebookTitle.Trim();
            bool validTitle = OneNoteApplication.IsNotebookNameValid(notebookTitle);

            return new Result
            {
                Title = $"Create notebook: \"{notebookTitle}\"",
                SubTitle = validTitle
                    ? $"Location: {OneNoteApplication.GetDefaultNotebookLocation()}"
                    : $"Notebook names cannot contain: {string.Join(' ', OneNoteApplication.InvalidNotebookChars)}",
                IcoPath = IconProvider.NewNotebook,
                Action = ResultAction(() =>
                {
                    if (!validTitle)
                    {
                        return false;
                    }

                    OneNoteApplication.CreateNotebook(notebookTitle, true);
                    _context.API.ChangeQuery(_context.CurrentPluginMetadata.ActionKeyword, true);
                    return true;
                }),
            };
        }

        // TODO Localize
        internal static List<Result> NoMatchesFound() => SingleResult(
            "No matches found",
            "Try searching something else, or syncing your notebooks.",
            IconProvider.Logo);

        internal static List<Result> InvalidQuery() => SingleResult(
            "Invalid query",
            "The first character of the search must be a letter or a digit",
            IconProvider.Warning);

        // TODO: Context menu show be links to download OneNote -> https://apps.microsoft.com/store/detail/XPFFZHVGQWWLHB?ocid=pdpshare
        internal static List<Result> OneNoteNotInstalled() => SingleResult(
            "OneNote is not installed",
            "Please install OneNote to use this plugin",
            IconProvider.Warning);

        internal static List<Result> SingleResult(string title, string? subTitle, string iconPath)
        {
            return new List<Result>
            {
                new Result
                {
                    Title = title,
                    SubTitle = subTitle,
                    IcoPath = iconPath,
                },
            };
        }

        internal static Func<ActionContext, bool> ResultAction(Func<bool> func)
        {
            return _ =>
            {
                bool result = func();

                // Closing the Run window, so can release the COM Object
                if (result)
                {
                    Task.Run(OneNoteApplication.ReleaseComObject);
                }

                return result;
            };
        }
    }
}
