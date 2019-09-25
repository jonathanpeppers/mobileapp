using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Text;
using Android.Util;
using Android.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using Toggl.Core.Reports;
using Toggl.Droid.Extensions;
using Point = Toggl.Shared.Point;

namespace Toggl.Droid.Views
{
    [Register("toggl.droid.views.PieChartView")]
    public sealed class PieChartView : View
    {
        private int padding;
        private double linesSeparatorHeight;
        private const float fullCircle = 360.0f;
        private const double radToDegree = 180 / Math.PI;
        private readonly TextPaint textPaint = new TextPaint();
        private Point nameCoordinates = new Point();
        private Point percentageCoordinates = new Point();
        private readonly Rect bounds = new Rect();

        private IEnumerable<ChartSegment> segments = new ChartSegment[0];

        public IEnumerable<ChartSegment> Segments
        {
            get => segments;
            set
            {
                segments = value;
                PostInvalidate();
            }
        }

        public PieChartView(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        public PieChartView(Context context)
            : base(context)
        {
            initialize(context);
        }

        public PieChartView(Context context, IAttributeSet attrs)
            : base(context, attrs)
        {
            initialize(context);
        }

        private void initialize(Context context)
        {
            padding = 16.DpToPixels(context);
            linesSeparatorHeight = 0.5 * padding;

            textPaint.Color = Color.White;
            textPaint.TextAlign = Paint.Align.Left;
            textPaint.TextSize = 10.SpToPixels(context);
            textPaint.AntiAlias = true;
            textPaint.SetTypeface(Typeface.Create("sans-serif", TypefaceStyle.Bold));
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            base.OnMeasure(widthMeasureSpec, widthMeasureSpec);
        }

        private bool isSegmentOnTheRight(float endDegrees) => endDegrees > 270 || (endDegrees >= 0 && endDegrees <= 90);

        private (int width, int height) getTextWidthAndHeight(string text)
        {
            textPaint.GetTextBounds(text, 0, text.Length, bounds);

            return (bounds.Width(), bounds.Height());
        }

        protected override void OnDraw(Canvas canvas)
        {
            var viewCenterX = Width * 0.5f;
            var viewCenterY = Height * 0.5f;
            var radius = viewCenterX;
            var totalSeconds = (float) Segments.Select(x => x.TrackedTime.TotalSeconds).Sum();

            var startDegrees = 270.0f;

            var oval = new RectF(0, 0, Width, Height);

            foreach (var segment in Segments)
            {
                var segmentPaint = new Paint();
                segmentPaint.Color = Color.ParseColor(segment.Color);
                segmentPaint.AntiAlias = true;

                var percent = (float) segment.TrackedTime.TotalSeconds / totalSeconds;
                var sweepDegrees = fullCircle * percent;
                var endDegrees = (startDegrees + sweepDegrees) % fullCircle;

                // Draw arc
                canvas.DrawArc(oval, startDegrees, sweepDegrees, true, segmentPaint);

                // Disable drawing on segments that are too small
                if (ProjectSummaryReport.ShouldDraw(percent))
                {
                    // Save state for restoring later.
                    canvas.Save();

                    var isOnTheRight = isSegmentOnTheRight(endDegrees);
                    var integerPercentage = (int) (percent * 100);
                    var nameToDraw = segment.FormattedName();
                    var percentageToDraw = $"{integerPercentage}%";

                    var (textWidth, textHeight) = getTextWidthAndHeight(nameToDraw);
                    var (percentWidth, percentHeight) = getTextWidthAndHeight(percentageToDraw);

                    // Translate to draw the text
                    canvas.Translate(viewCenterX, viewCenterY);
                    if (isOnTheRight)
                    {
                        canvas.Rotate(endDegrees >= 0 ? endDegrees : -endDegrees);

                        nameCoordinates.X = radius - padding - textWidth;
                        nameCoordinates.Y = -padding + textHeight;

                        percentageCoordinates.X = radius - padding - percentWidth;
                        percentageCoordinates.Y = -(padding + linesSeparatorHeight) + -textHeight + percentHeight;
                    }
                    else
                    {
                        canvas.Rotate(endDegrees + 180.0f);

                        nameCoordinates.X = -radius + padding;
                        nameCoordinates.Y = padding;

                        percentageCoordinates.X = -radius + padding;
                        percentageCoordinates.Y = textHeight + padding + linesSeparatorHeight;
                    }

                    canvas.DrawText(nameToDraw, (float) nameCoordinates.X, (float) nameCoordinates.Y, textPaint);
                    canvas.DrawText(percentageToDraw, (float) percentageCoordinates.X, (float) percentageCoordinates.Y, textPaint);

                    // Restore the original coordinate system.
                    canvas.Restore();
                }

                startDegrees = endDegrees;
            }
        }
    }
}