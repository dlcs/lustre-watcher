namespace LustreCollector.Filesystem;

public interface IFilesystemChangeWatcher
{
    public IAsyncEnumerable<FilesystemChangeEvent> Watch(FileInfo root);
}