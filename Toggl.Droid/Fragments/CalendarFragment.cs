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
using Toggl.Droid.Extensions.Reactive;
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
        public static int NumberOfDaysInTheWeek = 7;
        private const int calendarPagesCount = 14;
        private readonly Subject<bool> scrollToStartSignaler = new Subject<bool>();
        private CalendarDayFragmentAdapter calendarDayAdapter;
        private CalendarWeekStripeAdapter calendarWeekStripeAdapter;
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

            var startingCalendarDayViewPage = calculateDayViewPage(ViewModel.CurrentlyShownDate.Value);
            calendarViewPager.SetCurrentItem(startingCalendarDayViewPage, false);
            
            calendarWeekStripeAdapter = new CalendarWeekStripeAdapter(ViewModel.SelectDayFromWeekView, ViewModel.CurrentlyShownDate);
            calendarWeekStripePager.AddOnPageChangeListener(calendarWeekStripeAdapter);
            calendarWeekStripePager.Adapter = calendarWeekStripeAdapter;
            
            ViewModel.WeekViewHeaders
                .Subscribe(updateWeekViewHeaders)
                .DisposedBy(DisposeBag);
            
            ViewModel.WeekViewDays
                .Subscribe(weekDays =>
                {
                    calendarWeekStripeAdapter.UpdateWeekDays(weekDays);
                    var updatedCurrentPage = calendarWeekStripeAdapter.GetPageFor(ViewModel.CurrentlyShownDate.Value);
                    calendarWeekStripePager.SetCurrentItem(updatedCurrentPage, false);
                })
                .DisposedBy(DisposeBag);
            
            ViewModel.CurrentlyShownDate
                .Subscribe(calendarWeekStripeAdapter.UpdateSelectedDay)
                .DisposedBy(DisposeBag);
            
            ViewModel.CurrentlyShownDate
                .Select(calculateDayViewPage)
                .Subscribe(page => calendarViewPager.SetCurrentItem(page, true))
                .DisposedBy(DisposeBag);

            var startingPageForCalendarWeekPager = calendarWeekStripeAdapter.GetPageFor(ViewModel.CurrentlyShownDate.Value);
            calendarWeekStripePager.SetCurrentItem(startingPageForCalendarWeekPager, false);
            
            ViewModel.CurrentlyShownDate
                .Select(calendarWeekStripeAdapter.GetPageFor)
                .Subscribe(page => calendarWeekStripePager.SetCurrentItem(page, true))
                .DisposedBy(DisposeBag);
            
            calendarDayAdapter.CurrentPageRelay
                .DistinctUntilChanged()
                .Select(calculateDayForCalendarDayPage)
                .Subscribe(ViewModel.CurrentlyShownDate.Accept)
                .DisposedBy(DisposeBag);

            calendarWeekStripeLabelsContainer.Rx().TouchEvents()
                .Subscribe(touch => calendarWeekStripePager.OnTouchEvent(touch))
                .DisposedBy(DisposeBag);
        }

        private DateTime calculateDayForCalendarDayPage(int currentPage)
        {
            var today = AndroidDependencyContainer.Instance.TimeService.CurrentDateTime.ToLocalTime().Date;
            return today.AddDays(-(calendarPagesCount - currentPage - 1));
        }

        private int calculateDayViewPage(DateTime newDate)
        {
            var today = AndroidDependencyContainer.Instance.TimeService.CurrentDateTime.ToLocalTime().Date;
            var distanceFromToday = (today - newDate).Days;
            return calendarPagesCount - distanceFromToday - 1;
        }

        private void updateWeekViewHeaders(IImmutableList<DayOfWeek> days)
        {
            if (days.Count != NumberOfDaysInTheWeek)
                throw new ArgumentOutOfRangeException($"Week headers should contain exactly {NumberOfDaysInTheWeek} items");

            calendarWeekStripeHeaders.Indexed()
                .ForEach((textView, day) => textView.Text = days[day].Initial());
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
        
        private class CalendarWeekStripeAdapter : PagerAdapter, ViewPager.IOnPageChangeListener
        {
            private readonly InputAction<CalendarWeeklyViewDayViewModel> dayInputAction;
            private readonly BehaviorRelay<DateTime> currentlyShownDateRelay;
            private readonly Dictionary<int, CalendarWeekSectionViewHolder> pages = new Dictionary<int, CalendarWeekSectionViewHolder>();
            private ImmutableList<ImmutableList<CalendarWeeklyViewDayViewModel>> weekSections = ImmutableList<ImmutableList<CalendarWeeklyViewDayViewModel>>.Empty;
            private DateTime currentlySelectedDate = DateTime.Today;
            
            public CalendarWeekStripeAdapter(InputAction<CalendarWeeklyViewDayViewModel> dayInputAction, BehaviorRelay<DateTime> currentlyShownDateRelay)
            {
                this.dayInputAction = dayInputAction;
                this.currentlyShownDateRelay = currentlyShownDateRelay;
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

            public void OnPageScrollStateChanged(int state)
            {
            }

            public void OnPageScrolled(int position, float positionOffset, int positionOffsetPixels)
            {
            }

            public void OnPageSelected(int position)
            {
                var newDateOnSwipe = selectAppropriateNewDateOnSwipe(position);
                if (currentlySelectedDate == newDateOnSwipe)
                    return;
                
                currentlyShownDateRelay.Accept(newDateOnSwipe);
            }

            private DateTime selectAppropriateNewDateOnSwipe(int position)
            {
                var currentSections = weekSections;
                var daysOnPage = currentSections[position];
                var newDateOnSwipe = currentlySelectedDate;
                if (currentlySelectedDate > daysOnPage.Last().Date)
                {
                    newDateOnSwipe = currentlySelectedDate.AddDays(-NumberOfDaysInTheWeek);
                    newDateOnSwipe = daysOnPage.First(date => date.Enabled && date.Date >= newDateOnSwipe).Date;
                }
                else if (currentlySelectedDate < daysOnPage.First().Date)
                {
                    newDateOnSwipe = currentlySelectedDate.AddDays(NumberOfDaysInTheWeek);
                    newDateOnSwipe = daysOnPage.Last(date => date.Enabled && date.Date <= newDateOnSwipe).Date;
                }

                return newDateOnSwipe;
            }

            public void UpdateWeekDays(ImmutableList<CalendarWeeklyViewDayViewModel> newWeekDays)
            {
                weekSections = createWeekSections(newWeekDays);
                NotifyDataSetChanged();
                applyToAllPages((pageIndex, page) => page.UpdateDays(weekSections[pageIndex]));
            }

            public void UpdateSelectedDay(DateTime newSelectedDate)
            {
                currentlySelectedDate = newSelectedDate;
                applyToAllPages((pageIndex, page) => page.UpdateCurrentlySelectedDate(newSelectedDate));
            }
            
            private void applyToAllPages(Action<int, CalendarWeekSectionViewHolder> apply)
            {
                for (var pageIndex = 0; pageIndex < weekSections.Count; pageIndex++)
                {
                    pages.TryGetValue(pageIndex, out var page);
                    if (page != null)
                    {
                        apply(pageIndex, page);
                    }
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

            private CalendarWeekSectionViewHolder getPage(int position)
            {
                pages.TryGetValue(position, out var page);
                return page;
            }

            private ImmutableList<ImmutableList<CalendarWeeklyViewDayViewModel>> createWeekSections(ImmutableList<CalendarWeeklyViewDayViewModel> newWeekDays)
            {
                var weeklySections = new List<ImmutableList<CalendarWeeklyViewDayViewModel>>();
                for (var i = 0; i < newWeekDays.Count; i += NumberOfDaysInTheWeek)
                {
                    weeklySections.Add(newWeekDays.GetRange(i, NumberOfDaysInTheWeek));
                }

                return weeklySections.ToImmutableList();
            }
        }
    }
}