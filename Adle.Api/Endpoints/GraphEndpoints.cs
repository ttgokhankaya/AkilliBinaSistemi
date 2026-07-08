using Adle.Api.Data;
using Adle.Contracts;
using AdleGraph;
using AdleGraph.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Adle.Api.Endpoints;

/// <summary>
/// P3 graph build + run. CRUD persists graphs as TSV adjacency matrices
/// (GraphObjects.MatrixValue); /run rebuilds the graph via the platform-
/// independent AdleGraph core and generates random walks — the same
/// Graph.Run the WPF app uses, minus the device invocation.
/// </summary>
public static class GraphEndpoints
{
    public static void MapGraphEndpoints(this WebApplication app)
    {
        app.MapGet("/api/graphs", async (AdleDbContext db) =>
            await db.GraphObjects
                .AsNoTracking()
                .OrderBy(g => g.Id)
                .Select(g => new GraphDto(g.Id, g.Name, g.MatrixValue))
                .ToListAsync())
            .WithName("GetGraphs").WithOpenApi();

        app.MapGet("/api/graphs/{id:int}", async (int id, AdleDbContext db) =>
        {
            var g = await db.GraphObjects.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            return g is null ? Results.NotFound() : Results.Ok(new GraphDto(g.Id, g.Name, g.MatrixValue));
        }).WithName("GetGraph").WithOpenApi();

        app.MapPost("/api/graphs", async (GraphWriteDto dto, AdleDbContext db) =>
        {
            var g = new GraphObject { Name = dto.Name, MatrixValue = dto.MatrixValue };
            db.GraphObjects.Add(g);
            return await EndpointHelpers.SaveThen(db,
                () => Results.Created($"/api/graphs/{g.Id}", new GraphDto(g.Id, g.Name, g.MatrixValue)));
        }).WithName("CreateGraph").WithOpenApi();

        app.MapPut("/api/graphs/{id:int}", async (int id, GraphWriteDto dto, AdleDbContext db) =>
        {
            var g = await db.GraphObjects.FindAsync(id);
            if (g is null) return Results.NotFound();
            g.Name = dto.Name;
            g.MatrixValue = dto.MatrixValue;
            return await EndpointHelpers.SaveThen(db, () => Results.Ok(new GraphDto(g.Id, g.Name, g.MatrixValue)));
        }).WithName("UpdateGraph").WithOpenApi();

        app.MapDelete("/api/graphs/{id:int}", async (int id, AdleDbContext db) =>
        {
            var g = await db.GraphObjects.FindAsync(id);
            if (g is null) return Results.NotFound();
            db.GraphObjects.Remove(g);
            return await EndpointHelpers.SaveThen(db, () => Results.NoContent());
        }).WithName("DeleteGraph").WithOpenApi();

        app.MapPost("/api/graphs/{id:int}/run", async (int id, RunGraphRequest req, AdleDbContext db) =>
        {
            var g = await db.GraphObjects.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (g is null) return Results.NotFound();
            if (string.IsNullOrWhiteSpace(g.MatrixValue))
                return Results.BadRequest(new { error = "Graph has no matrix to run." });

            // The AdleGraph matrix parser splits on \r\n; normalize so matrices
            // stored with plain \n (e.g. edited in a browser textarea) still run.
            var matrix = g.MatrixValue.Replace("\r\n", "\n").Replace("\n", "\r\n");
            IGraph graph = Graph.CreateNewGraphFromMatrixString(matrix, g.Name ?? "");
            var start = graph.GetNodeWithName(req.StartNode);
            var end = graph.GetNodeWithName(req.EndNode);
            if (start is null || end is null)
                return Results.BadRequest(new { error = "Start or end node not found in graph." });

            var walks = graph.Run(start, end, req.UseEdgeWeights, req.Iterations, req.AllowLoops);
            var mapped = walks.Select(w => w.Select(n => n.Name).ToArray()).ToArray();
            return Results.Ok(new RunGraphResponse(mapped.Length, mapped));
        }).WithName("RunGraph").WithOpenApi()
          .Produces<RunGraphResponse>(200);
    }
}
