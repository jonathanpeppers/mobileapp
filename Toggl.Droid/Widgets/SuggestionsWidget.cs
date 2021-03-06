using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.OS;
using AndroidX.Work;
using Toggl.Droid.SystemServices;

namespace Toggl.Droid.Widgets
{
    [BroadcastReceiver(Label = "@string/suggestions", Exported = true)]
    [IntentFilter(new string[] { AppWidgetManager.ActionAppwidgetUpdate })]
    [MetaData("android.appwidget.provider", Resource = "@xml/suggestionswidgetprovider")]
    public class SuggestionsWidget : AppWidgetProvider
    {
        public override void OnDeleted(Context context, int[] appWidgetIds)
        {
            reportInstallationState(context, false);
            base.OnDeleted(context, appWidgetIds);
        }

        public override void OnEnabled(Context context)
        {
            reportInstallationState(context, true);
            base.OnEnabled(context);
        }

        public override void OnUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
        {
            base.OnUpdate(context, appWidgetManager, appWidgetIds);

            foreach (var appWidgetId in appWidgetIds)
            {
                var options = appWidgetManager.GetAppWidgetOptions(appWidgetId);
                var dimensions = WidgetDimensions.FromBundle(options);
                updateWidget(context, appWidgetManager, appWidgetId, dimensions);
            }
        }

        public override void OnAppWidgetOptionsChanged(Context context, AppWidgetManager appWidgetManager, int appWidgetId, Bundle newOptions)
        {
            base.OnAppWidgetOptionsChanged(context, appWidgetManager, appWidgetId, newOptions);
            var dimensions = WidgetDimensions.FromBundle(newOptions);

            updateWidget(context, appWidgetManager, appWidgetId, dimensions);
        }

        private static void updateWidget(Context context, AppWidgetManager appWidgetManager, int appWidgetId, WidgetDimensions dimensions)
        {
            var widgetFormFactor = SuggestionsWidgetFactory.Create(dimensions);
            var view = widgetFormFactor.Setup(context, appWidgetId);

            if (widgetFormFactor.ContainsListView)
                appWidgetManager.NotifyAppWidgetViewDataChanged(appWidgetId, Resource.Id.SuggestionsList);

            appWidgetManager.UpdateAppWidget(appWidgetId, view);
        }

        private void reportInstallationState(Context context, bool installed)
        {
            var inputData = new Data.Builder()
                .PutString(WidgetsAnalyticsWorker.WidgetsAnalyticsWorkerAction, WidgetsAnalyticsWorker.SuggestionsWidgetInstallAction)
                .PutBoolean(WidgetsAnalyticsWorker.SuggestionsWidgetInstallStateParameter, installed)
                .Build();

            WidgetsAnalyticsWorker.EnqueueWork(context, inputData);
        }
    }
}
