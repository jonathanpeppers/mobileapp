using System;
using System.Collections;
using Foundation;
using Toggl.Daneel.ExtensionKit.Analytics;

namespace Toggl.Daneel.ExtensionKit
{
    public class SharedStorage
    {
        private const string ApiTokenKey = "APITokenKey";
        private const string NeedsSyncKey = "NeedsSyncKey";
        private const string UserIdKey = "UserId";
        private const string SiriTrackingEventsKey = "SiriTrackingEventsKey";
        private const string LastUpdateKey = "LastUpdateKey";

        private NSUserDefaults userDefaults;

        private SharedStorage()
        {
            var bundleId = NSBundle.MainBundle.BundleIdentifier;
            if (bundleId.Contains("SiriExtension") || bundleId.Contains("ShareExtension"))
            {
                bundleId = bundleId.Substring(0, bundleId.LastIndexOf("."));
            }
            userDefaults = new NSUserDefaults($"group.{bundleId}.extensions", NSUserDefaultsType.SuiteName);
        }

        public static SharedStorage instance => new SharedStorage();

        public void SetApiToken(string apiToken)
        {
            userDefaults.SetString(apiToken, ApiTokenKey);
            userDefaults.Synchronize();
        }

        public void SetNeedsSync(bool value)
        {
            userDefaults.SetBool(value, NeedsSyncKey);
            userDefaults.Synchronize();
        }

        public void SetUserId(double userId)
        {
            userDefaults.SetDouble(userId, UserIdKey);
            userDefaults.Synchronize();
        }

        public void SetLastUpdateDate(DateTimeOffset date)
        {
            userDefaults.SetDouble(date.ToUnixTimeMilliseconds(), LastUpdateKey);
            userDefaults.Synchronize();
        }

        public void AddSiriTrackingEvent(SiriTrackingEvent e)
        {
            var currentEvents = (NSMutableArray)getTrackableEvents().MutableCopy();
            currentEvents.Add(e);

            userDefaults[SiriTrackingEventsKey] = NSKeyedArchiver.ArchivedDataWithRootObject(currentEvents);
            userDefaults.Synchronize();
        }

        public SiriTrackingEvent[] PopTrackableEvents()
        {
            var eventArrays = getTrackableEvents();
            userDefaults.RemoveObject(SiriTrackingEventsKey);
            return NSArray.FromArrayNative<SiriTrackingEvent>(eventArrays);
        }

        public double GetUserId() => userDefaults.DoubleForKey(UserIdKey);

        public string GetApiToken() => userDefaults.StringForKey(ApiTokenKey);

        public bool GetNeedsSync() => userDefaults.BoolForKey(NeedsSyncKey);

        public DateTimeOffset GetLastUpdateDate() => DateTimeOffset.FromUnixTimeMilliseconds((long)userDefaults.DoubleForKey(LastUpdateKey));

        public void DeleteEverything()
        {
            userDefaults.RemoveObject(ApiTokenKey);
            userDefaults.RemoveObject(NeedsSyncKey);
            userDefaults.RemoveObject(UserIdKey);
            userDefaults.RemoveObject(SiriTrackingEventsKey);
            userDefaults.Synchronize();
        }

        private NSArray getTrackableEvents()
        {
            var eventArrayData = userDefaults.ValueForKey(new NSString(SiriTrackingEventsKey)) as NSData;

            if (eventArrayData == null)
            {
                return new NSArray();
            }

            return NSKeyedUnarchiver.UnarchiveObject(eventArrayData) as NSArray;
        }
    }
}
