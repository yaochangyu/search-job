## 職務類別規則

職務類別規則包含「大類／中類／小類」，每一層級皆有名稱與代碼。

### 大類

| 名稱 | 代碼 |
| --- | --- |
| 管理幕僚／人資／行政 | 100000 |

### 中類

| 名稱 | 代碼 |
| --- | --- |
| 管理幕僚 | 100100 |
| 人力資源 | 100200 |

### 小類

| 名稱 | 代碼 |
| --- | --- |
| 經營管理主管 | 100101 |
| 特別助理 | 100105 |
| 人事助理 | 100205 |
| 就業服務員 | 100206 |

## 職缺欄位
- 職缺編號(常整數)
- 工作標題
- 工作說明
- 職務小類(多筆)

## 核心技術

- C#
- MSSQL

## 物件設計：職務類別（大／中／小）索引

目標：用「時間複雜度 O(1)」的型別（`Dictionary` / `HashSet`）建立索引，滿足以下查詢：

- 用一個或多個「職務小類」找到「大類集合」
- 用一個或多個「職務小類」找到「中類集合」
- 用一個或多個「職務中類」找到「小類集合」
- 用一個或多個「職務中類」找到「大類集合」
- 用一個或多個「職務大類」找到「小類集合」
- 用一個或多個「職務大類」找到「中類集合」

### C# 資料模型

```csharp
public sealed record MajorCategory(long Code, string Name);
public sealed record MiddleCategory(long Code, string Name, long MajorCode);
public sealed record SmallCategory(long Code, string Name, long MiddleCode, long MajorCode);
```

### C# 索引物件（O(1)）

設計重點：
- 單次查詢：`Dictionary.TryGetValue` / `HashSet.Contains` 期望為 O(1)
- 多筆輸入：以 `HashSet` 做去重與合併（union）

```csharp
using System;
using System.Collections.Generic;

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
			if (!_smallCodesByMajorCode.TryGetValue(majorCode, out var smallCodes))
				continue;

			foreach (var smallCode in smallCodes)
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
			if (!_middleCodesByMajorCode.TryGetValue(majorCode, out var middleCodes))
				continue;

			foreach (var middleCode in middleCodes)
			{
				if (_middleByCode.TryGetValue(middleCode, out var middle))
					result.Add(middle);
			}
		}
		return result;
	}
}
```

### 使用案例（以本規格的類別資料）

```csharp
var majors = new[]
{
	new MajorCategory(100000, "管理幕僚／人資／行政"),
};

var middles = new[]
{
	new MiddleCategory(100100, "管理幕僚", 100000),
	new MiddleCategory(100200, "人力資源", 100000),
};

var smalls = new[]
{
	new SmallCategory(100101, "經營管理主管", 100100, 100000),
	new SmallCategory(100105, "特別助理", 100100, 100000),
	new SmallCategory(100205, "人事助理", 100200, 100000),
	new SmallCategory(100206, "就業服務員", 100200, 100000),
};

var index = new JobCategoryIndex(majors, middles, smalls);

// 1) 用一個或多個小類找到大類
var majors1 = index.GetMajorsBySmallCodes(new[] { 100205L, 100105L });

// 2) 用一個或多個小類找到中類
var middles1 = index.GetMiddlesBySmallCodes(new[] { 100205L, 100105L });

// 3) 用一個或多個大類找到小類集合
var smalls1 = index.GetSmallsByMajorCodes(new[] { 100000L });

// 4) 用一個或多個大類找到中類集合
var middles2 = index.GetMiddlesByMajorCodes(new[] { 100000L });

// 5) 用一個或多個中類找到小類集合
var smalls2 = index.GetSmallsByMiddleCodes(new[] { 100100L, 100200L });

// 6) 用一個或多個中類找到大類集合
var majors2 = index.GetMajorsByMiddleCodes(new[] { 100100L, 100200L });
```

## 物件設計：職缺代碼 → 職務類別索引

目標：用「時間複雜度 O(1)」的型別（`Dictionary` / `HashSet`）建立索引，滿足以下查詢：

- 用一個或多個「職缺代碼」找到多筆「職務小類」
- 用一個或多個「職缺代碼」找到多筆「職務中類」
- 用一個或多個「職缺代碼」找到「職務大類」

### 設計說明（避免跨索引重算）

- `jobCode -> smallCodes`：直接回傳小類集合
- `jobCode -> middleCodes`：預先建立（避免查詢時再由小類推導）
- `jobCode -> majorCodes`：支援多筆職缺代碼輸入後取聯集；如果你的業務規則保證「同一批職缺代碼一定只屬於同一個大類」，可在上層加檢核

```csharp
using System;
using System.Collections.Generic;

public sealed class JobCodeCategoryIndex
{
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
```

### 使用案例（職缺代碼對應到本規格類別代碼）

```csharp
// 範例：職缺代碼 -> 類別代碼
var smallByJob = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
{
	["JOB-001"] = new HashSet<long> { 100105L, 100205L },
	["JOB-002"] = new HashSet<long> { 100101L },
};

var middleByJob = new Dictionary<string, HashSet<long>>(StringComparer.OrdinalIgnoreCase)
{
	["JOB-001"] = new HashSet<long> { 100100L, 100200L },
	["JOB-002"] = new HashSet<long> { 100100L },
};

var majorByJob = new Dictionary<string, HashSet<long>>(StringComparer.OrdinalIgnoreCase)
{
	["JOB-001"] = new HashSet<long> { 100000L },
	["JOB-002"] = new HashSet<long> { 100000L },
};

var jobCodeIndex = new JobCodeCategoryIndex(smallByJob, middleByJob, majorByJob);

// 1) 用一個或多個職缺代碼找到多筆職務小類
var smallCodes = jobCodeIndex.GetSmallCodes(new[] { "JOB-001", "JOB-002" });

// 2) 用一個或多個職缺代碼找到多筆職務中類
var middleCodes = jobCodeIndex.GetMiddleCodes(new[] { "JOB-001", "JOB-002" });

// 3) 用一個或多個職缺代碼找到職務大類（此範例為 {100000L}）
var majorCodes = jobCodeIndex.GetMajorCodes(new[] { "JOB-001", "JOB-002" });
```
