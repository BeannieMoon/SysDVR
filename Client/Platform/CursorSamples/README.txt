Custom mouse cursors for SysDVR
===============================

Drop image files in this folder and they show up in the cursor list, both in
Settings and in the player overlay menu. The selected image is used while the
mouse is over the video, the menus keep the normal pointer.

Supported formats: png, bmp, gif, jpg, webp. Use png if you want transparency,
which you almost certainly do.

Size
----
Around 32 to 64 pixels works best. Windows scales anything bigger to the system
cursor size and very large images may be refused outright, so if an image does
not show up, make it smaller.

Where the pointer actually is
-----------------------------
By default the top left corner of the image is the real pointer position, which
is what you want for arrow shaped cursors.

If the file name ends with _center (for example crosshair_center.png) the image
is centered on the pointer instead, which is what you want for crosshairs,
rings, dots or any symmetric picture.

The suffix is not shown in the UI, so "my_cat_center.png" is listed as "my cat".

Included samples
----------------
arrow.png             - a plain white arrow with a black outline
crosshair_center.png  - high contrast crosshair with an open middle
ring_center.png       - a ring that frames what is under the pointer

These are just starting points, delete them if you have nicer ones.
