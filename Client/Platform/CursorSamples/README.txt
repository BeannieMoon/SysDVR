Custom mouse cursors for SysDVR
===============================

Images in this folder can be used as the mouse cursor. Pick one from
Settings > "Choose cursor image", or from the same button in the player overlay
menu while streaming. The chosen cursor is used everywhere in SysDVR, not just
over the video.

The quickest way to add images is to drag and drop them on the SysDVR window.
They are copied in the folder of the theme you are looking at and applied right
away.

Supported formats: png, bmp, gif, jpg, webp. Use png if you want transparency,
which you almost certainly do.

Themes
------
Subfolders of this folder show up in the picker as themes, so you can keep a set
per game:

    cursors\
        arrow.png                 <- the "General" theme
        crosshair_center.png
        Zelda\
            sword_center.png      <- the "Zelda" theme
            rupee.png
        Splatoon\
            squid.png

SysDVR has no way of knowing which game is running on the console, so the theme
is switched by hand in the picker. Switching takes two clicks and does not
interrupt the stream.

Size
----
Anything works, images bigger than 64 pixels are scaled down automatically when
the cursor is built, so an image straight out of a generator can be dropped in
as is. The file itself is left alone so the picker preview stays sharp.

Where the pointer actually is
-----------------------------
By default the top left corner of the image is the real pointer position, which
is what you want for arrow shaped cursors.

If the file name ends with _center (for example crosshair_center.png) the image
is centered on the pointer instead, which is what you want for crosshairs,
rings, dots or any symmetric picture.

The suffix is not shown in the picker, so "my_cat_center.png" is listed as
"my cat".


Using the same pointer outside SysDVR
-------------------------------------
SysDVR only controls the pointer while it is over the SysDVR window. Everywhere
else Windows decides the shape, which is why it becomes a text I-beam over text,
a hand over links and so on. To get the same pointer everywhere, the image has to
become a real Windows cursor file and be set as the system pointer.

The tools folder next to this file does that:

    tools\Convert-PngToCursor.ps1    turns a png into a .cur file
    tools\Apply-WindowsCursor.ps1    sets that .cur as the Windows pointer

    powershell -ExecutionPolicy Bypass -File tools\Convert-PngToCursor.ps1 -Path arrow.png
    powershell -ExecutionPolicy Bypass -File tools\Apply-WindowsCursor.ps1 -CursorPath arrow.cur -Apply

The apply script changes your user account only, needs no admin rights, backs up
your current setting to Documents first, and undoes everything with:

    powershell -ExecutionPolicy Bypass -File tools\Apply-WindowsCursor.ps1 -Revert -Apply

By default it replaces the normal pointer and the text I-beam, which are the two
shapes that swap around the most. Keep the .cur file somewhere permanent, Windows
reads it on every logon.
Included samples
----------------
arrow.png             - a plain white arrow with a black outline
crosshair_center.png  - high contrast crosshair with an open middle
ring_center.png       - a ring that frames what is under the pointer

These are just starting points, delete them if you have nicer ones.
