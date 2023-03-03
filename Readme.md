Winamp Now Playing to File
===

This is a plugin for [Winamp](http://www.winamp.com/) that saves text information and album art for the currently playing song to files on your computer. You can customize where the files are saved, as well as the format of the text.

<!-- MarkdownTOC autolink="true" bracket="round" autoanchor="false" levels="1,2" -->

- [Problem](#problem)
- [Solution](#solution)
- [Installation](#installation)
- [Configuration](#configuration)
- [Integration](#integration)
- [Uninstallation](#uninstallation)

<!-- /MarkdownTOC -->

## Problem

I was broadcasting video game streams on [my Twitch.tv channel](https://twitch.tv/aldaviva), in which I also play music in the background. I wanted viewers to be able to tell which song I was playing at any given time in case they liked it and wanted to find it for themselves. I started using the [Advanced mIRC Integration Plug-In (AMIP)](http://amip.tools-for.net/wiki/), which is generally used for showing your Now Playing status in IRC using [mIRC](https://www.mirc.com/). It also lets you save the status to a text file, which I added as a Text Source in [OBS](https://obsproject.com/).

Unfortunately, AMIP only supports encoding the text using ANSI, OEM (DOS), FIDO, or KOI8 character encodings, none of which are UTF-8, which OBS requires. For example, `Jävla Sladdar` by [Etnoscope](https://www.discogs.com/Etnoscope-Way-Over-Deadline/master/284523) was being shown in the video stream as `J�vla Sladdar`, because even though AMIP was saving `ä` using ANSI (`0xe4`), OBS was decoding the file with UTF-8, so the character was not properly decoded. In UTF-8, `ä` is supposed to be encoded as `0xc3 0xa4` because `0xe4` is greater than `0x7f` (i.e. `0x34` requires more than 7 bits to represent), so it spills over into a second code unit (byte).

## Solution

I wrote my own Winamp plugin to save information about the currently playing song to a UTF-8 text file, and the song's album art to an image file.

## Installation

1. Ensure you have the [Microsoft .NET Framework 4.7.2](https://dotnet.microsoft.com/download/dotnet-framework) Runtime or later installed. This is included in Windows 10 version 1803 and later.
1. Download `WinampNowPlayingToFile.zip` from the [latest release](https://github.com/Aldaviva/WinampNowPlayingToFile/releases) (not the source code ZIP file).
1. Extract the archive to your Winamp installation directory.
    ```
    C:\Program Files (x86)\Winamp
    ├── plugins
    │   └── gen_WinampNowPlayingToFile.dll
    ├── WinampNowPlayingToFile.dll
    ├── Daniel15.Sharpamp.dll
    └── mustache-sharp.dll
    └── taglib-sharp.dll
    ```
1. Restart Winamp if it's already running.

## Configuration

Configuration of this plugin is performed in Winamp.

1. Start Winamp.
1. Go to Options › Preferences › Plug-ins › General Purpose.
1. Configure the Now Playing to File plugin.

Changes are saved to the registry in `HKCU\Software\WinampNowPlayingToFile`.

![configuration](.github/images/configuration.png)

### Text

By default, this plugin saves textual information about the currently playing song to `winamp_now_playing.txt` in your temporary directory (`%TEMP%`), and the file contains the track Artist, Title, and Album (if applicable), for example
```text
U2 - Exit - The Joshua Tree
```

To customize the text file location and contents, go to the plugin preferences in Winamp.

1. You can change the file contents by editing the **Text template** and inserting placeholders for the following song information.
	- `{{Album}}`
	- `{{Artist}}`
	- `{{Filename}}`
	- `{{Title}}`
	- `{{Year}}`
1. As you fill in the template, the **Text preview** will be updated to show how the currently-playing song would be rendered, or if no song is playing, an example song.
1. Advanced template logic can be added using [Handlebars expressions](https://handlebarsjs.com/), including the [built-in helpers](https://handlebarsjs.com/guide/builtin-helpers.html) like `{{#if}}`, `{{#else}}`, `{{/if}}`, and `{{#unless}}`. To insert a line break (CRLF), use `{{#newline}}`. The **Insert** button lets you add these helpers to your template, or you can type them in.
1. You can change where the file is written in your filesystem by selecting a different path for **Save text as**.

When Winamp is not playing a song, this text file will be truncated to 0 bytes.

### Album art

This plugin also copies the currently playing song's album art from the song metadata or folder. By default, it is copied to `%TEMP%\winamp_now_playing.png`.

Note that the file extension is not changed, even if the album art has a file type different from PNG, to make it easier to refer to this file from other programs like OBS without having to deal with multiple possible file extensions. This means that this file may be a JPEG with a `.png` file extension. Most programs, including OBS, can handle this case just fine, but it is a little silly looking.

You can customize the album art filename and path using **Save album art as** in the same plugin configuration dialog as the text file above.

When Winamp is playing a song with no album art, the image file will be replaced with a 1px × 1px opaque black PNG. To override this, save your desired image file as `emptyAlbumArt.png` in the Winamp installation directory.

When Winamp is not playing a song, the image file will be deleted.

## Integration

### OBS

1. Start playing a song in Winamp.
1. Create a new Text (GDI+) source in your OBS scene.
1. In the Properties for your text source, enable Read From File.
1. Select the text file created by this plugin (by default, `%TEMP%\winamp_now_playing.txt`).
1. Create a new Image source in your scene.
1. In the Properties for your image source, select the image file created by this plugin (by default, `%TEMP%\winamp_now_playing.png`).

## Uninstallation

1. In Winamp's Preferences, go to Plug-ins › General Purpose.
1. Select the Now Playing to File plugin, then click the Uninstall Selected Plug-In button.
1. Delete all the files you extracted to the Winamp installation directory when installing this plugin.
    ```
    C:\Program Files (x86)\Winamp
    ├── plugins
    │   └── gen_WinampNowPlayingToFile.dll
    ├── WinampNowPlayingToFile.dll
    ├── Daniel15.Sharpamp.dll
    ├── mustache-sharp.dll
    └── taglib-sharp.dll
    ```
1. Delete the song information files (by default, `winamp_now_playing.txt` and `winamp_now_playing.png` in `%TEMP%`).
