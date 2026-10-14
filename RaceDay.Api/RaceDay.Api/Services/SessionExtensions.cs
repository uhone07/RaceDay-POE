namespace RaceDay.Api.Services;

public static class SessionExtensions
{
    // Returns the logged-in user's ID (only call after [SessionAuthorize])
    public static int GetUserId(this ISession session) =>
        session.GetInt32("UserId") ?? 0;

    public static string GetRole(this ISession session) =>
        session.GetString("Role") ?? string.Empty;
}