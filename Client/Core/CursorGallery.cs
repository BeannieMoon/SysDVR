using SysDVR.Client.Platform;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SysDVR.Client.Core
{
    // The user can drop image files in the "cursors" folder next to the settings file to use them
    // as the mouse cursor in the player, this class keeps track of what's available in there.
    internal static class CursorGallery
    {
        // SDL_image can load more than this but these are the formats that make sense for a cursor
        static readonly string[] SupportedExtensions = [".png", ".bmp", ".gif", ".jpg", ".jpeg", ".webp"];

        // Images whose name ends with this are centered on the pointer instead of using the top left corner,
        // this is what you want for crosshair-like images.
        const string CenterHotspotSuffix = "_center";

        // File names (without the directory part) of the images found in the gallery folder
        public static string[] FileNames { get; private set; } = [];

        public static bool HasImages => FileNames.Length > 0;

        public static string? FolderPath => Resources.CursorsFolder();

        // Scans the gallery folder, this is cheap enough to be called whenever the settings page is opened
        public static void Refresh()
        {
            FileNames = [];

            var folder = FolderPath;
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
                return;

            try
            {
                var found = new List<string>();
                foreach (var file in Directory.EnumerateFiles(folder))
                {
                    if (SupportedExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
                        found.Add(Path.GetFileName(file));
                }

                found.Sort(StringComparer.OrdinalIgnoreCase);
                FileNames = found.ToArray();
            }
            catch (Exception ex)
            {
                Program.DebugLog("Failed to scan the cursors folder: " + ex);
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

            // Only accept plain file names, the option comes from a config file the user can edit
            if (name != Path.GetFileName(name))
                return null;

            var path = Path.Combine(folder, name);
            return File.Exists(path) ? path : null;
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
    }
}
