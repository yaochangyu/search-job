namespace SearchJob.Models;

public sealed class JobPosting
{
    private readonly HashSet<int> _minorCodes;

    public JobPosting(int jobId, string title, string description, IEnumerable<int>? minorCodes)
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
