using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using AdonisUI.Controls;
using DuplicateRemoval.dto;
using DuplicateRemoval.model;
using ImageMagick;
using K4os.Hash.xxHash;
using WindowsAPICodePack.Dialogs;
using WpfAnimatedGif;
using Directory = Alphaleonis.Win32.Filesystem.Directory;
using File = Alphaleonis.Win32.Filesystem.File;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;
using Path = Alphaleonis.Win32.Filesystem.Path;

namespace DuplicateRemoval;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : AdonisWindow
{
    private const int hashBufferSize = 4096;
    private readonly MainWindowModel _mainWindowModel;
    private List<FileEntry> _currentDuplicates;

    public MainWindow()
    {
        InitializeComponent();
        ImageBehavior.SetRepeatBehavior(imgContent, RepeatBehavior.Forever);

        _mainWindowModel = new MainWindowModel();
        DataContext = _mainWindowModel;
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        CommonOpenFileDialog cofd = new CommonOpenFileDialog { IsFolderPicker = true, Title = "Select a folder to scan", InitialDirectory = "C:\\" };
        if (cofd.ShowDialog() == CommonFileDialogResult.Ok)
        {
            _mainWindowModel.FolderPath = cofd.FileName;
        }
    }

    private void Scan_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_mainWindowModel.FolderPath))
        {
            return;
        }

        _mainWindowModel.FirstInstances.Clear();
        _mainWindowModel.Duplicates.Clear();

        ConcurrentBag<FileEntry> entries = new ConcurrentBag<FileEntry>();

        Parallel.ForEach(Directory.EnumerateFiles(_mainWindowModel.FolderPath, "*", System.IO.SearchOption.AllDirectories), file =>
        {
            FileInfo fileInfo = new FileInfo(file);
            ulong hash;

            using (FileStream fileStream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read))
            {
                XXH64 hasher = new XXH64(0);

                byte[] buffer = new byte[hashBufferSize];
                int bytesRead;
                while ((bytesRead = fileStream.Read(buffer, 0, hashBufferSize)) > 0)
                {
                    hasher.Update(new ReadOnlySpan<byte>(buffer, 0, bytesRead));
                }

                hash = hasher.Digest();
            }

            FileEntry entry = new FileEntry(fileInfo.FullName, fileInfo.Length, hash, fileInfo.LastWriteTime);
            entries.Add(entry);
        });

        Dictionary<ulong, int> hashCounts = new Dictionary<ulong, int>();

        foreach (FileEntry entry in entries)
        {
            if (hashCounts.ContainsKey(entry.Hash))
            {
                hashCounts[entry.Hash]++;
            }
            else
            {
                hashCounts[entry.Hash] = 1;
            }
        }

        List<FileEntry> filteredObjects = new List<FileEntry>();
        foreach (FileEntry entry in entries)
        {
            if (hashCounts[entry.Hash] > 1)
            {
                filteredObjects.Add(entry);
            }
        }

        _currentDuplicates = filteredObjects;

        UpdateFirstInstances();
    }

    private void UpdateFirstInstances()
    {
        foreach (FileEntry entry in _currentDuplicates)
        {
            if (_mainWindowModel.FirstInstances.All(item => item.Hash != entry.Hash))
            {
                _mainWindowModel.FirstInstances.Add(entry);
            }
        }
    }

    private void FirstInstance_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_mainWindowModel.SelectedFirstInstance == null)
        {
            return;
        }

        _mainWindowModel.Duplicates.Clear();

        foreach (FileEntry entry in _currentDuplicates)
        {
            if (entry.Hash == _mainWindowModel.SelectedFirstInstance.Hash)
            {
                _mainWindowModel.Duplicates.Add(entry);
            }
        }

        UpdatePreview(_mainWindowModel.SelectedFirstInstance.Path);
    }

    private void UpdatePreview(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        string extension = Path.GetExtension(path);

        BitmapImage image;
        MemoryStream memStream;

        switch (extension)
        {
            case ".txt":
            case ".nfo":
                _mainWindowModel.ImageContentVisible = Visibility.Hidden;

                _mainWindowModel.TextContent = File.ReadAllText(path);

                _mainWindowModel.TextContentVisible = Visibility.Visible;
                break;
            case ".jpg":
            case ".jpeg":
            case ".png":
            case ".gif":
                _mainWindowModel.TextContentVisible = Visibility.Hidden;

                image = new BitmapImage();
                image.BeginInit();
                memStream = new MemoryStream(File.ReadAllBytes(path));
                image.StreamSource = memStream;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
                ImageBehavior.SetAnimatedSource(imgContent, image);

                _mainWindowModel.ImageContentVisible = Visibility.Visible;
                break;
            case ".webp":
                _mainWindowModel.TextContentVisible = Visibility.Hidden;

                image = new BitmapImage();

                MagickImageCollection imgCollection = new MagickImageCollection(path);
                memStream = new MemoryStream();
                imgCollection.Write(memStream, MagickFormat.Gif);
                image.StreamSource = memStream;
                image.CacheOption = BitmapCacheOption.OnLoad;

                ImageBehavior.SetAnimatedSource(imgContent, image);

                _mainWindowModel.ImageContentVisible = Visibility.Visible;
                break;
            default:
                _mainWindowModel.ImageContentVisible = Visibility.Hidden;

                _mainWindowModel.TextContent = "No preview available for this file type.";

                _mainWindowModel.TextContentVisible = Visibility.Visible;
                break;
        }
    }

    private void DeleteAll_Click(object sender, RoutedEventArgs e)
    {
        if (_mainWindowModel.SelectedFirstInstance == null)
        {
            return;
        }

        ulong hashToDelete = _mainWindowModel.SelectedFirstInstance.Hash;

        List<FileEntry> toDelete = _currentDuplicates.Where(entry => entry.Hash == hashToDelete).ToList();

        foreach (FileEntry entry in toDelete)
        {
            if (entry.Path != null)
            {
                File.Delete(entry.Path);
            }
        }

        _currentDuplicates.RemoveAll(item => item.Hash == hashToDelete);

        _mainWindowModel.TextContent = string.Empty;
        _mainWindowModel.ImageContentVisible = Visibility.Hidden;
        _mainWindowModel.TextContentVisible = Visibility.Visible;
        _mainWindowModel.FirstInstances.Clear();
        _mainWindowModel.Duplicates.Clear();
        UpdateFirstInstances();
    }

    private void KeepSelected_Click(object sender, RoutedEventArgs e)
    {
        if (_mainWindowModel.SelectedFirstInstance == null)
        {
            return;
        }

        ulong hashToDelete = _mainWindowModel.SelectedFirstInstance.Hash;

        List<FileEntry> toDelete = _currentDuplicates.Where(entry => entry.Hash == hashToDelete).ToList();

        foreach (FileEntry entry in toDelete)
        {
            if (_mainWindowModel.DuplicateSelectedItems == null)
            {
                break;
            }

            if (_mainWindowModel.DuplicateSelectedItems.Any(item => item.Path.Equals(entry.Path)))
            {
                continue;
            }

            if (entry.Path != null)
            {
                File.Delete(entry.Path);

                _currentDuplicates.RemoveAll(item => item.Path.Equals(entry.Path));
            }
        }

        if (_currentDuplicates.Count(item => item.Hash == hashToDelete) < 2)
        {
            _currentDuplicates.RemoveAll(item => item.Hash == hashToDelete);
        }

        _mainWindowModel.TextContent = string.Empty;
        _mainWindowModel.ImageContentVisible = Visibility.Hidden;
        _mainWindowModel.TextContentVisible = Visibility.Visible;
        _mainWindowModel.FirstInstances.Clear();
        _mainWindowModel.Duplicates.Clear();
        UpdateFirstInstances();
    }

    private void DuplicateSelectionChanged_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        DataGrid grid = (DataGrid)sender;
        ObservableCollection<FileEntry> fileEntries = new ObservableCollection<FileEntry>(grid.SelectedItems.Cast<FileEntry>());
        _mainWindowModel.DuplicateSelectedItems = fileEntries;
    }
}