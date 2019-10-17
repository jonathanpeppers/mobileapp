﻿using System;
using Foundation;
using Toggl.Core.UI.ViewModels.Calendar;
using Toggl.iOS.Extensions;
using UIKit;
using Toggl.Core.UI.Helper;

namespace Toggl.iOS.Cells.Calendar
{
    public sealed partial class CalendarWeeklyViewDayCollectionViewCell : BaseCollectionViewCell<CalendarWeeklyViewDayViewModel>
    {
        public static readonly NSString Key = new NSString(nameof(CalendarWeeklyViewDayCollectionViewCell));
        public static readonly UINib Nib;

        private DateTime selectedDate;

        private bool isSelected => selectedDate == Item.Date;

        static CalendarWeeklyViewDayCollectionViewCell()
        {
            Nib = UINib.FromName(nameof(CalendarWeeklyViewDayCollectionViewCell), NSBundle.MainBundle);
        }

        protected CalendarWeeklyViewDayCollectionViewCell(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        protected override void UpdateView()
        {
            DayOfMonthLabel.TextColor = colorForDayOfMonthLabel();
            DayOfMonthLabel.Text = Item.Date.Day.ToString();
            IsSelectedDayBackgroundView.Hidden = !isSelected;
            IsSelectedDayBackgroundView.BackgroundColor = isSelectedDayBackgroundViewColor();

            var fontWeight = isSelected ? UIFontWeight.Semibold : UIFontWeight.Regular;
            DayOfMonthLabel.Font = UIFont.SystemFontOfSize(DayOfMonthLabel.Font.PointSize, fontWeight);
        }

        private UIColor colorForDayOfMonthLabel()
        {
            if (isSelected)
                return Colors.Calendar.WeekView.SelectedDayTextColor.ToNativeColor();

            if (Item.IsToday)
                return Colors.Calendar.WeekView.UnselectedTodayTextColor.ToNativeColor();

            if (!Item.Enabled)
                return Colors.Calendar.WeekView.UnavailableDayTextColor.ToNativeColor();

            return Colors.Calendar.WeekView.DefaultDayTextColor.ToNativeColor();
        }

        private UIColor isSelectedDayBackgroundViewColor()
        {
            if (Item.IsToday)
                return Colors.Calendar.WeekView.SelectedTodayBackgroundColor.ToNativeColor();

            return Colors.Calendar.WeekView.SelectedDayBackgroundColor.ToNativeColor();
        }

        public void UpdateSelectedDate(DateTime date)
        {
            selectedDate = date;
        }
    }
}

