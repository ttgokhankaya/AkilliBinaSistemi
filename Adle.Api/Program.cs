using Adle.Api.Data;
using Adle.Contracts;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// EF Core over PostgreSQL. Connection string comes from configuration
// ("ConnectionStrings:Adle"), overridable at runtime via ADLE_DB_CONNECTION
// to match the desktop app's convention (see CLAUDE.md).
var connectionString =
    Environment.GetEnvironmentVariable("ADLE_DB_CONNECTION")
    ?? builder.Configuration.GetConnectionString("Adle");
builder.Services.AddDbContext<AdleDbContext>(options => options.UseNpgsql(connectionString));

// Allow the Vite dev server to call the API during development.
const string DevCors = "dev-frontend";
builder.Services.AddCors(options => options.AddPolicy(DevCors, policy => policy
    .WithOrigins("http://localhost:5173", "https://localhost:5173")
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors(DevCors);
}

app.UseHttpsRedirection();

// P0 spike endpoint: proves core reuse + DB path end to end.
app.MapGet("/api/areas", async (AdleDbContext db) =>
    await db.Areas
        .AsNoTracking()
        .OrderBy(a => a.Id)
        .Select(a => new AreaDto(
            a.Id,
            a.Name,
            a.Width,
            a.Height,
            a.AreaId,
            a.AreaTypeId,
            a.AreaType != null ? a.AreaType.Name : null))
        .ToListAsync())
    .WithName("GetAreas")
    .WithOpenApi();

app.Run();
