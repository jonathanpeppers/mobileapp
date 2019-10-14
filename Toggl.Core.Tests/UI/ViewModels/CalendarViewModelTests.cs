using FluentAssertions;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Toggl.Core.Models.Interfaces;
using Toggl.Core.Tests.Generators;
using Toggl.Core.Tests.TestExtensions;
using Toggl.Core.UI.Navigation;
using Toggl.Core.UI.ViewModels;
using Toggl.Core.UI.ViewModels.Calendar;
using Toggl.Shared;
using Xunit;

namespace Toggl.Core.Tests.UI.ViewModels
{
    public sealed class CalendarViewModelTests
    {
        public abstract class CalendarViewModelTest : BaseViewModelTests<CalendarViewModel>
        {
            protected override CalendarViewModel CreateViewModel()
                => new CalendarViewModel(
                    DataSource,
                    TimeService,
                    RxActionFactory,
                    UserPreferences,
                    AnalyticsService,
                    BackgroundService,
                    InteractorFactory,
                    SchedulerProvider,
                    NavigationService
                );
        }

        public sealed class TheConstructor : CalendarViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useDataSource,
                bool useTimeService,
                bool useUserPreferences,
                bool useAnalyticsService,
                bool useBackgroundService,
                bool useInteractorFactory,
                bool useSchedulerProvider,
                bool useNavigationService,
                bool useRxActionFactory)
            {
                var dataSource = useDataSource ? DataSource : null;
                var timeService = useTimeService ? TimeService : null;
                var userPreferences = useUserPreferences ? UserPreferences : null;
                var rxActionFactory = useRxActionFactory ? RxActionFactory : null;
                var analyticsService = useAnalyticsService ? AnalyticsService : null;
                var backgroundService = useBackgroundService ? BackgroundService : null;
                var interactorFactory = useInteractorFactory ? InteractorFactory : null;
                var schedulerProvider = useSchedulerProvider ? SchedulerProvider : null;
                var navigationService = useNavigationService ? NavigationService : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new CalendarViewModel(
                        dataSource,
                        timeService,
                        rxActionFactory,
                        userPreferences,
                        analyticsService,
                        backgroundService,
                        interactorFactory,
                        schedulerProvider,
                        navigationService);

                tryingToConstructWithEmptyParameters.Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheCurrentlyShownDateStringObservable : CalendarViewModelTest
        {
            private static readonly DateTimeOffset now = new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.Zero).Date;

            public TheCurrentlyShownDateStringObservable()
            {
                TimeService.CurrentDateTime.Returns(now);
            }

            [Fact, LogIfTooSlow]
            public async Task StartsWithTheCurrentDate()
            {
                var preferences = Substitute.For<IThreadSafePreferences>();
                preferences.DateFormat.Returns(DateFormat.ValidDateFormats[0]);
                DataSource.Preferences.Current.Returns(Observable.Return(preferences));
                var expectedResult = "Thursday, Jan 2";
                var observer = TestScheduler.CreateObserver<string>();
                var viewModel = CreateViewModel();
                viewModel.CurrentlyShownDateString.Subscribe(observer);

                TestScheduler.Start();

                observer.Values().Should().BeEquivalentTo(new[] { expectedResult });
            }

            [Theory, LogIfTooSlow]
            [InlineData(-1, "Wednesday, Jan 1")]
            [InlineData(-2, "Tuesday, Dec 31")]
            [InlineData(-7, "Thursday, Dec 26")]
            [InlineData(-14, "Thursday, Dec 19")]
            public void EmitsNewDateWhenCurrentlyVisiblePageChanges(int pageIndex, string expectedDate)
            {
                var expectedResults = new[]
                {
                    "Thursday, Jan 2",
                    expectedDate
                };
                var observer = TestScheduler.CreateObserver<string>();
                var viewModel = CreateViewModel();
                viewModel.CurrentlyShownDateString.Subscribe(observer);

                TestScheduler.Start();
                viewModel.CurrentlyVisiblePage.Accept(pageIndex);
                TestScheduler.Start();

                observer.Values().Should().BeEquivalentTo(expectedResults);
            }
        }

        public sealed class TheOpenSettingsAction : CalendarViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void NavigatesToTheSettingsViewModel()
            {
                ViewModel.OpenSettings.Execute();

                NavigationService.Received().Navigate<SettingsViewModel>(View);
            }
        }
    }
}
