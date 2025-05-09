// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.CmdPal.Ext.OneNote.Components;
using Microsoft.CmdPal.Ext.OneNote.Properties;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Odotocodot.OneNote.Linq;

namespace Microsoft.CmdPal.Ext.OneNote.Helpers;

public class NewOneNoteItemHelper
{
    private static readonly CompositeFormat CreatePage = CompositeFormat.Parse(Resources.CreatePage);
    private static readonly CompositeFormat CreateSection = CompositeFormat.Parse(Resources.CreateSection);
    private static readonly CompositeFormat CreateSectionGroup = CompositeFormat.Parse(Resources.CreateSectionGroup);
    private static readonly CompositeFormat CreateNotebook = CompositeFormat.Parse(Resources.CreateNotebook);
    private static readonly CompositeFormat Path = CompositeFormat.Parse(Resources.Path);
    private static readonly CompositeFormat SectionNamesCannotContain = CompositeFormat.Parse(Resources.SectionNamesCannotContain);
    private static readonly CompositeFormat SectionGroupNamesCannotContain = CompositeFormat.Parse(Resources.SectionGroupNamesCannotContain);
    private static readonly CompositeFormat NotebookNamesCannotContain = CompositeFormat.Parse(Resources.NotebookNamesCannotContain);

    private static ListItem NewOneNoteItem(
        string name,
        IOneNoteItem parent,
        CompositeFormat titleFormat,
        ImmutableArray<char> invalidCharacters,
        CompositeFormat subTitleFormat,
        IconInfo icon,
        Action createItem)
    {
        name = name.Trim();
        var validTitle = !string.IsNullOrWhiteSpace(name) && !invalidCharacters.Any(name.Contains);

        string subTitle;
        if (validTitle)
        {
            var path = parent == null
                ? $"{OneNoteApplication.GetDefaultNotebookLocation()}{System.IO.Path.DirectorySeparatorChar}{name}"
                : $"{parent.RelativePath}{ResultHelper.PathSeparator}{name}";

            subTitle = string.Format(CultureInfo.CurrentCulture, Path, path);
        }
        else
        {
            subTitle = string.Format(CultureInfo.CurrentCulture, subTitleFormat, string.Join(' ', invalidCharacters));
        }

        return new ListItem(new AnonymousCommand(createItem))
        {
            Title = string.Format(CultureInfo.CurrentCulture, titleFormat, name),
            Subtitle = subTitle,
            Icon = icon,
        };
    }

    public static ListItem NewPage(string name, OneNoteSection section)
    {
        return NewOneNoteItem(name, section, CreatePage, [], null, IconProvider.NewPage, CreateAction);
        void CreateAction() => OneNoteApplication.CreatePage(section, name, true);
    }

    public static ListItem NewSection(string name, IOneNoteItem parent)
    {
        return NewOneNoteItem(name, parent, CreateSection, OneNoteApplication.InvalidSectionChars, SectionNamesCannotContain, IconProvider.NewSection, CreateAction);
        void CreateAction()
        {
            switch (parent)
            {
                case OneNoteNotebook notebook:
                    OneNoteApplication.CreateSection(notebook, name, true);
                    break;
                case OneNoteSectionGroup sectionGroup:
                    OneNoteApplication.CreateSection(sectionGroup, name, true);
                    break;
            }
        }
    }

    public static ListItem NewSectionGroup(string name, IOneNoteItem parent)
    {
        return NewOneNoteItem(name, parent, CreateSectionGroup, OneNoteApplication.InvalidSectionGroupChars, SectionGroupNamesCannotContain, IconProvider.NewSectionGroup, CreateAction);
        void CreateAction()
        {
            switch (parent)
            {
                case OneNoteNotebook notebook:
                    OneNoteApplication.CreateSectionGroup(notebook, name, true);
                    break;
                case OneNoteSectionGroup sectionGroup:
                    OneNoteApplication.CreateSectionGroup(sectionGroup, name, true);
                    break;
                default:
                    break;
            }
        }
    }

    public static ListItem NewNotebook(string name)
    {
        return NewOneNoteItem(name, null, CreateNotebook, OneNoteApplication.InvalidNotebookChars, NotebookNamesCannotContain, IconProvider.NewNotebook, CreateAction);
        void CreateAction() => OneNoteApplication.CreateNotebook(name, true);
    }
}
