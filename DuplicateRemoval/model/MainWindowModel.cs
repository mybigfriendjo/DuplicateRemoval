using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using DuplicateRemoval.dto;

namespace DuplicateRemoval.model;

public class MainWindowModel : ObservableObject
{
    private FileEntry? _selectedFirstInstance;
    private ObservableCollection<FileEntry> _duplicates = new ObservableCollection<FileEntry>();
    private ObservableCollection<FileEntry> _firstInstances = new ObservableCollection<FileEntry>();
    private ObservableCollection<FileEntry>? _duplicateSelectedItems = new ObservableCollection<FileEntry>();
    private string? _folderPath;
    private string? _textContent;
    private Visibility _imageContentVisible;
    private Visibility _textContentVisible;

    public ObservableCollection<FileEntry> Duplicates { get { return _duplicates; } set { SetProperty(ref _duplicates, value); } }

    public ObservableCollection<FileEntry>? DuplicateSelectedItems
    {
        get { return _duplicateSelectedItems; }
        set { SetProperty(ref _duplicateSelectedItems, value); }
    }

    public ObservableCollection<FileEntry> FirstInstances { get { return _firstInstances; } set { SetProperty(ref _firstInstances, value); } }

    public string? FolderPath { get { return _folderPath; } set { SetProperty(ref _folderPath, value); } }

    public Visibility ImageContentVisible { get { return _imageContentVisible; } set { SetProperty(ref _imageContentVisible, value); } }

    public FileEntry? SelectedFirstInstance { get { return _selectedFirstInstance; } set { SetProperty(ref _selectedFirstInstance, value); } }

    public string? TextContent { get { return _textContent; } set { SetProperty(ref _textContent, value); } }

    public Visibility TextContentVisible { get { return _textContentVisible; } set { SetProperty(ref _textContentVisible, value); } }
}