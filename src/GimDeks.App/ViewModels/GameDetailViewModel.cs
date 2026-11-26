using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GimDeks.Core.Interfaces;
using GimDeks.Core.Models;

namespace GimDeks.App.ViewModels;

public sealed class NoteView
{
    public required GameNote Note { get; init; }
    public string TimeText => Note.DibuatPada.Length >= 16 ? Note.DibuatPada[..16].Replace('T', ' ') : Note.DibuatPada;
}

public sealed class SessionView
{
    public required PlaySession Session { get; init; }
    public string DurationText
    {
        get
        {
            var seconds = Session.DurasiDetik;
            var hours = seconds / 3600;
            var minutes = (seconds % 3600) / 60;
            return hours > 0 ? $"{hours}h {minutes}m" : $"{minutes}m";
        }
    }
    public string StartText => Session.Mulai.Length >= 16 ? Session.Mulai[..16].Replace('T', ' ') : Session.Mulai;
}

public sealed class ImageItem
{
    public required GameImage Image { get; init; }
    public required string Path { get; init; }
}

public partial class GameDetailViewModel : ObservableObject
{
    private readonly IGameRepository _games;
    private readonly IGameNoteRepository _notes;
    private readonly IGameImageRepository _images;
    private readonly IPlaySessionRepository _sessions;
    private readonly Services.SessionTrackerService _tracker;

    public GameDetailViewModel(Game game, IGameRepository games, IGameNoteRepository notes,
        IGameImageRepository images, IPlaySessionRepository sessions, Services.SessionTrackerService tracker)
    {
        Game = game;
        _games = games;
        _notes = notes;
        _images = images;
        _sessions = sessions;
        _tracker = tracker;
    }

    public Game Game { get; }
    public ObservableCollection<NoteView> Notes { get; } = new();
    public ObservableCollection<SessionView> Sessions { get; } = new();
    public ObservableCollection<ImageItem> Images { get; } = new();

    [ObservableProperty]
    private string _selectedImagePath = "";

    [ObservableProperty]
    private string _newNote = "";

    [ObservableProperty]
    private string _statusText = "";

    [ObservableProperty]
    private bool _isBusy;

    public string HeaderText => Services.Loc.Format("Ui_Detail_Header", Game.Nama);

    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            Notes.Clear();
            foreach (var note in await _notes.GetByGameAsync(Game.Id))
            {
                Notes.Add(new NoteView { Note = note });
            }

            Sessions.Clear();
            foreach (var session in await _sessions.GetByGameAsync(Game.Id))
            {
                Sessions.Add(new SessionView { Session = session });
            }

            Images.Clear();
            var gallery = Services.CoverFileService.GalleryDirectory(Game.Id);
            foreach (var image in await _images.GetByGameAsync(Game.Id))
            {
                var path = Path.Combine(gallery, image.NamaFile);
                if (File.Exists(path))
                {
                    Images.Add(new ImageItem { Image = image, Path = path });
                }
            }
            SelectedImagePath = Images.FirstOrDefault()?.Path ?? "";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AddNoteAsync()
    {
        var text = NewNote.Trim();
        if (text.Length == 0)
        {
            return;
        }
        await _notes.AddAsync(Game.Id, text);
        NewNote = "";
        StatusText = Services.Loc.Get("Ui_Detail_NoteAdded");
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteNoteAsync(NoteView? note)
    {
        if (note is null)
        {
            return;
        }
        await _notes.DeleteAsync(note.Note.Id);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task AddImageAsync()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Images (*.png;*.jpg;*.jpeg;*.webp)|*.png;*.jpg;*.jpeg;*.webp",
            Title = Services.Loc.Get("Ui_Detail_AddImage"),
        };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var directory = Services.CoverFileService.GalleryDirectory(Game.Id);
            Directory.CreateDirectory(directory);
            var name = Guid.NewGuid().ToString("N") + Path.GetExtension(dialog.FileName).ToLowerInvariant();
            File.Copy(dialog.FileName, Path.Combine(directory, name), overwrite: true);
            await _images.AddAsync(Game.Id, name);
            await LoadAsync();
        }
        catch (Exception exception)
        {
            Services.CrashLog.Write(exception);
            StatusText = Services.Loc.Get("Ui_Detail_ImageFailed");
        }
    }

    [RelayCommand]
    private async Task DeleteImageAsync(ImageItem? item)
    {
        if (item is null)
        {
            return;
        }
        await _images.DeleteAsync(item.Image.Id);
        try { if (File.Exists(item.Path)) File.Delete(item.Path); } catch { /* best effort */ }
        await LoadAsync();
    }

    [RelayCommand]
    private async Task LaunchAsync()
    {
        if (_tracker.IsRunning(Game.Id))
        {
            StatusText = Services.Loc.Get("Ui_GameList_AlreadyRunning");
            return;
        }
        if (!await _tracker.LaunchAsync(Game))
        {
            StatusText = Services.Loc.Get("Ui_GameList_NoExe");
            return;
        }
        StatusText = Services.Loc.Format("Ui_GameList_Launched", Game.Nama);
        await LoadAsync();
    }

    [RelayCommand]
    private void OpenFolder()
    {
        var path = Game.FolderPath;
        if (string.IsNullOrWhiteSpace(path) || !System.IO.Directory.Exists(path))
        {
            StatusText = Services.Loc.Get("Ui_Detail_NoFolder");
            return;
        }
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = "\"" + path + "\"",
                UseShellExecute = false,
            });
        }
        catch (Exception exception)
        {
            Services.CrashLog.Write(exception);
            StatusText = Services.Loc.Get("Ui_Detail_FolderFailed");
        }
    }
}
