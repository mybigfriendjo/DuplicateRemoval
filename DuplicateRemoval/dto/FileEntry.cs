using CommunityToolkit.Mvvm.ComponentModel;

namespace DuplicateRemoval.dto;

public class FileEntry : ObservableObject
{
    private DateTime _lastChanged;
    private long _size;
    private string? _path;
    private ulong _hash;

    public ulong Hash { get { return _hash; } set { SetProperty(ref _hash, value); } }

    public DateTime LastChanged { get { return _lastChanged; } set { SetProperty(ref _lastChanged, value); } }

    public string? Path { get { return _path; } set { SetProperty(ref _path, value); } }

    public long Size { get { return _size; } set { SetProperty(ref _size, value); } }

    public FileEntry(string? path, long size, ulong hash, DateTime lastChanged)
    {
        Path = path;
        Size = size;
        Hash = hash;
        LastChanged = lastChanged;
    }
}