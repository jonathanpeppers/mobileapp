using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Core.Interactors;
using Toggl.Core.Interactors.Suggestions;
using Toggl.Core.Models.Interfaces;
using Toggl.Core.Suggestions;
using Toggl.Core.Tests.Generators;
using Toggl.Core.Tests.Mocks;
using Xunit;

namespace Toggl.Core.Tests.Interactors.Suggestions
{
    public sealed class GetSuggestionProvidersInteractorTests
    {
        public sealed class TheConstructor : BaseInteractorTests
        {

            private readonly IInteractor<IObservable<IThreadSafeWorkspace>> defaultWorkspaceInteractor = Substitute.For<IInteractor<IObservable<IThreadSafeWorkspace>>>();

            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useStopWatchProvider,
                bool useDataSource,
                bool useTimeService,
                bool useCalendarService,
                bool useDefaultWorkspaceInteractor)
            {
                Action createInstance = () => new GetSuggestionProvidersInteractor(
                    3,
                    useStopWatchProvider ? StopwatchProvider : null,
                    useDataSource ? DataSource : null,
                    useTimeService ? TimeService : null,
                    useCalendarService ? CalendarService : null,
                    useDefaultWorkspaceInteractor ? defaultWorkspaceInteractor : null);

                createInstance.Should().Throw<ArgumentNullException>();
            }

            [Theory, LogIfTooSlow]
            [InlineData(0)]
            [InlineData(-1)]
            [InlineData(10)]
            [InlineData(-100)]
            [InlineData(256)]
            public void ThrowsIfTheCountIsOutOfRange(int count)
            {
                Action createInstance = () => new GetSuggestionProvidersInteractor(
                    count,
                    StopwatchProvider,
                    DataSource,
                    TimeService,
                    CalendarService,
                    defaultWorkspaceInteractor);

                createInstance.Should().Throw<ArgumentException>();
            }
        }
    }
}
