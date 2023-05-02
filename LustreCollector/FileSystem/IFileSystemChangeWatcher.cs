namespace LustreCollector.FileSystem;

public interface IFileSystemChangeWatcher
{
    public IAsyncEnumerable<FilesystemChangeEvent> Watch(DirectoryInfo root);
}