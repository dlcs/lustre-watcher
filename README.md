# Lustre Collector

Lustre Collector is a background service that collects filesystem events from a Lustre filesystem and periodically cleans-up stale files.

## Background

The requirement for Lustre Collector grows out of a need for a fast local-disk cache for IIIF/DLCS assets that are hosted remotely (i.e., in S3).
When an asset is required for orchestration, it will potentially be provisioned to local-storage for fast access, but there's no mechanism in place for cleaning up data after all operations are complete.

This is where Lustre Collector comes in.
Every time the platform accesses an asset via the local-storage that information is recorded and used to determine which of the flies in the cache are infrequently accessed and can be purged with little to zero impact on orchestration and provisioning.

## Deployment

The application can be built as a standalone container image and deployed to any container runtime.

The following values that are configurable via appSettings:
- `MountPoint`
  - The path, local to the container, where the filesystem to be monitored is located. NOTE: it's assumed this mount point encapsulates the entire filesystem. Mounting a subdirectory will create incorrect disk usage reports.
- `CleanupPeriod`
  - The frequency, in milliseconds, that the cleanup routine will be executed. The cleanup routine will compare disk usage with a configured usage threshold and begin purging old files. Default: 2s.
- `FreeSpaceThreshold`
  - A value between (1, 100) representing the % of available free space beyond which scavenging will take place. For example, a value of 10 will begin cleanup when there is 10% or less free space available. Default: 10.
- `CleanupBatchSize`
  - During a cleanup operation, files are deleted in batches of this size. Cleanup will stop when free space below threshold. NOTE: We cannot iterate the full set as the change watcher can edit at the same time. Default: 100.
- `MinimiseDeletions`
  - During a cleanup operation, available size can be reassessed on every deletion to keep number of files removed to a minimum. Default: false.
- `FileWatcherBufferSize`
  - Value for [`FileSystemWatcher.InternalBufferSize`](https://learn.microsoft.com/en-us/dotnet/api/system.io.filesystemwatcher.internalbuffersize?view=net-6.0). Defaults to maximum 64KB.

In addition to [default `HostBuilder`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.host.createdefaultbuilder?view=dotnet-plat-ext-7.0&viewFallbackFrom=net-6.0) `IConfiguration` sources, environment variables prefix with "LUSTRE_" will be detected.

## Running

There is a Dockerfile available:

```bash
# build docker file
docker build -t lustre-watcher:local .

# run file
docker run -it --rm \
  --name lustre-watcher \
  -e MountPoint="/nas" \
  -v /path/to/folder:/nas \
  lustre-watcher:local
```