namespace SearchJob.Models;

/// <summary>
/// 職缺資料模型（對應 spec-2 第 3 章）。
/// </summary>
public sealed class JobVacancy
{
    private readonly HashSet<int> _minorCodes;

    public JobVacancy(int jobId, string title, string description, IEnumerable<int>? minorCodes)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }

        JobId = jobId;
        Title = title;
        Description = description ?? string.Empty;

        _minorCodes = minorCodes is null
            ? new HashSet<int>()
            : new HashSet<int>(minorCodes);
    }

    public int JobId { get; }

    public string Title { get; }

    public string Description { get; }

    public IReadOnlySet<int> MinorCodes => _minorCodes;
}
