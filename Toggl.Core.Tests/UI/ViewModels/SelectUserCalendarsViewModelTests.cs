using FluentAssertions;
using NSubstitute;
using System;
using System.Collections.Generic;
using Toggl.Core.Tests.Generators;
using Toggl.Core.UI.ViewModels.Calendar;
using Xunit;

namespace Toggl.Core.Tests.UI.ViewModels
{
    public sealed class SelectUserCalendarsViewModelTests
    {
        public abstract class SelectUserCalendarsViewModelTest : BaseViewModelTests<SelectUserCalendarsViewModel, bool, string[]>
        {
            protected SelectUserCalendarsViewModelTest()
            {
                UserPreferences.EnabledCalendarIds().Returns(new List<string>());
            }

            protected override SelectUserCalendarsViewModel CreateViewModel()
                => new SelectUserCalendarsViewModel(UserPreferences, AnalyticsService, OnboardingStorage, InteractorFactory, NavigationService, RxActionFactory);
        }

        public sealed class TheConstructor : SelectUserCalendarsViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useUserPreferences,
                bool useAnalyticsService,
                bool useOnboardingStorage,
                bool useInteractorFactory,
                bool useNavigationService,
                bool useRxActionFactory)
            {
                Action tryingToConstructWithEmptyParameters =
                    () => new SelectUserCalendarsViewModel(
                        useUserPreferences ? UserPreferences : null,
                        useAnalyticsService ? AnalyticsService : null,
                        useOnboardingStorage ? OnboardingStorage : null,
                        useInteractorFactory ? InteractorFactory : null,
                        useNavigationService ? NavigationService : null,
                        useRxActionFactory ? RxActionFactory : null
                    );

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

    }
}
