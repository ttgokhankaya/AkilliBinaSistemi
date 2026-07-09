using Adle.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Adle.Api.Endpoints;

internal static class EndpointHelpers
{
    /// <summary>Commits and maps DB constraint failures (FK, unique) to 400.</summary>
    public static Task<IResult> SaveThen(AdleDbContext db, Func<IResult> onSuccess) =>
        SaveThen(db, () => Task.FromResult(onSuccess()));

    /// <summary>Commits and maps DB constraint failures (FK, unique) to 400; async success (e.g. to load navigations).</summary>
    public static async Task<IResult> SaveThen(AdleDbContext db, Func<Task<IResult>> onSuccess)
    {
        try
        {
            await db.SaveChangesAsync();
            return await onSuccess();
        }
        catch (DbUpdateException ex)
        {
            return Results.BadRequest(new { error = ex.GetBaseException().Message });
        }
    }
}
