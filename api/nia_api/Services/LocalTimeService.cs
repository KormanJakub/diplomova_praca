namespace nia_api.Services;

public class LocalTimeService
{
    public static DateTime LocalTime()
    {
        var utcNow = DateTime.UtcNow;
        var gmtPlus2 = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
        var localTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, gmtPlus2);
        return localTime;
    }
}