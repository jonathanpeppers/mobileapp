﻿using System;
using Toggl.Core.Models.Interfaces;

namespace Toggl.Core.Extensions
{
    public static class ThreadSafeExtensions
    {
        private const string inaccessibleProjectColor = "#cecece";

        public static string DisplayName(this IThreadSafeProject project)
        {
            var name = project.Name ?? "";

            switch (project.SyncStatus)
            {
                case Storage.SyncStatus.RefetchingNeeded:
                    return Resources.InaccessibleProject;
                default:
                    return project.Active ? name : $"{name} {Resources.ArchivedProjectDecorator}";
            }
        }

        public static string DisplayColor(this IThreadSafeProject project)
        {
            switch (project.SyncStatus)
            {
                case Storage.SyncStatus.RefetchingNeeded:
                    return inaccessibleProjectColor;
                default:
                    return project.Color ?? "";
            }
        }

        public static TimeSpan? TimeSpanDuration(this IThreadSafeTimeEntry timeEntry)
            => timeEntry.Duration.HasValue
            ? TimeSpan.FromSeconds(timeEntry.Duration.Value)
            : (TimeSpan?)null;
    }
}
