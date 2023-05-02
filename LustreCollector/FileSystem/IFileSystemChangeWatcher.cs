namespace LustreCollector.FileSystem;

public interface IFileSystemChangeWatcher
{
    public IAsyncEnumerable<FileSystemChangeEvent> Watch(DirectoryInfo root);
}