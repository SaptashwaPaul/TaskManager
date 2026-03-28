namespace TaskManager.API.Helpers
{

    public static class DateTimeHelper
    {
        public static DateTime ToUtc(DateTime date)
        {
            return DateTime.SpecifyKind(date, DateTimeKind.Utc);
        }
    }

}