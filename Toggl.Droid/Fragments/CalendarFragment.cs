using Android.OS;
using Android.Views;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Android.Runtime;
using Android.Support.Constraints;
using Android.Support.V4.App;
using Android.Support.V4.View;
using Toggl.Core;
using Toggl.Core.UI.ViewModels.Calendar;
using Toggl.Droid.Extensions;
using Toggl.Droid.Fragments.Calendar;
using Toggl.Droid.Helper;
using Toggl.Droid.Presentation;
using Toggl.Droid.ViewHolders;
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
            
            calendarViewPager.SetCurrentItem(calendarPagesCount - 1, false);
            ViewModel.WeekViewHeaders
                .Subscribe(updateWeekViewHeaders)
                .DisposedBy(DisposeBag);
        }

        private void updateWeekViewHeaders(IImmutableList<DayOfWeek> days)
        {
            for (var i = 0; i < 7; i++)
                calendarWeekStripeHeaders[i].Text = days[i].Initial();
        }

        public void ScrollToStart()
        {
            scrollToStartSignaler.OnNext(true);
            calendarViewPager.SetCurrentItem(calendarPagesCount - 1, true);
        }

        public override void OnResume()
        {
            base.OnResume();
            if (hasResumedOnce) 
                return;
            hasResumedOnce = true;
            
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
                ((CalendarDayView) page).SetOffset(verticalOffsetProvider.Value);
            }
        }

        private class CalendarDayFragmentAdapter : FragmentStatePagerAdapter, ViewPager.IOnPageChangeListener
        {
            private readonly CalendarViewModel calendarViewModel;
            private readonly IObservable<bool> scrollToTopSign;
            public BehaviorRelay<int> OffsetRelay { get; } = new BehaviorRelay<int>(0);
            public BehaviorRelay<int> CurrentPageRelay { get; } = new BehaviorRelay<int>(0);

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
                    PageNumber = position,
                    ScrollToStartSign = scrollToTopSign
                };

            public void OnPageSelected(int position)
                => CurrentPageRelay.Accept(position);

            public void OnPageScrollStateChanged(int state)
            {
            }

            public void OnPageScrolled(int position, float positionOffset, int positionOffsetPixels)
            {
            }
        }
        
        private class CalendarWeekStripeAdapter : PagerAdapter
        {
            private readonly InputAction<CalendarWeeklyViewDayViewModel> dayInputAction;
            private readonly Dictionary<int, CalendarWeekSectionViewHolder> pages = new Dictionary<int, CalendarWeekSectionViewHolder>();
            private ImmutableList<ImmutableList<CalendarWeeklyViewDayViewModel>> weekSections = ImmutableList<ImmutableList<CalendarWeeklyViewDayViewModel>>.Empty;
            private DateTime currentlySelectedDate = DateTime.Today;
            
            public CalendarWeekStripeAdapter(InputAction<CalendarWeeklyViewDayViewModel> dayInputAction)
            {
                this.dayInputAction = dayInputAction;
            }

            public override Java.Lang.Object InstantiateItem(ViewGroup container, int position)
            {
                var weekStripe = (ConstraintLayout)LayoutInflater.From(container.Context).Inflate(Resource.Layout.CalendarWeekStripeDaysView, container, false);
                var weekSectionViewHolder = new CalendarWeekSectionViewHolder(weekStripe, dayInputAction);
                var weekSection = weekSections[position];
                
                weekSectionViewHolder.InitDaysAndSelectedDate(weekSection, currentlySelectedDate);

                pages[position] = weekSectionViewHolder;
                
                container.AddView(weekStripe);
                return weekStripe;
            }

            public override void DestroyItem(ViewGroup container, int position, Java.Lang.Object @object)
            {
                pages.TryGetValue(position, out var page);
                page?.Destroy();
                container.RemoveView(@object as View);
            }

            public override bool IsViewFromObject(View view, Java.Lang.Object @object) 
                => view == @object;

            public override int Count => weekSections.Count;

            public void UpdateWeekDays(ImmutableList<CalendarWeeklyViewDayViewModel> newWeekDays)
            {
                weekSections = createWeekSections(newWeekDays);
                NotifyDataSetChanged();
                for (var pageIndex = 0; pageIndex < weekSections.Count; pageIndex++)
                {
                    pages.TryGetValue(pageIndex, out var page);
                    page?.UpdateDays(weekSections[pageIndex]);
                }
            }
            
            public void UpdateSelectedDay(DateTime newSelectedDate)
            {
                currentlySelectedDate = newSelectedDate;
                for (var pageIndex = 0; pageIndex < weekSections.Count; pageIndex++)
                {
                    pages.TryGetValue(pageIndex, out var page);
                    page?.UpdateCurrentlySelectedDate(newSelectedDate);
                }
            }

            public int GetPageFor(DateTime selectedDate)
            {
                var currentWeekDays = weekSections;
                for (var page = 0; page < currentWeekDays.Count; page++)
                {
                    var weekSection = currentWeekDays[page];
                    if (selectedDate >= weekSection.First().Date && selectedDate <= weekSection.Last().Date)
                        return page;
                }

                return 0;
            }

            private ImmutableList<ImmutableList<CalendarWeeklyViewDayViewModel>> createWeekSections(ImmutableList<CalendarWeeklyViewDayViewModel> newWeekDays)
            {
                var weeklySections = new List<ImmutableList<CalendarWeeklyViewDayViewModel>>();
                for (var i = 0; i < newWeekDays.Count; i += 7)
                {
                    weeklySections.Add(newWeekDays.GetRange(i, 7));
                }

                return weeklySections.ToImmutableList();
            }
        }
    }
}