using Marten;

namespace Gplx.Modules.QuestionBank;

public sealed record LicenseClassResponse(string Slug, string Code, string Name, string Description, SourceProvenance Source);

public sealed record QuestionOptionResponse(string Id, string Text);

public sealed record QuestionResponse(
    Guid Id,
    string Slug,
    string LicenseClassSlug,
    string Topic,
    string Text,
    IReadOnlyList<QuestionOptionResponse> Options,
    bool IsCritical,
    string Explanation,
    string? MemoryTip,
    string QuestionBankVersion,
    SourceProvenance Source);

public sealed record QuestionSearchResponse(IReadOnlyList<QuestionResponse> Items, int Page, int PageSize, int Total);

public sealed record PracticeQuestionResponse(
    Guid Id,
    string Slug,
    string LicenseClassSlug,
    string Topic,
    string Text,
    IReadOnlyList<QuestionOptionResponse> Options,
    string CorrectOptionId,
    bool IsCritical,
    string Explanation,
    string? MemoryTip,
    string QuestionBankVersion,
    SourceProvenance Source);

public sealed record PracticeQuestionSearchResponse(
    IReadOnlyList<PracticeQuestionResponse> Items,
    int Page,
    int PageSize,
    int Total);

public static class QuestionBankQueries
{
    public static async Task<IReadOnlyList<LicenseClassResponse>> ListLicenseClasses(IQuerySession query, CancellationToken cancellationToken)
    {
        var documents = await query.Query<LicenseClassDocument>()
            .OrderBy(item => item.Code)
            .ToListAsync(cancellationToken);

        return documents.Select(ToResponse).ToList();
    }

    public static async Task<QuestionSearchResponse> SearchQuestions(
        IQuerySession query,
        string? licenseClassSlug,
        string? topic,
        string? search,
        bool? critical,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var publishedVersions = await PublishedQuestionBankVersions(query, cancellationToken);
        var queryable = query.Query<QuestionDocument>()
            .Where(item => publishedVersions.Contains(item.QuestionBankVersion))
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(licenseClassSlug))
        {
            queryable = queryable.Where(item => item.LicenseClassSlugs.Contains(licenseClassSlug!));
        }
        if (!string.IsNullOrWhiteSpace(topic))
        {
            queryable = queryable.Where(item => item.Topic == topic);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            queryable = queryable.Where(item => item.Text.Contains(search!) || item.Topic.Contains(search!));
        }
        if (critical is not null)
        {
            queryable = queryable.Where(item => item.IsCritical == critical.Value);
        }

        var total = await queryable.CountAsync(cancellationToken);
        var documents = await queryable
            .OrderBy(item => item.Slug)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        var items = documents.Select(ToResponse).ToList();
        return new QuestionSearchResponse(items, page, pageSize, total);
    }

    public static async Task<QuestionResponse?> GetQuestion(IQuerySession query, Guid id, CancellationToken cancellationToken)
    {
        var document = await query.LoadAsync<QuestionDocument>(id, cancellationToken);
        if (document is null) return null;
        var published = await query.Query<QuestionBankVersionDocument>()
            .Where(item => item.Status == "Published" && item.Version == document.QuestionBankVersion)
            .AnyAsync(cancellationToken);
        return published ? ToResponse(document) : null;
    }

    public static async Task<PracticeQuestionSearchResponse> SearchPracticeQuestions(
        IQuerySession query,
        string? licenseClassSlug,
        string? topic,
        string? search,
        bool? critical,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var publishedVersions = await PublishedQuestionBankVersions(query, cancellationToken);
        var queryable = query.Query<QuestionDocument>()
            .Where(item => publishedVersions.Contains(item.QuestionBankVersion))
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(licenseClassSlug))
        {
            queryable = queryable.Where(item => item.LicenseClassSlugs.Contains(licenseClassSlug!));
        }
        if (!string.IsNullOrWhiteSpace(topic))
        {
            queryable = queryable.Where(item => item.Topic == topic);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            queryable = queryable.Where(item => item.Text.Contains(search!) || item.Topic.Contains(search!));
        }
        if (critical is not null)
        {
            queryable = queryable.Where(item => item.IsCritical == critical.Value);
        }

        var total = await queryable.CountAsync(cancellationToken);
        var documents = await queryable
            .OrderBy(item => item.Slug)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        var items = documents
            .Select(ToPracticeResponse)
            .ToList();
        return new PracticeQuestionSearchResponse(items, page, pageSize, total);
    }

    public static async Task<PracticeQuestionResponse?> GetPracticeQuestion(
        IQuerySession query,
        Guid id,
        CancellationToken cancellationToken)
    {
        var document = await query.LoadAsync<QuestionDocument>(id, cancellationToken);
        if (document is null) return null;
        var published = await query.Query<QuestionBankVersionDocument>()
            .Where(item => item.Status == "Published" && item.Version == document.QuestionBankVersion)
            .AnyAsync(cancellationToken);
        return published ? ToPracticeResponse(document) : null;
    }

    public static async Task<IReadOnlyList<string>> ListTopics(IQuerySession query, CancellationToken cancellationToken)
    {
        var publishedVersions = await PublishedQuestionBankVersions(query, cancellationToken);
        var topics = await query.Query<QuestionDocument>()
            .Where(item => publishedVersions.Contains(item.QuestionBankVersion))
            .Select(item => item.Topic)
            .ToListAsync(cancellationToken);
        return topics.Distinct(StringComparer.Ordinal).OrderBy(topic => topic).ToList();
    }

    private static async Task<IReadOnlyList<string>> PublishedQuestionBankVersions(
        IQuerySession query,
        CancellationToken cancellationToken) =>
        await query.Query<QuestionBankVersionDocument>()
            .Where(item => item.Status == "Published")
            .Select(item => item.Version)
            .ToListAsync(cancellationToken);

    public static async Task<IReadOnlyList<RegulationVersionDocument>> ListPublishedRegulations(
        IQuerySession query,
        CancellationToken cancellationToken) =>
        await query.Query<RegulationVersionDocument>()
            .Where(item => item.Status == "Published")
            .OrderByDescending(item => item.EffectiveFrom)
            .ToListAsync(cancellationToken);

    public static async Task<IReadOnlyList<ExamBlueprintVersionDocument>> ListPublishedBlueprints(
        IQuerySession query,
        CancellationToken cancellationToken) =>
        await query.Query<ExamBlueprintVersionDocument>()
            .Where(item => item.Status == "Published")
            .OrderByDescending(item => item.EffectiveFrom)
            .ToListAsync(cancellationToken);

    private static LicenseClassResponse ToResponse(LicenseClassDocument document) =>
        new(document.Slug, document.Code, document.Name, document.Description, document.Source);

    private static QuestionResponse ToResponse(QuestionDocument document) =>
        new(
            document.Id,
            document.Slug,
            document.LicenseClassSlug,
            document.Topic,
            document.Text,
            document.Options.Select(option => new QuestionOptionResponse(option.Id, option.Text)).ToList(),
            document.IsCritical,
            document.Explanation,
            document.MemoryTip,
            document.QuestionBankVersion,
            document.Source);

    private static PracticeQuestionResponse ToPracticeResponse(QuestionDocument document) =>
        new(
            document.Id,
            document.Slug,
            document.LicenseClassSlug,
            document.Topic,
            document.Text,
            document.Options.Select(option => new QuestionOptionResponse(option.Id, option.Text)).ToList(),
            document.CorrectOptionId,
            document.IsCritical,
            document.Explanation,
            document.MemoryTip,
            document.QuestionBankVersion,
            document.Source);
}
