using System.Threading.Channels;
using Microsoft.Extensions.Options;

namespace LustreCollector.FileSystem;

/// <summary>
/// Uses <see cref="FileSystemWatcher"/> to monitor underlying filesystem for changes 
/// </summary>
public class NativeFileSystemChangeWatcher : IFileSystemChangeWatcher
{
    private readonly FileSystemWatcher _watcher;
    private readonly ILogger<NativeFileSystemChangeWatcher> _logger;
    private readonly Channel<FileSystemChangeEvent> _changes;

    public NativeFileSystemChangeWatcher(
        IOptionsMonitor<FileCleanupConfiguration> config,
        ILogger<NativeFileSystemChangeWatcher> logger)
    {
        _logger = logger;
        _watcher = CreateFileSystemWatcher(config.CurrentValue);
        _changes = Channel.CreateUnbounded<FileSystemChangeEvent>();
    }

    public IAsyncEnumerable<FileSystemChangeEvent> Watch(CancellationToken cancellationToken)
    {
        _watcher.Disposed += FsDisposedHandler; 
        _watcher.Error += FsErrorHandler;
        _watcher.Changed += FsEventHandler;
        _watcher.Deleted += FsEventHandler;
        _watcher.EnableRaisingEvents = true;

        return _changes.Reader.ReadAllAsync(cancellationToken);
    }

    private FileSystemWatcher CreateFileSystemWatcher(FileCleanupConfiguration config)
    {
        var watcher = new FileSystemWatcher(config.MountPoint, "*.*");
        watcher.InternalBufferSize = config.FileWatcherBufferSize;
        watcher.NotifyFilter = NotifyFilters.LastWrite;
        watcher.IncludeSubdirectories = true;
        return watcher;
    }
    
    private void FsEventHandler(object sender, FileSystemEventArgs fsEvent)
    {
        var change = FileSystemChangeEvent.FromNativeEvent(fsEvent);
        if (change != null)
        {
            _changes.Writer.TryWrite(change);
        }
    }

    private void FsErrorHandler(object sender, ErrorEventArgs args)
    {
        var exception = args.GetException();
        _logger.LogError(exception, "FileSystemWatcher error");
        throw exception;
    }

    private void FsDisposedHandler(object? sender, EventArgs args)
    {
        _logger.LogInformation("FileSystemWatcher disposed");
        throw new ApplicationException("FileSystemWatcher disposed");
    }
}