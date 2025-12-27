using SearchJob.Models;

namespace SearchJob.Indexes;

public sealed class MajorToJobIndex
{
    private readonly Dictionary<int, HashSet<int>> _jobIdsByMajorCode;

    public MajorToJobIndex(JobCategoryHierarchyIndex categoryIndex, IEnumerable<JobPosting> jobs)
    {
        ArgumentNullException.ThrowIfNull(categoryIndex);
        ArgumentNullException.ThrowIfNull(jobs);

        _jobIdsByMajorCode = new Dictionary<int, HashSet<int>>();

        foreach (var job in jobs)
        {
            var majorCodes = categoryIndex.GetMajorCodesByMinorCodes(job.MinorCodes);
            foreach (var majorCode in majorCodes)
            {
                if (!_jobIdsByMajorCode.TryGetValue(majorCode, out var set))
                {
                    set = new HashSet<int>();
                    _jobIdsByMajorCode[majorCode] = set;
                }

                set.Add(job.JobId);
            }
        }
    }

    public IReadOnlySet<int> GetJobIdsByMajorCodes(IEnumerable<int> majorCodes)
    {
        ArgumentNullException.ThrowIfNull(majorCodes);

        var result = new HashSet<int>();
        foreach (var majorCode in majorCodes)
        {
            if (_jobIdsByMajorCode.TryGetValue(majorCode, out var jobIds))
            {
                result.UnionWith(jobIds);
            }
        }

        return result;
    }
}
