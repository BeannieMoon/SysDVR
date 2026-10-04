> ## This is a personal fork. It is not SysDVR.
>
> **All credit for SysDVR goes to [exelix11](https://github.com/exelix11) and the people who
> contributed to it.** They wrote the sysmodule, the protocol and the client. I wrote none of it.
>
> This fork exists for one reason: I wanted to see my mouse cursor while streaming. It adds
> exactly two things to the client, nothing else:
>
> 1. An option to never hide the mouse cursor, including in full screen.
> 2. Custom cursor images, picked from a gallery folder, grouped in themes.
>
> These additions were written with an AI coding assistant, for my own use. They have had no
> review from upstream and are not tested beyond my own machine. Judge them accordingly.
>
> **Do not take this fork's problems to exelix11.** If anything here misbehaves it is my doing,
> not theirs. For real SysDVR support, releases and issues, go to
> **[exelix11/SysDVR](https://github.com/exelix11/SysDVR)** and use the official builds.
>
> The console-side sysmodule is untouched here, only the PC client differs. Same GPLv2 license
> as upstream.
>
> Everything below this line is the original project's readme, kept as it was.

---

# SysDVR
[![Discord](https://img.shields.io/discord/643436008452521984.svg?logo=discord&logoColor=white&label=Discord&color=7289DA
)](https://discord.gg/rqU5Tf8)
[![Latest release](https://img.shields.io/github/v/release/exelix11/SysDVR)](https://github.com/exelix11/SysDVR/releases)
[![Downloads](https://img.shields.io/github/downloads/exelix11/SysDVR/total)](https://github.com/exelix11/SysDVR/releases)
[![ko-fi](https://img.shields.io/badge/supporting-ko--fi-f96854)](https://ko-fi.com/exelix11)

This is a sysmodule that allows capturing the running game output to a pc via USB or network connection.

<p align="center">
  <img src="https://raw.githubusercontent.com/exelix11/SysDVR/master/.github/images/Screenshot.jpg" width="50%">
</p>

# Features
- Cross platform, can stream to Windows, Mac, Linux and Android.
- Stream via USB or Wifi.
- **Video quality is fixed to 720p @ 30fps with h264 compression, this is a hardware limit**.
- Audio quality is fixed to 16bit PCM @ 48kHz stereo. Not compressed.
- Very low latency with an optimal setup, most games are playable !

# Limitations
- **Only works on games that have video recording enabled** (aka you can long-press the capture button to save a video)
   - [There is now a workaround to support most games](https://github.com/exelix11/dvr-patches/), as it may cause issues it's hosted on a different repo and must be installed manually.
- Only captures game output. System UI, home menu and homebrews running as applet won't be captured.
- Stream quality depends heavily on the environment, bad usb wires or low wifi signal can affect it significantly.
- **USB streaming is not available when docked**
- Requires at least firmware 6.0.0

Clearly with these limitations **this sysmodule doesn't fully replace a capture card**.

# Usage
The guide has been moved to the wiki, you can find it [here](https://github.com/exelix11/SysDVR/wiki)

**If you have issues make sure to read the the [common issues page](https://github.com/exelix11/SysDVR/wiki/Troubleshooting). If you need help you can either ask on discord or open an issue with the correct template.**

## Donations
If you like my work and wish to support me you can donate on [ko-fi](https://ko-fi.com/exelix11)

## Credits
- Everyone from libnx and the people who reversed grc:d and wrote the service wrapper, mission2000 in particular for the suggestion on how to fix audio lag.
- [mtp-server-nx](https://github.com/retronx-team/mtp-server-nx) for their usb implementation
- [RTSPSharp](https://github.com/ngraziano/SharpRTSP) for the C# RTSP library
- Bonta on discord for a lot of help implementing a custom RTSP server
- [Xerpi](https://github.com/xerpi) for a lot of help while working on the UVC branch
