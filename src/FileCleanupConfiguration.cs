using System.ComponentModel.DataAnnotations;

namespace LustreCollector;

public class FileCleanupConfiguration : IValidatableObject
{
    public string MountPoint { get; set; }

    public int CleanupThreshold { get; set; } = 10;
    public int CleanupPeriod { get; set; } = 2_000;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var valid = true;

        var mountPointExists = File.Exists(MountPoint);
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