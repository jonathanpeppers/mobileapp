using System;
using System.Linq;
using Toggl.Core.Extensions;
using Toggl.Core.Models.Interfaces;
using Toggl.Core.MvvmCross.Transformations;
using Toggl.Core.MvvmCross.ViewModels.TimeEntriesLog;
using Toggl.Shared;
using Toggl.Storage;

namespace Toggl.Core.MvvmCross.Extensions
{
    public static class TimeEntryExtensions
    {
        public static LogItemViewModel ToViewModel(
            this IThreadSafeTimeEntry timeEntry,
            GroupId groupId,
            LogItemVisualizationIntent visualizationIntent,
            DurationFormat durationFormat)
            => new LogItemViewModel(
                groupId,
                new[] { timeEntry.Id },
                visualizationIntent,
                timeEntry.Billable,
                timeEntry.Description,
                timeEntry.Duration.HasValue
                    ? DurationAndFormatToString.Convert(
                        TimeSpan.FromSeconds(timeEntry.Duration.Value), durationFormat)
                    : string.Empty,
                timeEntry.Project?.DisplayName(),
                timeEntry.Project?.DisplayColor(),
                timeEntry.Project?.Client?.Name,
                timeEntry.Task?.Name,
                timeEntry.TagIds.Any(),
                timeEntry.SyncStatus == SyncStatus.SyncNeeded,
                timeEntry.SyncStatus != SyncStatus.SyncFailed,
                timeEntry.IsInaccessible);
    }
}
