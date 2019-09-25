using System;
using Toggl.Shared;

namespace Toggl.Core.Services
{
    public interface IFetchRemoteConfigService
    {
        void FetchRemoteConfigData(Action onFetchSucceeded, Action<Exception> onFetchFailed);
        RatingViewConfiguration ExtractRatingViewConfigurationFromRemoteConfig();
        PushNotificationsConfiguration ExtractPushNotificationsConfigurationFromRemoteConfig();
    }
}