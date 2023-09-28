﻿#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Mustache;
using WinampNowPlayingToFile.Facade;
using WinampNowPlayingToFile.Settings;

namespace WinampNowPlayingToFile.Presentation;

public partial class SettingsDialog: Form {

    private readonly ISettings            settings;
    private readonly WinampControllerImpl winampController;

    private static readonly FormatCompiler TEMPLATE_COMPILER = new();

    private static readonly Song EXAMPLE_SONG = new() {
        Album    = "The Joshua Tree",
        Artist   = "U2",
        Filename = "C:\\Exit.mp3",
        Title    = "Exit",
        Year     = 1987
    };

    private static readonly IReadOnlyDictionary<string, string> EXAMPLE_SONG_EXTRA_METADATA = new ReadOnlyDictionary<string, string>(new Dictionary<string, string> {
        { "albumartist", "U2" },
        { "bitrate", "320" },
        { "bpm", "123" },
        { "category", "Rock" },
        { "composer", "U2" },
        { "disc", "1" },
        { "family", "MPEG Layer 3 Audio File" },
        { "gain", "+0.92 dB" },
        { "genre", "Rock" },
        { "key", "E minor" },
        { "length", "4:11" },
        { "lossless", "0" },
        { "media", "LP" },
        { "producer", "Brian Eno, Daniel Lanois" },
        { "publisher", "Island Records" },
        { "rating", "2" },
        { "replaygain_album_gain", "-3.03 dB" },
        { "replaygain_album_peak", "1.022630334" },
        { "replaygain_track_gain", "-0.77 dB" },
        { "replaygain_track_peak", "1.006227493" },
        { "stereo", "1" },
        { "track", "10" },
        { "type", "0" },
        { "vbr", "0" }
    });

    public SettingsDialog(ISettings settings, WinampControllerImpl winampController) {
        this.settings         = settings;
        this.winampController = winampController;
        InitializeComponent();

        // Make buttons have animated state transitions, like every other program in the OS
        // https://stackoverflow.com/q/53456865/979493
        foreach (ButtonBase flatStylableControl in Controls.OfType<ButtonBase>()) {
            flatStylableControl.FlatStyle = FlatStyle.System;
        }
    }

    private void SettingsDialog_Load(object sender, EventArgs e) {
        textFilenameEditor.InitialDirectory = Path.GetDirectoryName(settings.textFilename);
        textFilenameEditor.FileName         = settings.textFilename;

        albumArtFilenameEditor.InitialDirectory = Path.GetDirectoryName(settings.albumArtFilename);
        albumArtFilenameEditor.FileName         = settings.albumArtFilename;

        textFilename.Text     = settings.textFilename;
        albumArtFilename.Text = settings.albumArtFilename;

        templateEditor.Text = settings.textTemplate;
        templateEditor.Select(templateEditor.TextLength, 0);

        winampController.songChanged += delegate { renderPreview(); };

        applyButton.Enabled = false;
    }

    private void onTextFileBrowseButtonClick(object sender, EventArgs e) {
        onBrowseButtonClick(textFilenameEditor, textFilename);
    }

    private void onAlbumArtBrowseButtonClick(object sender, EventArgs e) {
        onBrowseButtonClick(albumArtFilenameEditor, albumArtFilename);
    }

    private static void onBrowseButtonClick(SaveFileDialog filenameEditor, TextBox filenameTextBox) {
        try {
            filenameEditor.FileName = Path.GetFileName(filenameTextBox.Text) ?? "";
        } catch (ArgumentException) {
            filenameEditor.FileName = "";
        }

        try {
            filenameEditor.InitialDirectory = Path.GetDirectoryName(filenameTextBox.Text) ?? "";
        } catch (ArgumentException) {
            filenameEditor.InitialDirectory = "";
        }

        filenameEditor.ShowDialog();
        filenameTextBox.Text = filenameEditor.FileName;
    }

    private void onCancel(object sender, EventArgs e) {
        Close();
    }

