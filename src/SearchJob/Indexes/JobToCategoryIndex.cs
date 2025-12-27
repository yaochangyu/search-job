using SearchJob.Models;

namespace SearchJob.Indexes;

public sealed class JobToCategoryIndex
{
    private readonly JobCategoryHierarchyIndex _categoryIndex;
    private readonly Dictionary<int, HashSet<int>> _minorCodesByJobId;

    public JobToCategoryIndex(JobCategoryHierarchyIndex categoryIndex, IEnumerable<JobPosting> jobs)
    {
        ArgumentNullException.ThrowIfNull(categoryIndex);
        ArgumentNullException.ThrowIfNull(jobs);

        _categoryIndex = categoryIndex;
        _minorCodesByJobId = new Dictionary<int, HashSet<int>>();

        foreach (var job in jobs)
        {
            if (!_minorCodesByJobId.TryGetValue(job.JobId, out var set))
            {
                set = new HashSet<int>();
                _minorCodesByJobId[job.JobId] = set;
            }

            set.UnionWith(job.MinorCodes);
        }
    }

    public IReadOnlySet<int> GetMinorCodesByJobIds(IEnumerable<int> jobIds)
    {
        ArgumentNullException.ThrowIfNull(jobIds);

        var result = new HashSet<int>();
        foreach (var jobId in jobIds)
        {
            if (_minorCodesByJobId.TryGetValue(jobId, out var minorCodes))
            {
                result.UnionWith(minorCodes);
            }
        }

        return result;
    }

    public IReadOnlySet<int> GetMiddleCodesByJobIds(IEnumerable<int> jobIds)
    {
        ArgumentNullException.ThrowIfNull(jobIds);
        var minorCodes = GetMinorCodesByJobIds(jobIds);
        return _categoryIndex.GetMiddleCodesByMinorCodes(minorCodes);
    }

    public IReadOnlySet<int> GetMajorCodesByJobIds(IEnumerable<int> jobIds)
    {
        ArgumentNullException.ThrowIfNull(jobIds);
        var minorCodes = GetMinorCodesByJobIds(jobIds);
        return _categoryIndex.GetMajorCodesByMinorCodes(minorCodes);
    }
}
