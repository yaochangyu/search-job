using SearchJob.Models;

namespace SearchJob.Indexes;

/// <summary>
/// 職缺代碼 → 職務類別索引（對應 spec-2 第 5 章）。
/// 
/// 設計重點：
/// - 在建構時預先建置小類/中類/大類索引，避免查詢時跨索引重算
/// - 若輸入的 jobs 包含重複的 jobId，會將其 MinorCodes 進行聯集合併
/// - 中類和大類代碼在建構時從層級索引推導並快取（對應 spec-2 第 5.3 節）
/// </summary>
public sealed class JobToCategoryIndex
{
    private readonly Dictionary<int, HashSet<int>> _minorCodesByJobId;
    private readonly Dictionary<int, HashSet<int>> _middleCodesByJobId;
    private readonly Dictionary<int, HashSet<int>> _majorCodesByJobId;

    public JobToCategoryIndex(JobCategoryHierarchyIndex categoryIndex, IEnumerable<JobPosting> jobs)
    {
        ArgumentNullException.ThrowIfNull(categoryIndex);
        ArgumentNullException.ThrowIfNull(jobs);

        _minorCodesByJobId = new Dictionary<int, HashSet<int>>();
        _middleCodesByJobId = new Dictionary<int, HashSet<int>>();
        _majorCodesByJobId = new Dictionary<int, HashSet<int>>();

        foreach (var job in jobs)
        {
            // 建立小類索引
            if (!_minorCodesByJobId.TryGetValue(job.JobId, out var minorSet))
            {
                minorSet = new HashSet<int>();
                _minorCodesByJobId[job.JobId] = minorSet;
            }
            minorSet.UnionWith(job.MinorCodes);

            // 預先推導並快取中類代碼
            var middleCodes = categoryIndex.GetMiddleCodesByMinorCodes(job.MinorCodes);
            if (!_middleCodesByJobId.TryGetValue(job.JobId, out var middleSet))
            {
                middleSet = new HashSet<int>();
                _middleCodesByJobId[job.JobId] = middleSet;
            }
            middleSet.UnionWith(middleCodes);

            // 預先推導並快取大類代碼
            var majorCodes = categoryIndex.GetMajorCodesByMinorCodes(job.MinorCodes);
            if (!_majorCodesByJobId.TryGetValue(job.JobId, out var majorSet))
            {
                majorSet = new HashSet<int>();
                _majorCodesByJobId[job.JobId] = majorSet;
            }
            majorSet.UnionWith(majorCodes);
        }
    }

    /// <summary>
    /// 必備查詢 #1：用一個或多個「職缺代碼」找到多筆「職務小類」（對應 spec-2 第 5.2 節）。
    /// </summary>
    /// <param name="jobIds">職缺代碼集合（允許重複；不存在的代碼會被忽略）。</param>
    /// <returns>小類代碼集合（不重複、不保證順序）。</returns>
    public IReadOnlySet<int> GetMinorCodesByJobIds(IEnumerable<int> jobIds)
    {
        ArgumentNullException.ThrowIfNull(jobIds);
        return UnionAll(_minorCodesByJobId, jobIds);
    }

    /// <summary>
    /// 必備查詢 #2：用一個或多個「職缺代碼」找到多筆「職務中類」（對應 spec-2 第 5.2 節）。
    /// </summary>
    /// <param name="jobIds">職缺代碼集合（允許重複；不存在的代碼會被忽略）。</param>
    /// <returns>中類代碼集合（不重複、不保證順序）。</returns>
    public IReadOnlySet<int> GetMiddleCodesByJobIds(IEnumerable<int> jobIds)
    {
        ArgumentNullException.ThrowIfNull(jobIds);
        return UnionAll(_middleCodesByJobId, jobIds);
    }

    /// <summary>
    /// 必備查詢 #3：用一個或多個「職缺代碼」找到多個「職務大類」（對應 spec-2 第 5.2 節）。
    /// </summary>
    /// <param name="jobIds">職缺代碼集合（允許重複;不存在的代碼會被忽略）。</param>
    /// <returns>大類代碼集合（不重複、不保證順序）。</returns>
    public IReadOnlySet<int> GetMajorCodesByJobIds(IEnumerable<int> jobIds)
    {
        ArgumentNullException.ThrowIfNull(jobIds);
        return UnionAll(_majorCodesByJobId, jobIds);
    }

    private static IReadOnlySet<int> UnionAll(
        Dictionary<int, HashSet<int>> map,
        IEnumerable<int> keys)
    {
        var result = new HashSet<int>();
        foreach (var key in keys)
        {
            if (map.TryGetValue(key, out var values))
            {
                result.UnionWith(values);
            }
        }
        return result;
    }
}
