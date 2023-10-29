// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Odotocodot.OneNote.Linq;

namespace Microsoft.PowerToys.Run.Plugin.OneNote.Components
{
    public class IconProvider
    {
        public static string NewPage { get; internal set; } = "Images/temp.svg";

        public static string NewSection { get; internal set; } = "Images/temp.svg";

        public static string NewSectionGroup { get; internal set; } = "Images/temp.svg";

        public static string NewNotebook { get; internal set; } = "Images/temp.svg";

        public static string Logo { get; internal set; } = "Images/temp.svg";

        public static string Warning { get; internal set; } = "Images/temp.svg";

        public static string Recent { get; internal set; } = "Images/temp.svg";

        public static string Sync { get; internal set; } = "Images/temp.svg";

        public static string Search { get; internal set; } = "Images/temp.svg";

        public static string RecentPage { get; internal set; } = "Images/temp.svg";

        public static string Notebook { get; internal set; } = string.Empty;

        internal static string GetIcon(IOneNoteItem item)
        {
            return "Images/temp.svg";
        }
    }
}
