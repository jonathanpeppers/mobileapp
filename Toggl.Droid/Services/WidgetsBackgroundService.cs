using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Support.V4.App;
using Toggl.Core;
using Toggl.Core.Analytics;
using Toggl.Core.Extensions;
using Toggl.Core.Interactors;
using Toggl.Droid.Extensions;
using Toggl.Droid.Widgets;
using static Toggl.Droid.Services.JobServicesConstants;
using static Toggl.Droid.Widgets.WidgetsConstants;
using static Android.Appwidget.AppWidgetManager;
using Toggl.Droid.Helper;

namespace Toggl.Droid.Services
{
    [Service(Permission = "android.permission.BIND_JOB_SERVICE", Exported = true)]
    public sealed class WidgetsBackgroundService : JobIntentService
    { 
        private IInteractorFactory interactorFactory => AndroidDependencyContainer.Instance.InteractorFactory;
        private ITimeService timeService => AndroidDependencyContainer.Instance.TimeService;

        public static void EnqueueWork(Context context, Intent intent)
        {
            var serviceClass = JavaUtils.ToClass<WidgetsBackgroundService>();
            EnqueueWork(context, serviceClass, TimerWidgetBackgroundServiceJobId, intent);
        }

        protected override void OnHandleWork(Intent intent)
        {
            var action = intent.Action;

            AndroidDependencyContainer.EnsureInitialized(Application.Context);

            switch (action)
            {
                case StartTimeEntryAction:
                    _ = handleStartTimeEntry();
                    break;
                case StopRunningTimeEntryAction:
                    _ = handleStopRunningTimeEntry();
                    break;
                case SuggestionTapped:
                    _ = continueTimeEntryFromSuggestion(intent);
                    break;
                default:
                    throw new InvalidOperationException($"Cannot handle intent with action {action}");
            }
        }

        private async Task handleStartTimeEntry()
        {
            var now = timeService.CurrentDateTime;
            var workspaceId = (await interactorFactory.GetDefaultWorkspace().Execute()).Id;
            var prototype = "".AsTimeEntryPrototype(now, workspaceId);
            await interactorFactory.CreateTimeEntry(prototype, TimeEntryStartOrigin.Widget).Execute();
        }

        private async Task handleStopRunningTimeEntry()
        {
            var now = timeService.CurrentDateTime;
            await interactorFactory.StopTimeEntry(now, TimeEntryStopOrigin.Widget).Execute();
        }

        private async Task continueTimeEntryFromSuggestion(Intent intent)
        {
            var index = intent.GetIntExtra(TappedSuggestionIndex, 0);

            var suggestions = WidgetSuggestionItem.SuggestionsFromSharedPreferences().ToList();
            var suggestion = suggestions[index];

            var now = timeService.CurrentDateTime;

            var timeEntryPrototype = suggestion.Description.AsTimeEntryPrototype(
                startTime: now,
                workspaceId: suggestion.WorkspaceId,
                projectId: suggestion.ProjectId,
                taskId: suggestion.TaskId,
                isBillable:suggestion.IsBillable,
                tagIds: suggestion.TagsIds);

            await interactorFactory.CreateTimeEntry(timeEntryPrototype, TimeEntryStartOrigin.Widget).Execute();
        }
    }
}
