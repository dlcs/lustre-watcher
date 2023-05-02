using System.Threading.Channels;

namespace LustreCollector.Filesystem;

public class NativeFilesystemChangeWatcher : IFilesystemChangeWatcher
{
    private readonly FileSystemWatcher watcher;
    private readonly ILogger<NativeFilesystemChangeWatcher> _logger;

    public NativeFilesystemChangeWatcher(
        FileSystemWatcher watcher,
        ILogger<NativeFilesystemChangeWatcher> logger)
    {
        this.watcher = watcher;
        _logger = logger;
    }
    
    public IAsyncEnumerable<FilesystemChangeEvent> Watch(DirectoryInfo root)
    {
        var changes = Channel.CreateUnbounded<FilesystemChangeEvent>();
        try
        {
            //var watcher = new FileSystemWatcher();
            watcher.Path = root.FullName;
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Filter = "*.*";
            FileSystemEventHandler fsEventHandler = (sender, fsEvent) =>
            {
                var change = FilesystemChangeEvent.FromNativeEvent(fsEvent);
                if (change != null && !changes.Writer.TryWrite(change))
                {
                    // we're dropping changes because we can't process them fast enough
                    _logger.LogWarning("Unable to log {Change}, ChannelWriter failed write", change);
                }
            };
            watcher.Disposed += (sender, args) => _logger.LogInformation("Watcher disposed");
            watcher.Error += (sender, args) => _logger.LogWarning("*****uh-oh");
            watcher.InternalBufferSize = 65536;
            watcher.Changed += fsEventHandler;
            watcher.Deleted += fsEventHandler;

            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;

            return changes.Reader.ReadAllAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FSW err");
            throw;
        }
    }
}