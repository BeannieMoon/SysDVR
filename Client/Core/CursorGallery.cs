using SysDVR.Client.Platform;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SysDVR.Client.Core
{
    // The user can drop image files in the "cursors" folder next to the settings file to use them
    // as the mouse cursor. Images can be grouped in subfolders so you can keep a set per game,
    // those are called themes here and the file names are stored relative to the gallery folder.
    internal static class CursorGallery
    {
        // SDL_image can load more than this but these are the formats that make sense for a cursor
        static readonly string[] SupportedExtensions = [".png", ".bmp", ".gif", ".jpg", ".jpeg", ".webp"];

        // Images whose name ends with this are centered on the pointer instead of using the top left corner,
        // this is what you want for crosshair-like images.
        const string CenterHotspotSuffix = "_center";

        // Theme of the images that are not in a subfolder
        public const string DefaultTheme = "";

        // Bumped whenever the contents change so open views know they have to reload
        public static int Revision { get; private set; }

        // Image paths relative to the gallery folder, eg "zelda/sword.png"
        public static string[] FileNames { get; private set; } = [];

        // Subfolder names, the first one is always DefaultTheme
        public static string[] Themes { get; private set; } = [DefaultTheme];

        // Theme the UI is currently showing, new images are imported in there
        public static string CurrentTheme = DefaultTheme;

        public static bool HasImages => FileNames.Length > 0;

        public static string? FolderPath => Resources.CursorsFolder();

        // Feedback from the last drag and drop import, shown in the picker
        public static string LastImportMessage = "";

        // Scans the gallery folder and its subfolders
        public static void Refresh()
        {
            FileNames = [];
            Themes = [DefaultTheme];
            Revision++;

            var folder = FolderPath;
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
                return;

            try
            {
                var images = new List<string>();
                var themes = new List<string>() { DefaultTheme };

                foreach (var file in Directory.EnumerateFiles(folder))
                    if (IsSupported(file))
                        images.Add(Path.GetFileName(file));

                // Only one level of subfolders, deeper ones would make the picker confusing
                foreach (var dir in Directory.EnumerateDirectories(folder))
                {
                    var theme = Path.GetFileName(dir);
                    var added = false;

                    foreach (var file in Directory.EnumerateFiles(dir))
                    {
                        if (!IsSupported(file))
                            continue;

                        images.Add($"{theme}/{Path.GetFileName(file)}");
                        added = true;
                    }

                    if (added)
                        themes.Add(theme);
                }

                images.Sort(StringComparer.OrdinalIgnoreCase);
                FileNames = images.ToArray();
                Themes = themes.ToArray();

                if (!Themes.Contains(CurrentTheme))
                    CurrentTheme = DefaultTheme;
            }
            catch (Exception ex)
            {
                Program.DebugLog("Failed to scan the cursors folder: " + ex);
            }
        }

        public static string[] FileNamesForTheme(string theme) =>
            FileNames.Where(x => ThemeOf(x) == theme).ToArray();

        public static string ThemeOf(string fileName)
        {
            var slash = fileName.IndexOf('/');
            return slash < 0 ? DefaultTheme : fileName.Substring(0, slash);
        }

        // Copies an image the user dropped on the window in the gallery, returns the new file name
        public static string? Import(string sourcePath, string theme, out string message)
        {
            message = "";

            if (!IsSupported(sourcePath))
            {
                message = string.Format(Program.Strings.Settings.CursorImportNotAnImage, Path.GetFileName(sourcePath));
                return null;
            }

            var folder = FolderPath;
            if (string.IsNullOrWhiteSpace(folder))
                return null;

            try
            {
                var target = string.IsNullOrEmpty(theme) ? folder : Path.Combine(folder, theme);
                Directory.CreateDirectory(target);

                var name = Path.GetFileName(sourcePath);
                var destination = Path.Combine(target, name);

                // Don't silently overwrite an image the user already added
                var attempt = 1;
                while (File.Exists(destination) && !PathsMatch(destination, sourcePath))
                {
                    name = $"{Path.GetFileNameWithoutExtension(sourcePath)}_{++attempt}{Path.GetExtension(sourcePath)}";
                    destination = Path.Combine(target, name);
                }

                if (!PathsMatch(destination, sourcePath))
                    File.Copy(sourcePath, destination, false);

                Refresh();

                message = string.Format(Program.Strings.Settings.CursorImportDone, Path.GetFileNameWithoutExtension(name));
                return string.IsNullOrEmpty(theme) ? name : $"{theme}/{name}";
            }
            catch (Exception ex)
            {
                Program.DebugLog("Failed to import a cursor image: " + ex);
                message = string.Format(Program.Strings.Settings.CursorImportFailed, Path.GetFileName(sourcePath));
                return null;
            }
        }

        // Full path of a gallery image, null if it's not there anymore
        public static string? PathOf(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            var folder = FolderPath;
            if (string.IsNullOrWhiteSpace(folder))
                return null;

            // The option comes from a config file the user can edit, don't let it point outside the gallery
            var full = Path.GetFullPath(Path.Combine(folder, name));
            if (!full.StartsWith(Path.GetFullPath(folder), StringComparison.OrdinalIgnoreCase))
                return null;

            return File.Exists(full) ? full : null;
        }

        // Whether the image should be centered on the pointer, see CenterHotspotSuffix
        public static bool IsCentered(string? name) =>
            Path.GetFileNameWithoutExtension(name ?? "").EndsWith(CenterHotspotSuffix, StringComparison.OrdinalIgnoreCase);

        // Name to show in the UI
        public static string DisplayName(string name)
        {
            var pretty = Path.GetFileNameWithoutExtension(name);

            if (pretty.EndsWith(CenterHotspotSuffix, StringComparison.OrdinalIgnoreCase))
                pretty = pretty.Substring(0, pretty.Length - CenterHotspotSuffix.Length);

            return pretty.Replace('_', ' ');
        }

        static bool IsSupported(string path) =>
            SupportedExtensions.Contains(Path.GetExtension(path).ToLowerInvariant());

        static bool PathsMatch(string a, string b) =>
            string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);
    }
}
