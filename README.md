# Lustre Collector

Lustre Collector is a background service that collects filesystem events from a Lustre filesystem and periodically cleans-up stale files.

## Background

The requirement for Lustre Collector grows out of a need for a fast local-disk cache for IIIF/DLCS assets that are hosted remotely (i.e., in S3).
When an asset is required for orchestration, it will potentially be provisioned to local-storage for fast access, but there's no mechanism in place for cleaning up data after all operations are complete.

This is where Lustre Collector comes in.
Every time the platform accesses an asset via the local-storage that information is recorded and used to determine which of the flies in the cache are infrequently accessed and can be purged with little to zero impact on orchestration and provisioning.

## Implementation Strategy

Previously implementations of this solution relied on walking entire filesystem trees of networked filesystems on a schedule, which resulted in both inaccurate data and generating far more IO than would be necessary.


As of Lustre 2.0 the MDS has a changelog feature that can record [all operations](https://github.com/DDNStorage/lustre_manual_markdown/blob/master/03.01-Monitoring%20a%20Lustre%20File%20System.md#lustre-changelogs) that happen in a Lustre filesystem.
Note that access time modifications and open events are not recorded by default, but can be recorded by setting the changelog mask:

```shell
> # lctl set_param mdd.lustre-MDT0000.changelog_mask=OPEN ATIME CREAT UNLINK
```

The changelog is a simple fixed size ring buffer that will begin to purge old records as the storage fills.
However, once a changelog reader has been registered Lustre will stop purging records that have not been marked as seen by all readers.

Every record stored contains the following information for every event:
```
operation_type(numerical/text) 
timestamp 
datestamp 
flags 
t=target_FID 
ef=extended_flags
u=uid:gid
nid=client_NID
p=parent_FID 
target_name
```

From a `parent_FID` and a `target_name` an event is recorded for the full path by running:

```shell
> $ "$(lfs fid2path $parent_FID)/$target_name"
```