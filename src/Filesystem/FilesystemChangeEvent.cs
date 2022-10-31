using static LustreCollector.Filesystem.FilesystemChangeEventKind;

namespace LustreCollector.Filesystem;

public enum FilesystemChangeEventKind
{
    Created,
    Deleted,
    Accessed,
}

public class FilesystemChangeEvent
{
    public FilesystemChangeEventKind Kind { get; }
    public string Path { get; }

    public FilesystemChangeEvent(FilesystemChangeEventKind kind, string path)
    {
        Kind = kind;
        Path = path;
    }

    public static FilesystemChangeEvent? FromNativeEvent(FileSystemEventArgs nativeEvent)
    {
        return nativeEvent.ChangeType switch
        {
            WatcherChangeTypes.Created => new FilesystemChangeEvent(Created, nativeEvent.FullPath),
            WatcherChangeTypes.Deleted => new FilesystemChangeEvent(Deleted, nativeEvent.FullPath),
            WatcherChangeTypes.Changed => new FilesystemChangeEvent(Accessed, nativeEvent.FullPath),
            WatcherChangeTypes.All => new FilesystemChangeEvent(Accessed, nativeEvent.FullPath),
            WatcherChangeTypes.Renamed => null,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}