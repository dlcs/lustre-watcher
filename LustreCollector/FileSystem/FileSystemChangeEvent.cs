using static LustreCollector.FileSystem.FileSystemChangeEventKind;

namespace LustreCollector.FileSystem;

public enum FileSystemChangeEventKind
{
    Created,
    Deleted,
    Accessed,
}

/// <summary>
/// Represents a single file system change event
/// </summary>
public class FileSystemChangeEvent
{
    public FileSystemChangeEventKind Kind { get; }
    public string Path { get; }

    public FileSystemChangeEvent(FileSystemChangeEventKind kind, string path)
    {
        Kind = kind;
        Path = path;
    }

    public static FileSystemChangeEvent? FromNativeEvent(FileSystemEventArgs nativeEvent)
    {
        return nativeEvent.ChangeType switch
        {
            WatcherChangeTypes.Created => new FileSystemChangeEvent(Created, nativeEvent.FullPath),
            WatcherChangeTypes.Deleted => new FileSystemChangeEvent(Deleted, nativeEvent.FullPath),
            WatcherChangeTypes.Changed => new FileSystemChangeEvent(Accessed, nativeEvent.FullPath),
            WatcherChangeTypes.All => new FileSystemChangeEvent(Accessed, nativeEvent.FullPath),
            WatcherChangeTypes.Renamed => null,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}