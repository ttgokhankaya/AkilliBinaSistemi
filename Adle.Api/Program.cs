using Adle.Api.Data;
using Adle.Api.Endpoints;
using Adle.Api.MlClient;
using Adle.Analysis.Prediction;
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

// Typed HttpClient for the Python ml_service. Base URL from ML_SERVICE_URL or
// config ("MlService:BaseUrl"), defaulting to the local docker-compose port.
var mlBaseUrl =
    Environment.GetEnvironmentVariable("ML_SERVICE_URL")
    ?? builder.Configuration["MlService:BaseUrl"]
    ?? "http://localhost:8000/";
builder.Services.AddHttpClient<MlServiceClient>(client =>
{
    client.BaseAddress = new Uri(mlBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(120);
});

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

app.MapGet("/api/devices", async (AdleDbContext db) =>
    await db.DeviceBases
        .AsNoTracking()
        .OrderBy(d => d.Id)
        .Select(d => new DeviceDto(
            d.Id,
            d.Name,
            d.Ip,
            d.State,
            d.Discriminator,
            d.AreaId,
            d.Area != null ? d.Area.Name : null))
        .ToListAsync())
    .WithName("GetDevices")
    .WithOpenApi();

app.MapGet("/api/operations", async (AdleDbContext db) =>
    await db.Operations
        .AsNoTracking()
        .OrderBy(o => o.StartTime)
        .Select(o => new OperationDto(
            o.Id,
            o.Name,
            o.StartTime,
            o.Duration.TotalSeconds))
        .ToListAsync())
    .WithName("GetOperations")
    .WithOpenApi();

// Next-step + person prediction over the extracted Adle.Analysis pipeline
// (SimilarityRule -> LCSRule, min-max + softmax). Same service the console
// benchmark uses — the web layer just adapts JSON in/out.
app.MapPost("/api/analysis/predict", (PredictRequest req) =>
{
    var service = new PredictionService(req.LcsThreshold);

    var order = 0;
    var training = req.Training
        .Select(t => SequenceFactory.ToTrainingBar(t.Tokens, t.Person, order++))
        .ToList();

    var result = service.PredictNextStep(SequenceFactory.FromNodeNames(req.Prefix), training);

    var distribution = result.NextStepDistribution
        .Where(x => x.value != null)
        .Select(x => new NextNodeProbability(x.value.Name, x.probability))
        .ToList();

    return new PredictResponse(
        result.PredictedNextNode?.Name,
        result.PredictedPerson,
        result.MatchCount,
        result.RuleUsed,
        distribution);
})
    .WithName("PredictNextStep")
    .WithOpenApi();

// Anomaly endpoints proxy the Python ml_service (already web-native).
app.MapPost("/api/anomaly/tsne", async (TsneRequestDto req, MlServiceClient ml, CancellationToken ct) =>
        await ml.ComputeTsneAsync(req, ct))
    .WithName("ComputeTsne")
    .WithOpenApi();

app.MapPost("/api/anomaly/random-forest", async (RandomForestRequestDto req, MlServiceClient ml, CancellationToken ct) =>
        await ml.ComputeRandomForestAsync(req, ct))
    .WithName("ComputeRandomForest")
    .WithOpenApi();

// P3 write paths (create/update/delete for areas, devices, operations).
app.MapWriteEndpoints();

app.Run();
