using System;
using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Toggl.Core;
using Toggl.Core.Calendar;
using Toggl.Core.UI.Calendar;
using Toggl.Droid.Extensions;
using Toggl.Droid.ViewHelpers;
using Constants = Toggl.Core.Helper.Constants;

namespace Toggl.Droid.Views.Calendar
{
    [Register("toggl.droid.views.CalendarDayView")]
    public partial class CalendarDayView : View
    {
        private const int hoursPerDay = Constants.HoursPerDay;

        private GestureDetector gestureDetector;
        private OverScroller scroller;
        private Handler handler;
        private ITimeService timeService;
        private CalendarLayoutCalculator calendarLayoutCalculator;

        private int scrollOffset;
        private bool isScrolling;
        private bool flingWasCalled;

        private float availableWidth;
        private int hourHeight;
        private int maxHeight;
        private RectF tapCheckRectF = new RectF();

        private ImmutableList<CalendarItem> calendarItems = ImmutableList<CalendarItem>.Empty;
        private ImmutableList<CalendarItemRectAttributes> calendarItemLayoutAttributes = ImmutableList<CalendarItemRectAttributes>.Empty;
        
        private readonly ISubject<CalendarItem> calendarItemTappedSubject = new Subject<CalendarItem>();
        public IObservable<CalendarItem> CalendarItemTappedObservable
            => calendarItemTappedSubject.AsObservable();

        private readonly ISubject<DateTimeOffset> emptySpansTouchedObservable = new Subject<DateTimeOffset>();

        public IObservable<DateTimeOffset> EmptySpansTouchedObservable
            => emptySpansTouchedObservable.AsObservable();
        
        #region Constructors

        protected CalendarDayView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public CalendarDayView(Context context) : base(context)
        {
            init();
        }

        public CalendarDayView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            init();
        }

        public CalendarDayView(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
        {
            init();
        }

        public CalendarDayView(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context, attrs, defStyleAttr, defStyleRes)
        {
            init();
        }

        #endregion

        private void init()
        {
            timeService = AndroidDependencyContainer.Instance.TimeService;
            calendarLayoutCalculator = new CalendarLayoutCalculator(timeService);
            gestureDetector = new GestureDetector(Context, new CalendarGestureListener(onTouchDown, scrollView, flingView, onSingleTapUp));
            scroller = new OverScroller(Context);
            handler = new Handler(Looper.MainLooper);
            hourHeight = 56.DpToPixels(Context);
            maxHeight = hourHeight * 24;

            initBackgroundBackingFields();
            initEventDrawingBackingFields();
        }

        partial void initBackgroundBackingFields();
        partial void initEventDrawingBackingFields();
        
        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);
            
            canvas.Save();
            canvas.Translate(0f, -scrollOffset);
            
            canvas.ClipRect(0, scrollOffset, Width, Height + scrollOffset);
            drawHourLines(canvas);
            drawCalendarItems(canvas);

