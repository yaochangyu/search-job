namespace SearchJob.JobCategories;

public sealed class JobCodeCategoryIndex
{
	// 反查：職缺代碼 -> 類別代碼集合
	// 設計目的：查詢時僅做 O(1) TryGetValue + union 合併
	private readonly Dictionary<string, HashSet<long>> _smallCodesByJobCode;
	private readonly Dictionary<string, HashSet<long>> _middleCodesByJobCode;
	private readonly Dictionary<string, HashSet<long>> _majorCodesByJobCode;

	public JobCodeCategoryIndex(
		Dictionary<string, HashSet<long>> smallCodesByJobCode,
		Dictionary<string, HashSet<long>> middleCodesByJobCode,
		Dictionary<string, HashSet<long>> majorCodesByJobCode)
	{
		_smallCodesByJobCode = smallCodesByJobCode;
		_middleCodesByJobCode = middleCodesByJobCode;
		_majorCodesByJobCode = majorCodesByJobCode;
	}

	public IReadOnlyCollection<long> GetSmallCodes(IEnumerable<string> jobCodes)
		=> UnionAll(_smallCodesByJobCode, jobCodes);

	public IReadOnlyCollection<long> GetMiddleCodes(IEnumerable<string> jobCodes)
		=> UnionAll(_middleCodesByJobCode, jobCodes);

	public IReadOnlyCollection<long> GetMajorCodes(IEnumerable<string> jobCodes)
		=> UnionAll(_majorCodesByJobCode, jobCodes);

	private static IReadOnlyCollection<long> UnionAll(
		Dictionary<string, HashSet<long>> map,
		IEnumerable<string> keys)
	{
		var result = new HashSet<long>();
		foreach (var key in keys)
		{
			if (!map.TryGetValue(key, out var values))
				continue;

			foreach (var value in values)
				result.Add(value);
		}
		return result;
	}
}
