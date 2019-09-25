using System;
using Toggl.Shared;

namespace Toggl.Core.Services
{
    public interface IRemoteConfigService
    {
        RatingViewConfiguration GetRatingViewConfiguration();
        PushNotificationsConfiguration GetPushNotificationsConfiguration();
    }
}
