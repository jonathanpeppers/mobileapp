using Toggl.Core.Analytics;
using Toggl.Core.Interactors;
using Toggl.Core.Services;
using Toggl.Core.UI.Navigation;
using Toggl.Shared;
using Toggl.Storage.Settings;

namespace Toggl.Core.UI.ViewModels.Calendar
{
    [Preserve(AllMembers = true)]
    public sealed class SelectUserCalendarsViewModel : SelectUserCalendarsViewModelBase
    {
        public SelectUserCalendarsViewModel(
            IUserPreferences userPreferences,
            IAnalyticsService analyticsService,
            IOnboardingStorage onboardingStorage,
            IInteractorFactory interactorFactory,
            INavigationService navigationService,
            IRxActionFactory rxActionFactory)
            : base(userPreferences, interactorFactory, onboardingStorage, analyticsService, navigationService, rxActionFactory)
        {
        }
    }
}
