using Gplx.BuildingBlocks;
using Gplx.Modules.Exams;
using Gplx.Modules.Identity;
using Gplx.Modules.Learning;
using Gplx.Modules.QuestionBank;
using JasperFx.Events.Daemon;
using Marten;
using Wolverine;

namespace Gplx.Api;

public static class ApiEndpoints
{
    public static void MapGplxApi(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api");
        api.MapGet("/health", () => Results.Ok(new { status = "ok", service = "gplx-api" }));

        api.MapGet("/licenses", QuestionBankQueries.ListLicenseClasses);
        api.MapGet("/questions", async (
            IQuerySession query,
            string? licenseClassSlug,
            string? topic,
            string? search,
            bool? critical,
            int? page,
            int? pageSize,
            CancellationToken cancellationToken) =>
            Results.Ok(await QuestionBankQueries.SearchQuestions(query, licenseClassSlug, topic, search, critical, page ?? 1, pageSize ?? 20, cancellationToken)));
        api.MapGet("/topics", QuestionBankQueries.ListTopics);
        api.MapGet("/practice/questions", async (
            IQuerySession query,
            string? licenseClassSlug,
            string? topic,
            string? search,
            bool? critical,
            int? page,
            int? pageSize,
            CancellationToken cancellationToken) =>
            Results.Ok(await QuestionBankQueries.SearchPracticeQuestions(
                query, licenseClassSlug, topic, search, critical, page ?? 1, pageSize ?? 20, cancellationToken)));
        api.MapGet("/practice/questions/{id:guid}", async (Guid id, IQuerySession query, CancellationToken cancellationToken) =>
        {
            var question = await QuestionBankQueries.GetPracticeQuestion(query, id, cancellationToken);
            return question is null ? Results.NotFound() : Results.Ok(question);
        });
        api.MapGet("/regulations", QuestionBankQueries.ListPublishedRegulations);
        api.MapGet("/exam-blueprints", QuestionBankQueries.ListPublishedBlueprints);
        api.MapGet("/questions/{id:guid}", async (Guid id, IQuerySession query, CancellationToken cancellationToken) =>
        {
            var question = await QuestionBankQueries.GetQuestion(query, id, cancellationToken);
            return question is null ? Results.NotFound() : Results.Ok(question);
        });

        api.MapPost("/exams", async (StartExamRequest request, IMessageBus bus, CancellationToken cancellationToken) =>
            Results.Ok(await bus.InvokeAsync<StartExamResult>(new StartExamCommand(request.LicenseClassSlug), cancellationToken)));
        api.MapGet("/exams/{id:guid}", async (Guid id, IQuerySession query, CancellationToken cancellationToken) =>
        {
            var view = await query.LoadAsync<ExamAttemptView>(id, cancellationToken);
            return view is null ? Results.NotFound() : Results.Ok(view);
        });
        api.MapPost("/exams/{id:guid}/answers", async (Guid id, AnswerRequest request, IMessageBus bus, CancellationToken cancellationToken) =>
            Results.Ok(await bus.InvokeAsync<AnswerQuestionResult>(new AnswerQuestionCommand(id, request.QuestionId, request.OptionId), cancellationToken)));
        api.MapPost("/exams/{id:guid}/flags", async (Guid id, FlagRequest request, IMessageBus bus, CancellationToken cancellationToken) =>
            Results.Ok(await bus.InvokeAsync<FlagQuestionResult>(new FlagQuestionCommand(id, request.QuestionId, request.Flagged), cancellationToken)));
        api.MapPost("/exams/{id:guid}/submit", async (Guid id, IMessageBus bus, CancellationToken cancellationToken) =>
            Results.Ok(await bus.InvokeAsync<SubmitExamResult>(new SubmitExamCommand(id), cancellationToken)));
        api.MapGet("/exams/{id:guid}/result", async (Guid id, IQuerySession query, CancellationToken cancellationToken) =>
        {
            var view = await query.LoadAsync<ExamAttemptView>(id, cancellationToken);
            return view is null ? Results.NotFound() : view.Status == nameof(ExamAttemptStatus.Scored) ? Results.Ok(view) : Results.Conflict(view);
        });

        var admin = api.MapGroup("/admin");
        admin.MapPost("/auth/login", AdminAuthentication.Login).AllowAnonymous();
        admin.MapGet("/question-banks", async (IQuerySession query, CancellationToken cancellationToken) =>
            Results.Ok(await query.Query<QuestionBankVersionDocument>().OrderByDescending(item => item.EffectiveFrom).ToListAsync(cancellationToken)))
            .RequireAuthorization(PermissionNames.QuestionBankRead);
        admin.MapGet("/question-banks/{id:guid}/preview", async (Guid id, IQuerySession query, CancellationToken cancellationToken) =>
        {
            var version = await query.LoadAsync<QuestionBankVersionDocument>(id, cancellationToken);
            if (version is null) return Results.NotFound();
            var questions = await query.Query<QuestionDocument>()
                .Where(item => item.QuestionBankVersion == version.Version)
                .OrderBy(item => item.Slug)
                .ToListAsync(cancellationToken);
            return Results.Ok(new
            {
                version,
                questions,
                validationErrors = AdminVersionCommandHandlers.ValidateQuestionBank(version, questions)
            });
        }).RequireAuthorization(PermissionNames.QuestionBankRead);
        admin.MapGet("/license-classes", async (IQuerySession query, CancellationToken cancellationToken) =>
            Results.Ok(await query.Query<LicenseClassDocument>().OrderBy(item => item.Code).ToListAsync(cancellationToken)))
            .RequireAuthorization(PermissionNames.QuestionBankRead);
        admin.MapPost("/license-classes", async (SaveLicenseClassRequest request, IMessageBus bus, CancellationToken cancellationToken) =>
            Results.Ok(await bus.InvokeAsync<SaveLicenseClassResult>(
                new SaveLicenseClassCommand(request.Id, request.Slug, request.Code, request.Name, request.Description, request.Source),
                cancellationToken)))
            .RequireAuthorization(PermissionNames.QuestionBankEdit);
        admin.MapPut("/question-banks/{versionId:guid}/questions/{questionId:guid}", async (
            Guid versionId,
            Guid questionId,
            AdminQuestionInput question,
            IMessageBus bus,
            CancellationToken cancellationToken) =>
            Results.Ok(await bus.InvokeAsync<QuestionDocument>(
                new EditQuestionCommand(versionId, questionId, question), cancellationToken)))
            .RequireAuthorization(PermissionNames.QuestionBankEdit);
        admin.MapPost("/question-banks/import", async (ImportQuestionBankRequest request, IMessageBus bus, CancellationToken cancellationToken) =>
            Results.Ok(await bus.InvokeAsync<ImportQuestionBankResult>(
                new ImportQuestionBankCommand(request.Version, request.EffectiveFrom, request.LicenseClassSlugs, request.Questions, request.Source),
                cancellationToken)))
            .RequireAuthorization(PermissionNames.QuestionBankImport);
        admin.MapPost("/question-banks/{id:guid}/publish", async (Guid id, IMessageBus bus, CancellationToken cancellationToken) =>
            Results.Ok(await bus.InvokeAsync<QuestionBankVersionDocument>(new PublishQuestionBankVersionCommand(id), cancellationToken)))
            .RequireAuthorization(PermissionNames.QuestionBankPublish);
        admin.MapPost("/question-banks/{id:guid}/deprecate", async (Guid id, IMessageBus bus, CancellationToken cancellationToken) =>
            Results.Ok(await bus.InvokeAsync<QuestionBankVersionDocument>(new DeprecateQuestionBankVersionCommand(id), cancellationToken)))
            .RequireAuthorization(PermissionNames.QuestionBankPublish);

        admin.MapGet("/regulations", async (IQuerySession query, CancellationToken cancellationToken) =>
            Results.Ok(await query.Query<RegulationVersionDocument>().OrderByDescending(item => item.EffectiveFrom).ToListAsync(cancellationToken)))
            .RequireAuthorization(PermissionNames.RegulationRead);
        admin.MapPost("/regulations", async (SaveRegulationVersionRequest request, IMessageBus bus, CancellationToken cancellationToken) =>
            Results.Ok(await bus.InvokeAsync<SaveRegulationVersionResult>(
                new SaveRegulationVersionCommand(request.Id, request.Version, request.Title, request.Summary, request.EffectiveFrom, request.Source),
                cancellationToken)))
            .RequireAuthorization(PermissionNames.RegulationManage);
        admin.MapPost("/regulations/{id:guid}/publish", async (Guid id, IMessageBus bus, CancellationToken cancellationToken) =>
            Results.Ok(await bus.InvokeAsync<RegulationVersionDocument>(new PublishRegulationVersionCommand(id), cancellationToken)))
            .RequireAuthorization(PermissionNames.RegulationPublish);

        admin.MapGet("/exam-blueprints", async (IQuerySession query, CancellationToken cancellationToken) =>
            Results.Ok(await query.Query<ExamBlueprintVersionDocument>().OrderByDescending(item => item.EffectiveFrom).ToListAsync(cancellationToken)))
            .RequireAuthorization(PermissionNames.ExamBlueprintRead);
        admin.MapPost("/exam-blueprints", async (SaveExamBlueprintVersionRequest request, IMessageBus bus, CancellationToken cancellationToken) =>
            Results.Ok(await bus.InvokeAsync<SaveExamBlueprintVersionResult>(
                new SaveExamBlueprintVersionCommand(request.Id, request.Version, request.EffectiveFrom, request.Blueprints, request.Source),
                cancellationToken)))
            .RequireAuthorization(PermissionNames.ExamBlueprintManage);
        admin.MapPost("/exam-blueprints/{id:guid}/publish", async (Guid id, IMessageBus bus, CancellationToken cancellationToken) =>
            Results.Ok(await bus.InvokeAsync<ExamBlueprintVersionDocument>(new PublishExamBlueprintVersionCommand(id), cancellationToken)))
            .RequireAuthorization(PermissionNames.ExamBlueprintPublish);

        admin.MapGet("/analytics/question-performance", async (IQuerySession query, int? limit, CancellationToken cancellationToken) =>
        {
            var performance = await query.Query<QuestionPerformanceDocument>().ToListAsync(cancellationToken);
            var questions = await query.Query<QuestionDocument>().ToListAsync(cancellationToken);
            var questionById = questions.ToDictionary(item => item.Id);
            var items = performance
                .OrderBy(item => item.Attempts == 0 ? 0 : (double)item.CorrectAnswers / item.Attempts)
                .ThenByDescending(item => item.Attempts)
                .Take(Math.Clamp(limit ?? 100, 1, 500))
                .Select(item => new
                {
                    item.Id,
                    item.LicenseClassSlug,
                    item.Attempts,
                    item.CorrectAnswers,
                    item.IncorrectAnswers,
                    item.CriticalMistakes,
                    accuracy = item.Attempts == 0 ? 0 : (double)item.CorrectAnswers / item.Attempts,
                    item.LastScoredAt,
                    question = questionById.TryGetValue(item.Id, out var question) ? question.Text : null,
                    topic = questionById.TryGetValue(item.Id, out question) ? question.Topic : null
                });
            return Results.Ok(items);
        }).RequireAuthorization(PermissionNames.AnalyticsRead);

        admin.MapGet("/projection/status", async (IProjectionCoordinator coordinator, CancellationToken cancellationToken) =>
        {
            var daemons = await coordinator.AllDaemonsAsync();
            return Results.Ok(daemons.Select(daemon => new
            {
                daemon.StoreUri,
                daemon.IsRunning,
                daemon.IsHighWaterStale,
                daemon.HighWaterLastPolledAt
            }));
        }).RequireAuthorization(PermissionNames.ProjectionRead);
        admin.MapPost("/projection/rebuild/question-performance", async (IProjectionCoordinator coordinator, CancellationToken cancellationToken) =>
        {
            await coordinator.DaemonForMainDatabase().RebuildProjectionAsync(typeof(QuestionPerformanceSubscription), cancellationToken);
            return Results.Accepted();
        }).RequireAuthorization(PermissionNames.ProjectionRebuild);
    }

    private sealed record StartExamRequest(string LicenseClassSlug);
    private sealed record AnswerRequest(Guid QuestionId, string OptionId);
    private sealed record FlagRequest(Guid QuestionId, bool Flagged);
    private sealed record ImportQuestionBankRequest(
        string Version,
        DateOnly EffectiveFrom,
        IReadOnlyList<string> LicenseClassSlugs,
        IReadOnlyList<AdminQuestionInput> Questions,
        SourceProvenance Source);
    private sealed record SaveLicenseClassRequest(
        Guid? Id,
        string Slug,
        string Code,
        string Name,
        string Description,
        SourceProvenance Source);
    private sealed record SaveRegulationVersionRequest(
        Guid? Id,
        string Version,
        string Title,
        string Summary,
        DateOnly EffectiveFrom,
        SourceProvenance Source);
    private sealed record SaveExamBlueprintVersionRequest(
        Guid? Id,
        string Version,
        DateOnly EffectiveFrom,
        IReadOnlyList<ExamBlueprintRule> Blueprints,
        SourceProvenance Source);
}