    private void onTemplateChange(object sender, EventArgs e) {
        renderPreview();
        onFormDirty();
    }

    private void renderPreview() {
        Song previewSong = isSongPlaying() ? winampController.currentSong : EXAMPLE_SONG;

        try {
            templatePreview.Text = compileTemplate().Render(previewSong);
        } catch (KeyNotFoundException e) {
            templatePreview.Text = $"Template key not found: {e.Message}";
        } catch (FormatException e) {
            templatePreview.Text = $"Template format error: {e.Message}";
        }
    }

    private bool isSongPlaying() => !string.IsNullOrEmpty(winampController.currentSong.Title);

    private Generator compileTemplate() {
        Generator generator = TEMPLATE_COMPILER.Compile(templateEditor.Text);
        generator.KeyNotFound += (_, args) => {
            args.Substitute = isSongPlaying()
                ? winampController.fetchMetadataFieldValue(args.Key)
                : EXAMPLE_SONG_EXTRA_METADATA.TryGetValue(args.Key.ToLowerInvariant(), out string? value)
                    ? value
                    : string.Empty;

            args.Handled = true;
        };
        return generator;
    }

    private void showTemplateMenu(object sender, EventArgs e) {
        insertTemplatePlaceholderMenu.Show(templateInsertButton, new Point(0, templateInsertButton.Height));
    }

    private void onTemplateMenuSelection(object sender, ToolStripItemClickedEventArgs e) {
        if (e.ClickedItem == helpToolStripMenuItem) {
            Process.Start("https://handlebarsjs.com/guide/#language-features");
        } else {
            string textToInsert;
            if (e.ClickedItem == newLineToolStripMenuItem) {
                textToInsert = "#newline";
            } else if (e.ClickedItem == ifToolStripMenuItem) {
                textToInsert = "#if Album}} - {{Album}}{{/if";
            } else if (e.ClickedItem == ifElseToolStripMenuItem) {
                textToInsert = "#if Album}} - {{Album}}{{#else}} - (no album){{/if";
            } else if (e.ClickedItem.Tag is string tag && !string.IsNullOrWhiteSpace(tag)) {
                textToInsert = tag;
            } else {
                textToInsert = e.ClickedItem.Text;
            }

            string placeholder      = $"{{{{{textToInsert}}}}}";
            string originalTemplate = templateEditor.Text;
            int    selectionStart   = templateEditor.SelectionStart;
            int    selectionEnd     = selectionStart + templateEditor.SelectionLength;

            StringBuilder newTemplate = new();
            newTemplate.Append(originalTemplate.Substring(0, selectionStart));
            newTemplate.Append(placeholder);
            newTemplate.Append(originalTemplate.Substring(selectionEnd));

            templateEditor.Text            = newTemplate.ToString();
            templateEditor.SelectionLength = 0;
            templateEditor.SelectionStart  = selectionStart + placeholder.Length;
            templateEditor.Focus();
        }
    }

    private void onClickOk(object sender, EventArgs args) {
        try {
            save();
            Close();
        } catch (Exception e) when (e is FormatException or KeyNotFoundException) {
            //leave form open, with invalid inputs unsaved
        }
    }

    private void onClickApply(object sender, EventArgs args) {
        try {
            save();
        } catch (Exception e) when (e is FormatException or KeyNotFoundException) {
            //leave form open, with invalid inputs unsaved
        }
    }

    private void save() {
        try {
            compileTemplate().Render(EXAMPLE_SONG);

            settings.textFilename     = textFilename.Text;
            settings.albumArtFilename = albumArtFilename.Text;
            settings.textTemplate     = templateEditor.Text;
            settings.save();

            applyButton.Enabled = false;
        } catch (FormatException e) {
            MessageBox.Show($"Invalid template:\n\n{e.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            throw;
        }
    }

    private void onFormDirty() {
        applyButton.Enabled = true;
    }

    private void onSubmitFilename(object sender, CancelEventArgs e) {
        onFormDirty();
    }

    private void onFilenameChange(object sender, EventArgs e) {
        onFormDirty();
    }

}