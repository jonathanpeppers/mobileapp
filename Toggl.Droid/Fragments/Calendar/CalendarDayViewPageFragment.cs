using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Android.OS;
using Android.Support.V4.App;
using Android.Text;
using Android.Views;
using Toggl.Core.UI.ViewModels.Calendar;
using Toggl.Core.UI.ViewModels.Calendar.ContextualMenu;
using Toggl.Core.UI.Views;
using Toggl.Droid.Adapters;
using Toggl.Droid.Extensions.Reactive;
using Toggl.Droid.ViewHolders;
using Toggl.Shared.Extensions;
using Toggl.Shared.Extensions.Reactive;
using TimeEntryExtensions = Toggl.Droid.Extensions.TimeEntryExtensions;

namespace Toggl.Droid.Fragments.Calendar
{
    public partial class CalendarDayViewPageFragment : Fragment, IView
    {
        private readonly TimeSpan defaultTimeEntryDurationForCreation = TimeSpan.FromMinutes(30);
        private CompositeDisposable DisposeBag = new CompositeDisposable();
        private IDisposable dismissActionDisposeBag;
        private SimpleAdapter<CalendarMenuAction> menuActionsAdapter;

        public CalendarDayViewModel ViewModel { get; set; }
        public BehaviorRelay<int> CurrentPageRelay { get; set; }
        public BehaviorRelay<int> ScrollOffsetRelay { get; set; }
        public BehaviorRelay<bool> MenuVisibilityRelay { get; set; }
        public IObservable<bool> ScrollToStartSign { get; set; }
        public IObservable<Unit> InvalidationListener { get; set; }

        public int PageNumber { get; set; }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.CalendarDayFragmentPage, container, false);
            initializeViews(view);
            return view;
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);
            if (ViewModel == null) return;

            ViewModel.AttachView(this);
            ViewModel.ContextualMenuViewModel.AttachView(this);
            calendarDayView.SetCurrentDate(ViewModel.Date);
            calendarDayView.SetOffset(ScrollOffsetRelay?.Value ?? 0);
            calendarDayView.UpdateItems(ViewModel.CalendarItems);
            
            ViewModel.CalendarItems.CollectionChange
                .Subscribe(_ => calendarDayView.UpdateItems(ViewModel.CalendarItems))
                .DisposedBy(DisposeBag);

            calendarDayView.CalendarItemTappedObservable
                .Subscribe(item => ViewModel.ContextualMenuViewModel.OnCalendarItemUpdated.Inputs.OnNext(item))
                .DisposedBy(DisposeBag);

            calendarDayView.EmptySpansTouchedObservable
                .Select(emptySpan => (emptySpan, defaultTimeEntryDurationForCreation))
                .Subscribe(ViewModel.OnDurationSelected.Inputs)
                .DisposedBy(DisposeBag);

            calendarDayView.EditCalendarItem
                .Subscribe(ViewModel.OnTimeEntryEdited.Inputs)
                .DisposedBy(DisposeBag);

            ScrollToStartSign?
                .Subscribe(scrollToStart)
                .DisposedBy(DisposeBag);

            calendarDayView.ScrollOffsetObservable
                .Subscribe(updateScrollOffsetIfCurrentPage)
                .DisposedBy(DisposeBag);

            menuActionsAdapter = new SimpleAdapter<CalendarMenuAction>(
                Resource.Layout.ContextualMenuActionCell,
                CalendarMenuActionCellViewHolder.Create);
            
            actionsRecyclerView.SetAdapter(menuActionsAdapter);

            ViewModel.ContextualMenuViewModel
                .CurrentMenu
                .Subscribe(updateMenuBindings)
                .DisposedBy(DisposeBag);
            
            ViewModel.ContextualMenuViewModel
                .MenuVisible
                .Subscribe(contextualMenuContainer.Rx().IsVisible())
                .DisposedBy(DisposeBag);
            
            ViewModel.ContextualMenuViewModel
                .MenuVisible
                .Subscribe(notifyMenuVisibilityIfCurrentPage)
                .DisposedBy(DisposeBag);
            
            ViewModel.ContextualMenuViewModel
                .TimeEntryPeriod
                .Subscribe(periodText.Rx().TextObserver())
                .DisposedBy(DisposeBag);
            
            ViewModel.ContextualMenuViewModel
                .TimeEntryInfo
                .Select(convertTimeEntryInfoToSpannable)
                .Subscribe(timeEntryDetails.Rx().TextFormattedObserver())
                .DisposedBy(DisposeBag);
            
            InvalidationListener?
                .Subscribe(_ => invalidatePage())
                .DisposedBy(DisposeBag);
        }

        private void invalidatePage()
        {
            calendarDayView.Post(() =>
            {
                calendarDayView.Invalidate();
                contextualMenuContainer.Invalidate();
            });
        }
        
        private void updateMenuBindings(CalendarContextualMenu contextualMenu)
        {
            menuActionsAdapter.Items = contextualMenu.Actions;
            dismissActionDisposeBag?.Dispose();
            dismissActionDisposeBag = dismissButton.Rx().Tap()
                .Subscribe(contextualMenu.Dismiss.Inputs);
        }

        private void notifyMenuVisibilityIfCurrentPage(bool menuIsVisible)
        {
            if (CurrentPageRelay?.Value == PageNumber)
            {
                MenuVisibilityRelay?.Accept(menuIsVisible);
            }
        }

        private ISpannable convertTimeEntryInfoToSpannable(TimeEntryDisplayInfo timeEntryDisplayInfo)
        {
            var description = string.IsNullOrEmpty(timeEntryDisplayInfo.Description)
                ? Shared.Resources.NoDescription
                : timeEntryDisplayInfo.Description;
            var hasProject = !string.IsNullOrEmpty(timeEntryDisplayInfo.Project);
            var builder = new SpannableStringBuilder();
            var projectTaskClient = TimeEntryExtensions.ToProjectTaskClient(
                Context,
                hasProject,
                timeEntryDisplayInfo.Project,
                timeEntryDisplayInfo.ProjectTaskColor,
                timeEntryDisplayInfo.Task,
                timeEntryDisplayInfo.Client,
                false,
                false
            );
            
            builder.Append(description);
            builder.Append(" ");
            builder.Append(projectTaskClient);
            return builder;
        }

        private void updateScrollOffsetIfCurrentPage(int scrollOffset)
        {
            if (PageNumber != CurrentPageRelay?.Value) 
                return;
            
            ScrollOffsetRelay?.Accept(scrollOffset);
        }

        private void scrollToStart(bool shouldSmoothScroll)
        {
            if (PageNumber != CurrentPageRelay?.Value)
                return;
            
            calendarDayView?.ScrollToCurrentHour(shouldSmoothScroll);
        }

        public override void OnDestroyView()
        {
            ViewModel?.DetachView();
            dismissActionDisposeBag?.Dispose();
            DisposeBag.Dispose();
            DisposeBag = new CompositeDisposable();
            base.OnDestroyView();
        }
    }
}