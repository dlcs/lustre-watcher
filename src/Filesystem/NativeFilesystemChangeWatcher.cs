using System.Threading.Channels;

namespace LustreCollector.Filesystem;

public class NativeFilesystemChangeWatcher : IFilesystemChangeWatcher
{
    private readonly ILogger<NativeFilesystemChangeWatcher> _logger;

    public NativeFilesystemChangeWatcher(ILogger<NativeFilesystemChangeWatcher> logger)
    {
        _logger = logger;
    }
    
    public IAsyncEnumerable<FilesystemChangeEvent> Watch(DirectoryInfo root)
    {
        var changes = Channel.CreateUnbounded<FilesystemChangeEvent>();
        var watcher = new FileSystemWatcher();

        watcher.Path = root.FullName;
        watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.LastAccess | NotifyFilters.FileName |
                               NotifyFilters.DirectoryName;
        watcher.Filter = "*.*";
        FileSystemEventHandler fsEventHandler = (sender, fsEvent) =>
        {
            var change = FilesystemChangeEvent.FromNativeEvent(fsEvent);
            if (change != null && !changes.Writer.TryWrite(change))
            {
                // we're dropping changes because we can't process them fast enough'
                _logger.LogWarning("Unable to log {Change}, ChannelWriter failed write", change);
            }
        };
        watcher.Changed += fsEventHandler;
        watcher.Deleted += fsEventHandler;
        
        watcher.EnableRaisingEvents = true;

        return changes.Reader.ReadAllAsync();
    }
}