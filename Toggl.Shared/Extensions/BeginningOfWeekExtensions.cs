using System.Globalization;

namespace Toggl.Shared.Extensions
{
    public static class BeginningOfWeekExtensions
    {
        public static string ToLocalizedString(this BeginningOfWeek beginningOfWeek)
            => CultureInfo.CurrentCulture.DateTimeFormat.DayNames[(int)beginningOfWeek];
    }
}
