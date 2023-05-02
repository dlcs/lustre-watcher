namespace LustreCollector.FileSystem;

public interface IFileSystemChangeWatcher
{
    public IAsyncEnumerable<FileSystemChangeEvent> Watch(CancellationToken cancellationToken);
}