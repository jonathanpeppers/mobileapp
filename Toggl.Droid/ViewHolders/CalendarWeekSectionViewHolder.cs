using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Android.Content;
using Android.Graphics;
using Android.Support.Constraints;
using Android.Views;
using Android.Widget;
using Toggl.Core.UI.ViewModels.Calendar;
using Toggl.Droid.Extensions;
using Toggl.Droid.Extensions.Reactive;
using Toggl.Shared.Extensions;

namespace Toggl.Droid.ViewHolders
{
    public class CalendarWeekSectionViewHolder
    {
        private CompositeDisposable disposableBag = new CompositeDisposable();
        private readonly InputAction<CalendarWeeklyViewDayViewModel> dayInputAction;
        private readonly ConstraintLayout rootView;
        private readonly TextView[] dayTextViews;
        private readonly View currentDayIndicator;

        private DateTime currentlySelectedDate;
        private ImmutableList<CalendarWeeklyViewDayViewModel> currentWeekSection = ImmutableList<CalendarWeeklyViewDayViewModel>.Empty;

        public CalendarWeekSectionViewHolder(ConstraintLayout view, InputAction<CalendarWeeklyViewDayViewModel> dayInputAction)
        {
            this.dayInputAction = dayInputAction;
            rootView = view;
            dayTextViews = view.GetChildren<TextView>().ToArray();
            currentDayIndicator = view.FindViewById(Resource.Id.CurrentDayIndicator);

            for (var dayIndex = 0; dayIndex < dayTextViews.Length; dayIndex++)
            {
                var index = dayIndex;
                dayTextViews[dayIndex].Rx().Tap()
                    .Select(_ => index)
                    .Subscribe(onDayClicked)
                    .DisposedBy(disposableBag);
            }
        }

        private void onDayClicked(int dayIndex)
        {
            if (currentWeekSection.Count <= dayIndex)
                return;

            var dayViewModel = currentWeekSection[dayIndex];
            if (!dayViewModel.Enabled)
                return;

            dayInputAction.Inputs.OnNext(dayViewModel);
        }

        public void InitDaysAndSelectedDate(ImmutableList<CalendarWeeklyViewDayViewModel> weekSection, DateTime newSelectedDate)
        {
            currentlySelectedDate = newSelectedDate;
            currentWeekSection = weekSection;
            updateView();
        }

        public void UpdateCurrentlySelectedDate(DateTime newSelectedDate)
        {
            currentlySelectedDate = newSelectedDate;
            updateView();
        }

        public void UpdateDays(ImmutableList<CalendarWeeklyViewDayViewModel> weekSection)
        {
            currentWeekSection = weekSection;
            updateView();
        }

        private void updateView()
        {
            var weekSection = currentWeekSection;
            var foundCurrentDay = false;
            var constraintSet = new ConstraintSet();
            constraintSet.Clone(rootView);

            for (var i = 0; i < weekSection.Count; i++)
            {
                var dayViewModel = weekSection[i];
                var dayTextView = dayTextViews[i];
                dayTextView.Text = dayViewModel.Date.Day.ToString();
                dayTextView.SetTextColor(selectTextColorFor(rootView.Context, dayViewModel));

                if (dayViewModel.Date != currentlySelectedDate)
                    continue;

                constraintSet.Connect(currentDayIndicator.Id, ConstraintSet.Top, dayTextView.Id, ConstraintSet.Top);
                constraintSet.Connect(currentDayIndicator.Id, ConstraintSet.Right, dayTextView.Id, ConstraintSet.Right);
                constraintSet.Connect(currentDayIndicator.Id, ConstraintSet.Bottom, dayTextView.Id, ConstraintSet.Bottom);
                constraintSet.Connect(currentDayIndicator.Id, ConstraintSet.Left, dayTextView.Id, ConstraintSet.Left);
                foundCurrentDay = true;
            }

            constraintSet.SetVisibility(currentDayIndicator.Id, (int) foundCurrentDay.ToVisibility());
            constraintSet.ApplyTo(rootView);
        }

        private Color selectTextColorFor(Context context, CalendarWeeklyViewDayViewModel calendarWeeklyViewDayViewModel)
        {
            if (!calendarWeeklyViewDayViewModel.Enabled)
                return context.SafeGetColor(Resource.Color.weekStripeDisabledDayColor);

            if (calendarWeeklyViewDayViewModel.Date == currentlySelectedDate)
                return Color.White;

            return calendarWeeklyViewDayViewModel.IsToday
                ? context.SafeGetColor(Resource.Color.accent)
                : context.SafeGetColor(Resource.Color.primaryText);
        }

        public void Destroy()
        {
            disposableBag.Dispose();
            disposableBag = new CompositeDisposable();
        }
    }
}