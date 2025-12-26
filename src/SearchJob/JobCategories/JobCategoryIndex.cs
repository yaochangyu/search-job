namespace SearchJob.JobCategories;

public sealed class JobCategoryIndex
{
	// 基本定義（Code -> Entity）
	private readonly Dictionary<long, MajorCategory> _majorByCode;
	private readonly Dictionary<long, MiddleCategory> _middleByCode;
	private readonly Dictionary<long, SmallCategory> _smallByCode;

	// 反查：小類 -> 中類/大類
	private readonly Dictionary<long, long> _middleCodeBySmallCode;
	private readonly Dictionary<long, long> _majorCodeBySmallCode;

	// 聚合：中類 -> 小類集合
	private readonly Dictionary<long, HashSet<long>> _smallCodesByMiddleCode;

	// 聚合：大類 -> 小類集合 / 中類集合
	private readonly Dictionary<long, HashSet<long>> _smallCodesByMajorCode;
	private readonly Dictionary<long, HashSet<long>> _middleCodesByMajorCode;

	public JobCategoryIndex(
		IEnumerable<MajorCategory> majors,
		IEnumerable<MiddleCategory> middles,
		IEnumerable<SmallCategory> smalls)
	{
		_majorByCode = new();
		_middleByCode = new();
		_smallByCode = new();

		_middleCodeBySmallCode = new();
		_majorCodeBySmallCode = new();
		_smallCodesByMiddleCode = new();

		_smallCodesByMajorCode = new();
		_middleCodesByMajorCode = new();

		foreach (var major in majors)
		{
			_majorByCode[major.Code] = major;
			_smallCodesByMajorCode.TryAdd(major.Code, new HashSet<long>());
			_middleCodesByMajorCode.TryAdd(major.Code, new HashSet<long>());
		}

		foreach (var middle in middles)
		{
			_middleByCode[middle.Code] = middle;
			_smallCodesByMiddleCode.TryAdd(middle.Code, new HashSet<long>());
			if (!_middleCodesByMajorCode.TryGetValue(middle.MajorCode, out var middleSet))
			{
				middleSet = new HashSet<long>();
				_middleCodesByMajorCode[middle.MajorCode] = middleSet;
			}
			middleSet.Add(middle.Code);
		}

		foreach (var small in smalls)
		{
			_smallByCode[small.Code] = small;
			_middleCodeBySmallCode[small.Code] = small.MiddleCode;
			_majorCodeBySmallCode[small.Code] = small.MajorCode;

			if (!_smallCodesByMiddleCode.TryGetValue(small.MiddleCode, out var smallSetInMiddle))
			{
				smallSetInMiddle = new HashSet<long>();
				_smallCodesByMiddleCode[small.MiddleCode] = smallSetInMiddle;
			}
			smallSetInMiddle.Add(small.Code);

			if (!_smallCodesByMajorCode.TryGetValue(small.MajorCode, out var smallSet))
			{
				smallSet = new HashSet<long>();
				_smallCodesByMajorCode[small.MajorCode] = smallSet;
			}
			smallSet.Add(small.Code);

			if (!_middleCodesByMajorCode.TryGetValue(small.MajorCode, out var middleSet))
			{
				middleSet = new HashSet<long>();
				_middleCodesByMajorCode[small.MajorCode] = middleSet;
			}
			middleSet.Add(small.MiddleCode);
		}
	}

	public IReadOnlyCollection<MajorCategory> GetMajorsBySmallCodes(IEnumerable<long> smallCodes)
	{
		var result = new HashSet<MajorCategory>();
		foreach (var smallCode in smallCodes)
		{
			if (_majorCodeBySmallCode.TryGetValue(smallCode, out var majorCode)
				&& _majorByCode.TryGetValue(majorCode, out var major))
			{
				result.Add(major);
			}
		}
		return result;
	}

	public IReadOnlyCollection<MiddleCategory> GetMiddlesBySmallCodes(IEnumerable<long> smallCodes)
	{
		var result = new HashSet<MiddleCategory>();
		foreach (var smallCode in smallCodes)
		{
			if (_middleCodeBySmallCode.TryGetValue(smallCode, out var middleCode)
				&& _middleByCode.TryGetValue(middleCode, out var middle))
			{
				result.Add(middle);
			}
		}
		return result;
	}

	public IReadOnlyCollection<SmallCategory> GetSmallsByMiddleCodes(IEnumerable<long> middleCodes)
	{
		var result = new HashSet<SmallCategory>();
		foreach (var middleCode in middleCodes)
		{
			if (!_smallCodesByMiddleCode.TryGetValue(middleCode, out var smallCodesInMiddle))
				continue;

			foreach (var smallCode in smallCodesInMiddle)
			{
				if (_smallByCode.TryGetValue(smallCode, out var small))
					result.Add(small);
			}
		}
		return result;
	}

	public IReadOnlyCollection<MajorCategory> GetMajorsByMiddleCodes(IEnumerable<long> middleCodes)
	{
		var result = new HashSet<MajorCategory>();
		foreach (var middleCode in middleCodes)
		{
			if (_middleByCode.TryGetValue(middleCode, out var middle)
				&& _majorByCode.TryGetValue(middle.MajorCode, out var major))
			{
				result.Add(major);
			}
		}
		return result;
	}

	public IReadOnlyCollection<SmallCategory> GetSmallsByMajorCodes(IEnumerable<long> majorCodes)
	{
		var result = new HashSet<SmallCategory>();
		foreach (var majorCode in majorCodes)
		{
			if (!_smallCodesByMajorCode.TryGetValue(majorCode, out var smallCodesInMajor))
				continue;

			foreach (var smallCode in smallCodesInMajor)
			{
				if (_smallByCode.TryGetValue(smallCode, out var small))
					result.Add(small);
			}
		}
		return result;
	}

	public IReadOnlyCollection<MiddleCategory> GetMiddlesByMajorCodes(IEnumerable<long> majorCodes)
	{
		var result = new HashSet<MiddleCategory>();
		foreach (var majorCode in majorCodes)
		{
			if (!_middleCodesByMajorCode.TryGetValue(majorCode, out var middleCodesInMajor))
				continue;

			foreach (var middleCode in middleCodesInMajor)
			{
				if (_middleByCode.TryGetValue(middleCode, out var middle))
					result.Add(middle);
			}
		}
		return result;
	}
}