            canvas.Restore();
        }

        partial void drawHourLines(Canvas canvas);
        partial void drawCalendarItems(Canvas canvas);
        
        protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
        {
            base.OnLayout(changed, left, top, right, bottom);
            availableWidth = Width - leftMargin;
            processBackgroundOnLayout(changed, left, top, right, bottom);
            processEventsOnLayout(changed, left, top, right, bottom);
        }

        partial void processBackgroundOnLayout(bool changed, int left, int top, int right, int bottom);
        partial void processEventsOnLayout(bool changed, int left, int top, int right, int bottom);

        private void continueScroll()
        {
            isScrolling = isScrolling && scroller.ComputeScrollOffset();
            if (!isScrolling)
            {
                Invalidate();
                return;
            }

            if (scroller.CurrY == scrollOffset)
            {
                handler.Post(continueScroll);
                return;
            }

            scrollOffset = scroller.CurrY;

            if (scrollOffset < 0)
            {
                scrollOffset = 0;
            }
            else if (maxHeight - scrollOffset < Height)
            {
                scrollOffset = maxHeight - Height;
            }

            handler.Post(continueScroll);
            Invalidate();
        }

        private void onTouchDown(MotionEvent e1)
        {
            scroller.ForceFinished(true);
            flingWasCalled = false;
            handler.RemoveCallbacks(continueScroll);
            Invalidate();
        }

        private void onSingleTapUp(MotionEvent e1)
        {
            var calendarItemsAvailableDuringTouch = calendarItems;
            var itemsToSearch = calendarItemLayoutAttributes;
            var touchX = e1.GetX();    
            var touchY = e1.GetY();
            
            for (var i = 0; i < itemsToSearch.Count; i++)
            {
                var calendarItemAttr = itemsToSearch[i];
                calendarItemAttr.CalculateRect(hourHeight, minHourHeight, tapCheckRectF);

                if (!tapCheckRectF.Contains(touchX, touchY + scrollOffset)) continue;
                calendarItemTappedSubject.OnNext(calendarItemsAvailableDuringTouch[i]);
                return;
            }
            
            var today = timeService.CurrentDateTime.Date;
            var touchAtOffset = today.AddHours((touchY + scrollOffset) / hourHeight);
            emptySpansTouchedObservable.OnNext(touchAtOffset);
        }

        private void scrollView(MotionEvent e1, MotionEvent e2, float deltaX, float deltaY)
        {
            scrollOffset += (int) deltaY;

            if (scrollOffset < 0)
            {
                scrollOffset = 0;
            }
            else if (maxHeight - scrollOffset < Height)
            {
                scrollOffset = maxHeight - Height;
            }

            isScrolling = true;
            PostInvalidate();
        }

        private void flingView(MotionEvent e1, MotionEvent e2, float velocityX, float velocityY)
        {
            scroller.ForceFinished(true);

            flingWasCalled = true;
            isScrolling = true;
            scroller.Fling(0, scrollOffset, 0, (int) (-velocityY / 2f), 0, 0, 0, maxHeight);

            handler.Post(continueScroll);
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            switch (e.Action)
            {
                case MotionEventActions.Down:
                    gestureDetector.OnTouchEvent(e);
                    return true;

                case MotionEventActions.Up:
                    gestureDetector.OnTouchEvent(e);
                    if (flingWasCalled)
                        return true;
                    if (!isScrolling)
                        return true;

                    isScrolling = false;
                    Invalidate();
                    return true;

                case MotionEventActions.Move:
                    gestureDetector.OnTouchEvent(e);
                    return true;

                case MotionEventActions.Cancel:
                    gestureDetector.OnTouchEvent(e);
                    isScrolling = false;
                    return true;

                default:
                    return gestureDetector.OnTouchEvent(e) || base.OnTouchEvent(e);
            }
        }

        private class CalendarGestureListener : GestureDetector.SimpleOnGestureListener
        {
            private readonly Action<MotionEvent> onDown;
            private readonly Action<MotionEvent, MotionEvent, float, float> onScroll;
            private readonly Action<MotionEvent, MotionEvent, float, float> onFling;
            private readonly Action<MotionEvent> onSingleTapUp;

            public CalendarGestureListener(Action<MotionEvent> onDown,
                Action<MotionEvent, MotionEvent, float, float> onScroll,
                Action<MotionEvent, MotionEvent, float, float> onFling,
                Action<MotionEvent> onSingleTapUp)
            {
                this.onSingleTapUp = onSingleTapUp;
                this.onFling = onFling;
                this.onScroll = onScroll;
                this.onDown = onDown;
            }

            public override bool OnDown(MotionEvent e)
            {
                onDown(e);
                return true;
            }

            public override bool OnFling(MotionEvent e1, MotionEvent e2, float velocityX, float velocityY)
            {
                onFling(e1, e2, velocityX, velocityY);
                return true;
            }

            public override bool OnScroll(MotionEvent e1, MotionEvent e2, float distanceX, float distanceY)
            {
                onScroll(e1, e2, distanceX, distanceY);
                return true;
            }

            public override bool OnSingleTapUp(MotionEvent e)
            {
                onSingleTapUp(e);
                return true;
            }
        }
    }
}