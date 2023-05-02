using System.ComponentModel.DataAnnotations;

namespace LustreCollector;

public class FileCleanupConfiguration : IValidatableObject
{
    /// <summary>
    /// The path, local to the container, where the filesystem to be monitored is located
    /// </summary>
    public string MountPoint { get; set; }
    
    /// <summary>
    /// A value between [0, 100) representing a percentage of total disk space usage before considering cleanup.
    /// </summary>
    public int CleanupThreshold { get; set; } = 10;
    
    /// <summary>
    /// The frequency, in milliseconds, that the cleanup routine will be executed.
    /// </summary>
    public int CleanupPeriod { get; set; } = 2_000;

    /// <summary>
    /// Number of file candidates to consider for deletion when threshold breached.
    /// </summary>
    public int CleanupBatchSize { get; set; } = 100;
    
    /// <summary>
    /// If true, size will be reassessed after every deletion to minimise number of files removed. Else the entire batch
    /// of candidates will be removed before checking size.
    /// </summary>
    public bool MinimiseDeletions { get; set; }

    /// <summary>
    /// InternalBuffer size for FileSystemWatcher, in bytes. Valid values: 4KB -> 64KB
    /// </summary>
    public int FileWatcherBufferSize { get; set; } = 65536;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var valid = true;

        var mountPointExists = Directory.Exists(MountPoint);
        valid &= mountPointExists;

        if (!mountPointExists)
        {
            yield return new ValidationResult("MountPoint must exist");
        }

        var cleanupThresholdValid = CleanupThreshold is > 1 and < 100;
        valid &= cleanupThresholdValid;

        if (!cleanupThresholdValid)
        {
            yield return new ValidationResult("CleanupThreshold must be in the range of (0,100)");
        }

        var cleanupPeriodValid = CleanupPeriod > 0;
        valid &= cleanupPeriodValid;

        if (!cleanupPeriodValid)
        {
            yield return new ValidationResult("CleanupPeriod must be non-zero");
        }

        if (valid)
        {
            yield return ValidationResult.Success;
        }
    }
}