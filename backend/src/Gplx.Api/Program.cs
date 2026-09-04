using Gplx.Api;
using Gplx.Modules.Exams;
using Gplx.Modules.Identity;
using Gplx.Modules.Learning;
using Gplx.Modules.QuestionBank;
using JasperFx;
using JasperFx.CodeGeneration.Model;
using JasperFx.Events.Daemon;
using Marten;
using Wolverine;
using Wolverine.Marten;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddCors(options => options.AddPolicy("frontend", policy =>
    policy.WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:3000"])
        .AllowAnyHeader()
        .AllowAnyMethod()));
var marten = builder.Services.AddMarten(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Postgres");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("ConnectionStrings:Postgres is required.");
    }
    options.Connection(connectionString);
    options.DatabaseSchemaName = "gplx";
    options.AutoCreateSchemaObjects = builder.Environment.IsDevelopment()
        ? AutoCreate.All
        : AutoCreate.CreateOrUpdate;
    options.Schema.For<LicenseClassDocument>();
    options.Schema.For<QuestionDocument>();
    options.Schema.For<QuestionBankVersionDocument>();
    options.Schema.For<RegulationVersionDocument>();
    options.Schema.For<ExamBlueprintVersionDocument>();
    options.Schema.For<ExamAttemptSnapshot>();
    options.Schema.For<ExamAttemptView>();
    options.Schema.For<QuestionPerformanceDocument>();
});
marten.IntegrateWithWolverine();
marten.AddAsyncDaemon(DaemonMode.HotCold);
marten.AddSubscriptionWithServices<QuestionPerformanceSubscription>(ServiceLifetime.Singleton);
builder.Host.UseWolverine(options =>
{
    options.ServiceLocationPolicy = ServiceLocationPolicy.AllowedButWarn;
    options.Policies.AutoApplyTransactions();
    options.Discovery.IncludeType<ExamCommandHandlers>();
    options.Discovery.IncludeType<AdminVersionCommandHandlers>();
});
builder.Services.AddScoped<NormalizedDataSeeder>();
builder.Services.AddAdminAuthentication(builder.Configuration, builder.Environment);

var app = builder.Build();
app.UseExceptionHandler(errorApp => errorApp.Run(ApiExceptionHandler.HandleAsync));
app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapGplxApi();

if (app.Configuration.GetValue<bool>("Seed:Enabled"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var session = scope.ServiceProvider.GetRequiredService<IDocumentSession>();
    await scope.ServiceProvider.GetRequiredService<NormalizedDataSeeder>().SeedAsync(session, CancellationToken.None);
}

app.Run();

public partial class Program;
