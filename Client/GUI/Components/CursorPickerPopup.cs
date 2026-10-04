using ImGuiNET;
using SysDVR.Client.App;
using SysDVR.Client.Core;
using SysDVR.Client.Platform;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace SysDVR.Client.GUI.Components
{
    // Small popup to browse the images in the cursor gallery and pick one by looking at it.
    // Images can be dragged and dropped on the window to add them to the current theme.
    internal class CursorPickerPopup : IDisposable
    {
        class Thumbnail : IDisposable
        {
            // File name relative to the gallery folder
            public readonly string FileName;
            public readonly string Display;
            public readonly Image? Preview;

            public Thumbnail(ClientApp owner, string fileName)
            {
                FileName = fileName;
                Display = CursorGallery.DisplayName(fileName);

                try
                {
                    var path = CursorGallery.PathOf(fileName);
                    if (path is not null)
                        Preview = Image.FromFile(owner, path);
                }
                catch (Exception ex)
                {
                    // A broken file shouldn't take down the whole picker
                    Program.DebugLog($"Failed to load the preview for {fileName}: " + ex);
                }
            }

            public void Dispose() => Preview?.Dispose();
        }

        readonly StringTable.SettingsTable Strings = Program.Strings.Settings;

        public readonly Gui.Popup Popup = new(Program.Strings.Settings.CursorPopupTitle);

        readonly ClientApp Owner;

        // Called with the picked image, null means the system cursor
        readonly Action<string?> OnPicked;

        readonly List<Thumbnail> Thumbnails = new();
        int GalleryRevision = -1;
        int ThemeIndex;

        public CursorPickerPopup(ClientApp owner, Action<string?> onPicked)
        {
            Owner = owner;
            OnPicked = onPicked;
        }

        public void Reload()
        {
            CursorGallery.Refresh();
            LoadThumbnails();
        }

        void LoadThumbnails()
        {
            foreach (var t in Thumbnails)
                t.Dispose();

            Thumbnails.Clear();

            ThemeIndex = Math.Max(0, Array.IndexOf(CursorGallery.Themes, CursorGallery.CurrentTheme));

            foreach (var file in CursorGallery.FileNamesForTheme(CursorGallery.CurrentTheme))
                Thumbnails.Add(new Thumbnail(Owner, file));

            GalleryRevision = CursorGallery.Revision;
        }

        // Theme names as shown in the combo, the default one gets a friendly label
        string[] ThemeLabels()
        {
            var themes = CursorGallery.Themes;
            var labels = new string[themes.Length];

            for (int i = 0; i < themes.Length; i++)
                labels[i] = string.IsNullOrEmpty(themes[i]) ? Strings.CursorThemeDefault : themes[i];

            return labels;
        }

        public void Draw()
        {
            if (!Popup.Begin(ImGui.GetIO().DisplaySize * 0.8f))
                return;

            // Something was added by drag and drop while the popup was open
            if (GalleryRevision != CursorGallery.Revision)
                LoadThumbnails();

            var uiScale = ImGui.GetFontSize() / 16;

            if (CursorGallery.Themes.Length > 1)
            {
                ImGui.AlignTextToFramePadding();
                ImGui.Text(Strings.CursorThemeLabel);
                ImGui.SameLine();
                ImGui.SetNextItemWidth(200 * uiScale);

                var labels = ThemeLabels();
                if (ImGui.Combo("##cursortheme", ref ThemeIndex, labels, labels.Length))
                {
                    CursorGallery.CurrentTheme = CursorGallery.Themes[ThemeIndex];
                    LoadThumbnails();
                }

                ImGui.SameLine();
            }

            if (ImGui.Button(Strings.CursorSystemDefault))
            {
                OnPicked(null);
                Popup.RequestClose();
            }

            ImGui.NewLine();

            var listSize = ImGui.GetContentRegionAvail() - new Vector2(0, 90 * uiScale);
            if (listSize.Y < 100 * uiScale)
                listSize.Y = 100 * uiScale;

            ImGui.BeginChildFrame(ImGui.GetID("##cursorlist"), listSize, ImGuiWindowFlags.NavFlattened);

            if (Thumbnails.Count == 0)
            {
                ImGui.TextWrapped(Strings.CursorGalleryEmpty);
            }
            else
            {
                var cell = 72 * uiScale;
                var spacing = ImGui.GetStyle().ItemSpacing.X;
                var perRow = Math.Max(1, (int)((listSize.X - spacing) / (cell + spacing)));

                for (int i = 0; i < Thumbnails.Count; i++)
                {
                    var thumb = Thumbnails[i];
                    var selected = thumb.FileName == Program.Options.CursorImage;

                    if (selected)
                        ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonActive]);

                    bool clicked;
                    if (thumb.Preview is not null)
                    {
                        // Keep the aspect ratio of the image inside the square cell
                        var size = thumb.Preview.Width > thumb.Preview.Height
                            ? new Vector2(cell, thumb.Preview.ScaleHeight(cell))
                            : new Vector2(thumb.Preview.ScaleWidth(cell), cell);

                        clicked = ImGui.ImageButton($"##cursor{i}", thumb.Preview.Texture, size);
                    }
                    else
                    {
                        clicked = ImGui.Button($"{thumb.Display}##cursor{i}", new Vector2(cell, cell));
                    }

                    if (selected)
                        ImGui.PopStyleColor();

                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip(thumb.Display);

                    if (clicked)
                    {
                        OnPicked(thumb.FileName);
                        Popup.RequestClose();
                    }

                    if ((i + 1) % perRow != 0 && i != Thumbnails.Count - 1)
                        ImGui.SameLine();
                }
            }

            Gui.MakeWindowScrollable();
            ImGui.EndChildFrame();

            // Applied when the slider is released, rebuilding the cursor on every frame of a drag
            // would mean reloading the file from disk each time
            ImGui.AlignTextToFramePadding();
            ImGui.Text(Strings.CursorSizeLabel);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(220 * uiScale);
            ImGui.SliderInt("##cursorsize", ref Program.Options.CursorSize, SDLContext.MinCursorSize, SDLContext.MaxCursorSize, "%d px");
            if (ImGui.IsItemDeactivatedAfterEdit())
                OnPicked(Program.Options.CursorImage);

            ImGui.TextWrapped(Program.IsAndroid ? Strings.CursorGalleryHintNoDrop : Strings.CursorGalleryHint);

            if (!Program.IsAndroid && CursorGallery.FolderPath is string folder)
            {
                if (ImGui.Button(Strings.CursorImageFolderButton))
                {
                    try
                    {
                        Directory.CreateDirectory(string.IsNullOrEmpty(CursorGallery.CurrentTheme) ? folder : Path.Combine(folder, CursorGallery.CurrentTheme));
                        SystemUtil.OpenURL(folder);
                    }
                    catch (Exception e)
                    {
                        Program.DebugLog("Failed to open the cursors folder: " + e);
                    }
                }

                ImGui.SameLine();
                if (ImGui.Button(Strings.CursorImageRefreshButton))
                    Reload();

                ImGui.SameLine();
            }

            if (ImGui.Button(Program.Strings.General.PopupCloseButton))
                Popup.RequestClose();

            ImGui.EndPopup();
        }

        public void Dispose()
        {
            foreach (var t in Thumbnails)
                t.Dispose();

            Thumbnails.Clear();
        }
    }
}
