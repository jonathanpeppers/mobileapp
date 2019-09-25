using System;
using System.Collections.Generic;
using System.Reactive;
using Toggl.Shared;

namespace Toggl.Networking.ApiClients
{
    public interface IPushServicesApi
    {
        IObservable<Unit> Subscribe(PushNotificationsToken token);
        IObservable<Unit> Unsubscribe(PushNotificationsToken token);
        IObservable<List<PushNotificationsToken>> GetAll();
    }
}
