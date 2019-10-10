using Android.OS;
using Android.Views;
using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Support.V4.View;
using Toggl.Core;
using Toggl.Core.UI.ViewModels.Calendar;
using Toggl.Droid.Activities;
using Toggl.Droid.Extensions;
using Toggl.Droid.Fragments.Calendar;
using Toggl.Droid.Helper;
using Toggl.Droid.Presentation;
using Toggl.Droid.Views.Calendar;
using Toggl.Shared.Extensions;
using Toggl.Shared.Extensions.Reactive;

namespace Toggl.Droid.Fragments
{
    public partial class CalendarFragment : ReactiveTabFragment<CalendarViewModel>, IScrollableToStart
    {
        private const int calendarPagesCount = 14;
        private readonly Subject<bool> scrollToStartSignaler = new Subject<bool>();
        private CalendarDayFragmentAdapter calendarDayAdapter;
        private ITimeService timeService;
        private int defaultToolbarElevationInDPs;
        private bool hasResumedOnce = false;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.CalendarFragment, container, false);
            InitializeViews(view);
            timeService = AndroidDependencyContainer.Instance.TimeService;
            defaultToolbarElevationInDPs = 4.DpToPixels(Context);
            return view;
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);

            calendarDayAdapter = new CalendarDayFragmentAdapter(ViewModel, scrollToStartSignaler, ChildFragmentManager);
            calendarViewPager.Adapter = calendarDayAdapter;
            calendarViewPager.AddOnPageChangeListener(calendarDayAdapter);
            calendarViewPager.SetPageTransformer(false, new VerticalOffsetPageTransformer(calendarDayAdapter.OffsetRelay));
            calendarDayAdapter.CurrentPageRelay
                .Select(getDateAtAdapterPosition)
                .Subscribe(configureHeaderDate)
                .DisposedBy(DisposeBag);

            calendarDayAdapter.OffsetRelay
                .Select(offset => offset == 0)
                .DistinctUntilChanged()
                .Subscribe(updateAppbarElevation)
                .DisposedBy(DisposeBag);

            calendarDayAdapter.MenuVisibilityRelay
                .Subscribe(swipeIsLocked => calendarViewPager.IsLocked = swipeIsLocked)
                .DisposedBy(DisposeBag);
            
            calendarDayAdapter.MenuVisibilityRelay
                .Select(CommonFunctions.Invert)
                .Subscribe(hideBottomBar)
                .DisposedBy(DisposeBag);

            calendarViewPager.SetCurrentItem(calendarPagesCount - 1, false);
        }

        private void hideBottomBar(bool bottomBarShouldBeHidden)
        {
            (Activity as MainTabBarActivity)?.ChangeBottomBarVisibility(bottomBarShouldBeHidden);
            calendarDayAdapter?.InvalidateCurrentPage();
        }

        public void ScrollToStart()
        {
            if (calendarDayAdapter?.MenuVisibilityRelay.Value == true)
                return;
            
            scrollToStartSignaler.OnNext(true);
            calendarViewPager.SetCurrentItem(calendarPagesCount - 1, true);
        }

        public override void OnResume()
        {
            base.OnResume();
            if (hasResumedOnce) 
                return;
            hasResumedOnce = true;
            
            if (calendarDayAdapter?.MenuVisibilityRelay.Value == true)
                return;
            
            scrollToStartSignaler.OnNext(false);
        }

        private void configureHeaderDate(DateTimeOffset offset)
        {
            var dayText = offset.ToString(Shared.Resources.CalendarToolbarDateFormat);
            headerDateTextView.Text = dayText;
        }

        private DateTimeOffset getDateAtAdapterPosition(int position)
        {
            var currentDate = timeService.CurrentDateTime.ToLocalTime().Date;
            return currentDate.AddDays(-(calendarDayAdapter.Count - 1 - position));
        }

        private void updateAppbarElevation(bool isAtTop)
        {
            if (MarshmallowApis.AreNotAvailable)
                return;
            
            var targetElevation = isAtTop ? 0f : defaultToolbarElevationInDPs;
            appBarLayout.Elevation = targetElevation;
        }
        
        private class VerticalOffsetPageTransformer : Java.Lang.Object, ViewPager.IPageTransformer
        {
            private BehaviorRelay<int> verticalOffsetProvider { get; }

            public VerticalOffsetPageTransformer(BehaviorRelay<int> verticalOffsetProvider)
            {
                this.verticalOffsetProvider = verticalOffsetProvider;
            }

            public void TransformPage(View page, float position)
            {
                var calendarDayView = page.FindViewById<CalendarDayView>(Resource.Id.CalendarDayView);
                calendarDayView?.SetOffset(verticalOffsetProvider.Value);
            }
        }

        private class CalendarDayFragmentAdapter : FragmentStatePagerAdapter, ViewPager.IOnPageChangeListener
        {
            private readonly CalendarViewModel calendarViewModel;
            private readonly IObservable<bool> scrollToTopSign;
            private readonly ISubject<Unit> pageNeedsToBeInvalidated = new Subject<Unit>(); 
            public BehaviorRelay<int> OffsetRelay { get; } = new BehaviorRelay<int>(0);
            public BehaviorRelay<int> CurrentPageRelay { get; } = new BehaviorRelay<int>(0);
            public BehaviorRelay<bool> MenuVisibilityRelay { get; } = new BehaviorRelay<bool>(false);

            public CalendarDayFragmentAdapter(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
            {
            }

            public CalendarDayFragmentAdapter(CalendarViewModel calendarViewModel, IObservable<bool> scrollToTopSign, FragmentManager fm) : base(fm)
            {
                this.calendarViewModel = calendarViewModel;
                this.scrollToTopSign = scrollToTopSign;
            }

            public override int Count { get; } = calendarPagesCount;

            public override Fragment GetItem(int position)
                => new CalendarDayViewPageFragment
                {
                    ViewModel = calendarViewModel.DayViewModelAt(-(Count - 1 - position)),
                    ScrollOffsetRelay = OffsetRelay,
                    CurrentPageRelay = CurrentPageRelay,
                    MenuVisibilityRelay =  MenuVisibilityRelay,
                    PageNumber = position,
                    ScrollToStartSign = scrollToTopSign,
                    InvalidationListener = pageNeedsToBeInvalidated.AsObservable()
                };

            public void InvalidateCurrentPage()
            {
                pageNeedsToBeInvalidated.OnNext(Unit.Default);
            }
            
            public void OnPageSelected(int position)
                => CurrentPageRelay.Accept(position);

            public void OnPageScrollStateChanged(int state)
            {
            }

            public void OnPageScrolled(int position, float positionOffset, int positionOffsetPixels)
            {
            }
        }
    }
}