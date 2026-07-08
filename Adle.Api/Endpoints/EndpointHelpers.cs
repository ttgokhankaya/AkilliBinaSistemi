using Adle.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Adle.Api.Endpoints;

internal static class EndpointHelpers
{
    /// <summary>Commits and maps DB constraint failures (FK, unique) to 400.</summary>
    public static async Task<IResult> SaveThen(AdleDbContext db, Func<IResult> onSuccess)
    {
        try
        {
            await db.SaveChangesAsync();
            return onSuccess();
        }
        catch (DbUpdateException ex)
        {
            return Results.BadRequest(new { error = ex.GetBaseException().Message });
        }
    }
}
