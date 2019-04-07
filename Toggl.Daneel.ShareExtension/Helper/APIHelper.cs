using System;
using Foundation;
using Toggl.Daneel.ExtensionKit;
using Toggl.Ultrawave;
using Toggl.Ultrawave.Network;

namespace Toggl.Daneel.ShareExtension.Helper
{
    public class APIHelper
    {
        #if USE_PRODUCTION_API
        private const ApiEnvironment environment = ApiEnvironment.Production;
        #else
        private const ApiEnvironment environment = ApiEnvironment.Staging;
        #endif

        public static ITogglApi GetTogglAPI()
        {
            var apiToken = SharedStorage.instance.GetApiToken();
            Console.WriteLine(SharedStorage.instance);
            Console.WriteLine(apiToken);
            if (apiToken == null)
            {
                return null;
            }

            var version = NSBundle.MainBundle.InfoDictionary["CFBundleShortVersionString"].ToString();
            var userAgent = new UserAgent("Daneel", $"{version} ShareExtension");
            var apiConfiguration = new ApiConfiguration(environment, Credentials.WithApiToken(apiToken), userAgent);
            return TogglApiFactory.WithConfiguration(apiConfiguration);
        }
    }
}
