using System.Threading.Channels;

namespace LustreCollector.FileSystem;

public class NativeFileSystemChangeWatcher : IFileSystemChangeWatcher
{
    private readonly FileSystemWatcher watcher;
    private readonly ILogger<NativeFileSystemChangeWatcher> _logger;

    public NativeFileSystemChangeWatcher(
        FileSystemWatcher watcher,
        ILogger<NativeFileSystemChangeWatcher> logger)
    {
        this.watcher = watcher;
        _logger = logger;
    }
    
    public IAsyncEnumerable<FileSystemChangeEvent> Watch(DirectoryInfo root)
    {
        var changes = Channel.CreateUnbounded<FileSystemChangeEvent>();
        try
        {
            //var watcher = new FileSystemWatcher();
            watcher.Path = root.FullName;
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Filter = "*.*";
            FileSystemEventHandler fsEventHandler = (sender, fsEvent) =>
            {
                var change = FileSystemChangeEvent.FromNativeEvent(fsEvent);
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