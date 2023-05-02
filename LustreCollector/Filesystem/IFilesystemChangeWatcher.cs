namespace LustreCollector.Filesystem;

public interface IFilesystemChangeWatcher
{
    public IAsyncEnumerable<FilesystemChangeEvent> Watch(DirectoryInfo root);
}