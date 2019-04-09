﻿using Toggl.Shared.Models;

namespace Toggl.Storage.Realm.Models
{
    interface IModifiableId : IIdentifiable
    {
        new long Id { get; set; }

        long? OriginalId { get; set; }

        void ChangeId(long id);
    }
}
