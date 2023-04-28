# Lustre Collector

Lustre Collector is a background service that collects filesystem events from a Lustre filesystem and periodically cleans-up stale files.

## Background

The requirement for Lustre Collector grows out of a need for a fast local-disk cache for IIIF/DLCS assets that are hosted remotely (i.e., in S3).
When an asset is required for orchestration, it will potentially be provisioned to local-storage for fast access, but there's no mechanism in place for cleaning up data after all operations are complete.

This is where Lustre Collector comes in.
Every time the platform accesses an asset via the local-storage that information is recorded and used to determine which of the flies in the cache are infrequently accessed and can be purged with little to zero impact on orchestration and provisioning.

## Deployment

The application can be built as a standalone container image and deployed to any container runtime.

There are currently 3 values that are configurable via appSettings:
- LUSTRE_mountPoint
  - The path, local to the container, where the filesystem to be monitored is located. NOTE: it's assumed this mount point encapsulates the entire filesystem. Mounting a subdirectory will create incorrect disk usage reports.
- LUSTRE_cleanupPeriod
  - The frequency, in milliseconds, that the cleanup routine will be executed. The cleanup routine will compare disk usage with a configured usage threshold and begin purging old files.
- LUSTRE_cleanupThreshold
  - A value between [0, 100) representing a percentage of total disk space usage before considering cleanup. For example, a value of 10 will begin cleanup when there is 10% or less free space available.  
- LUSTRE_cleanupBatchSize
  - During a cleanup operation, files are deleted in batches of this size. Cleanup will stop when free space below threshold. NOTE: We cannot iterate the full set as the change watcher can edit at the same time.    

If specifying as environment variables, prefix with "LUSTRE_"