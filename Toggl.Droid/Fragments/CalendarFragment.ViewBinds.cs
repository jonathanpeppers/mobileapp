using Android.Support.Design.Widget;
using Android.Support.V4.View;
using Android.Views;
using Android.Widget;
using Toggl.Droid.Views.Calendar;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Toggl.Droid.Fragments
{
    public partial class CalendarFragment
    {
        private TextView headerTimeEntriesDurationTextView;
        private TextView headerDateTextView;
        private LockableViewPager calendarViewPager;
        private AppBarLayout appBarLayout;
        private Toolbar toolbar;

        protected override void InitializeViews(View view)
        {
            headerDateTextView = view.FindViewById<TextView>(Resource.Id.HeaderDateTextView);
            headerTimeEntriesDurationTextView = view.FindViewById<TextView>(Resource.Id.HeaderTimeEntriesDurationTextView);
            appBarLayout = view.FindViewById<AppBarLayout>(Resource.Id.HeaderView);
            calendarViewPager = view.FindViewById<LockableViewPager>(Resource.Id.Pager);
            toolbar = view.FindViewById<Toolbar>(Resource.Id.Toolbar);
        }

        private void setupToolbar()
        {
            //todo: setup on settings toolbars revamp issue
            toolbar.InflateMenu(Resource.Menu.CalendarFragmentMenu);
            var saveMenuItem = toolbar.Menu.FindItem(Resource.Id.Settings);
            saveMenuItem.SetTitle(Shared.Resources.Settings);
        }
    }
}
