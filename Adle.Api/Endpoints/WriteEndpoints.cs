using Adle.Api.Data;
using Adle.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Adle.Api.Endpoints;

/// <summary>
/// P3 write paths: create/update/delete for areas, devices and operations.
/// Kept out of Program.cs to keep the composition root readable. FK/uniqueness
/// violations surface as 400 rather than 500.
/// </summary>
public static class WriteEndpoints
{
    public static void MapWriteEndpoints(this WebApplication app)
    {
        MapAreas(app);
        MapDevices(app);
        MapOperations(app);
    }

    private static void MapAreas(WebApplication app)
    {
        app.MapPost("/api/areas", async (AreaWriteDto dto, AdleDbContext db) =>
        {
            var area = new Area
            {
                Name = dto.Name,
                Width = dto.Width,
                Height = dto.Height,
                AreaId = dto.ParentAreaId,
                AreaTypeId = dto.AreaTypeId,
            };
            db.Areas.Add(area);
            return await SaveThen(db, () => Results.Created($"/api/areas/{area.Id}", ToDto(area)));
        }).WithName("CreateArea").WithOpenApi();

        app.MapPut("/api/areas/{id:int}", async (int id, AreaWriteDto dto, AdleDbContext db) =>
        {
            var area = await db.Areas.FindAsync(id);
            if (area is null) return Results.NotFound();
            area.Name = dto.Name;
            area.Width = dto.Width;
            area.Height = dto.Height;
            area.AreaId = dto.ParentAreaId;
            area.AreaTypeId = dto.AreaTypeId;
            return await SaveThen(db, () => Results.Ok(ToDto(area)));
        }).WithName("UpdateArea").WithOpenApi();

        app.MapDelete("/api/areas/{id:int}", async (int id, AdleDbContext db) =>
        {
            var area = await db.Areas.FindAsync(id);
            if (area is null) return Results.NotFound();
            db.Areas.Remove(area);
            return await SaveThen(db, () => Results.NoContent());
        }).WithName("DeleteArea").WithOpenApi();
    }

    private static void MapDevices(WebApplication app)
    {
        app.MapPost("/api/devices", async (DeviceWriteDto dto, AdleDbContext db) =>
        {
            var device = new DeviceBasis
            {
                Name = dto.Name,
                Ip = dto.Ip,
                State = dto.State,
                Discriminator = dto.Kind,
                AreaId = dto.AreaId,
            };
            db.DeviceBases.Add(device);
            return await SaveThen(db, () => Results.Created($"/api/devices/{device.Id}", ToDto(device)));
        }).WithName("CreateDevice").WithOpenApi();

        app.MapPut("/api/devices/{id:int}", async (int id, DeviceWriteDto dto, AdleDbContext db) =>
        {
            var device = await db.DeviceBases.FindAsync(id);
            if (device is null) return Results.NotFound();
            device.Name = dto.Name;
            device.Ip = dto.Ip;
            device.State = dto.State;
            device.Discriminator = dto.Kind;
            device.AreaId = dto.AreaId;
            return await SaveThen(db, () => Results.Ok(ToDto(device)));
        }).WithName("UpdateDevice").WithOpenApi();

        app.MapDelete("/api/devices/{id:int}", async (int id, AdleDbContext db) =>
        {
            var device = await db.DeviceBases.FindAsync(id);
            if (device is null) return Results.NotFound();
            db.DeviceBases.Remove(device);
            return await SaveThen(db, () => Results.NoContent());
        }).WithName("DeleteDevice").WithOpenApi();
    }

    private static void MapOperations(WebApplication app)
    {
        app.MapPost("/api/operations", async (OperationWriteDto dto, AdleDbContext db) =>
        {
            var op = new Operation
            {
                Name = dto.Name,
                StartTime = dto.StartTime,
                Duration = TimeSpan.FromSeconds(dto.DurationSeconds),
            };
            db.Operations.Add(op);
            return await SaveThen(db, () => Results.Created($"/api/operations/{op.Id}", ToDto(op)));
        }).WithName("CreateOperation").WithOpenApi();

        app.MapPut("/api/operations/{id:int}", async (int id, OperationWriteDto dto, AdleDbContext db) =>
        {
            var op = await db.Operations.FindAsync(id);
            if (op is null) return Results.NotFound();
            op.Name = dto.Name;
            op.StartTime = dto.StartTime;
            op.Duration = TimeSpan.FromSeconds(dto.DurationSeconds);
            return await SaveThen(db, () => Results.Ok(ToDto(op)));
        }).WithName("UpdateOperation").WithOpenApi();

        app.MapDelete("/api/operations/{id:int}", async (int id, AdleDbContext db) =>
        {
            var op = await db.Operations.FindAsync(id);
            if (op is null) return Results.NotFound();
            db.Operations.Remove(op);
            return await SaveThen(db, () => Results.NoContent());
        }).WithName("DeleteOperation").WithOpenApi();
    }

    /// <summary>Commits and maps DB constraint failures (FK, unique) to 400.</summary>
    private static async Task<IResult> SaveThen(AdleDbContext db, Func<IResult> onSuccess)
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

    private static AreaDto ToDto(Area a) =>
        new(a.Id, a.Name, a.Width, a.Height, a.AreaId, a.AreaTypeId, a.AreaType?.Name);

    private static DeviceDto ToDto(DeviceBasis d) =>
        new(d.Id, d.Name, d.Ip, d.State, d.Discriminator, d.AreaId, d.Area?.Name);

    private static OperationDto ToDto(Operation o) =>
        new(o.Id, o.Name, o.StartTime, o.Duration.TotalSeconds);
}
