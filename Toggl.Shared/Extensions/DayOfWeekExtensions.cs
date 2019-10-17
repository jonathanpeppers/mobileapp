using System;
using System.Globalization;

namespace Toggl.Shared.Extensions
{
    public static class DayOfWeekExtensions
    {
        public static string Initial(this DayOfWeek dayOfWeek)
            => CultureInfo.CurrentCulture.DateTimeFormat.ShortestDayNames[(int)dayOfWeek];

        public static string FullName(this DayOfWeek dayOfWeek)
            => CultureInfo.CurrentCulture.DateTimeFormat.DayNames[(int)dayOfWeek];
    }
}
