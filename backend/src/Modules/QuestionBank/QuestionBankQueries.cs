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
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var queryable = query.Query<QuestionDocument>().AsQueryable();
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

        var documents = await queryable.OrderBy(item => item.Slug).ToListAsync(cancellationToken);

        var total = documents.Count;
        var items = documents.Skip((page - 1) * pageSize).Take(pageSize).Select(ToResponse).ToList();
        return new QuestionSearchResponse(items, page, pageSize, total);
    }

    public static async Task<QuestionResponse?> GetQuestion(IQuerySession query, Guid id, CancellationToken cancellationToken)
    {
        var document = await query.LoadAsync<QuestionDocument>(id, cancellationToken);
        return document is null ? null : ToResponse(document);
    }

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
}
