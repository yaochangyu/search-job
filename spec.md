## 職務類別規則

職務類別規則包含「大類／中類／小類」，每一層級皆有名稱與代碼。

### 大類

| 名稱 | 代碼 |
| --- | --- |
| 管理幕僚／人資／行政 | 1 |

### 中類

| 名稱 | 代碼 |
| --- | --- |
| 管理幕僚 | 1-1 |
| 人力資源 | 1-2 |

### 小類

| 名稱 | 代碼 |
| --- | --- |
| 經營管理主管 | 1-1-1 |
| 特別助理 | 1-1-2 |
| 人力助理 | 1-2-1 |
| 就業服務員 | 1-2-2 |

## 核心技術

- C#
- MSSQL

## 物件設計：職務類別（大／中／小）索引

目標：用「時間複雜度 O(1)」的型別（`Dictionary` / `HashSet`）建立索引，滿足以下查詢：

- 用一個或多個「職務小類」找到「大類」
- 用一個或多個「職務小類」找到「中類」
- 用一個或多個「職務大類」找到「小類集合」
- 用一個或多個「職務大類」找到「中類集合」

### C# 資料模型

```csharp
public sealed record MajorCategory(string Code, string Name);
public sealed record MiddleCategory(string Code, string Name, string MajorCode);
public sealed record SmallCategory(string Code, string Name, string MiddleCode, string MajorCode);
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
	private readonly Dictionary<string, MajorCategory> _majorByCode;
	private readonly Dictionary<string, MiddleCategory> _middleByCode;
	private readonly Dictionary<string, SmallCategory> _smallByCode;

	// 反查：小類 -> 中類/大類
	private readonly Dictionary<string, string> _middleCodeBySmallCode;
	private readonly Dictionary<string, string> _majorCodeBySmallCode;

	// 聚合：大類 -> 小類集合 / 中類集合
	private readonly Dictionary<string, HashSet<string>> _smallCodesByMajorCode;
	private readonly Dictionary<string, HashSet<string>> _middleCodesByMajorCode;

	public JobCategoryIndex(
		IEnumerable<MajorCategory> majors,
		IEnumerable<MiddleCategory> middles,
		IEnumerable<SmallCategory> smalls)
	{
		_majorByCode = new(StringComparer.OrdinalIgnoreCase);
		_middleByCode = new(StringComparer.OrdinalIgnoreCase);
		_smallByCode = new(StringComparer.OrdinalIgnoreCase);

		_middleCodeBySmallCode = new(StringComparer.OrdinalIgnoreCase);
		_majorCodeBySmallCode = new(StringComparer.OrdinalIgnoreCase);

		_smallCodesByMajorCode = new(StringComparer.OrdinalIgnoreCase);
		_middleCodesByMajorCode = new(StringComparer.OrdinalIgnoreCase);

		foreach (var major in majors)
		{
			_majorByCode[major.Code] = major;
			_smallCodesByMajorCode.TryAdd(major.Code, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
			_middleCodesByMajorCode.TryAdd(major.Code, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
		}

		foreach (var middle in middles)
		{
			_middleByCode[middle.Code] = middle;
			if (!_middleCodesByMajorCode.TryGetValue(middle.MajorCode, out var middleSet))
			{
				middleSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				_middleCodesByMajorCode[middle.MajorCode] = middleSet;
			}
			middleSet.Add(middle.Code);
		}

		foreach (var small in smalls)
		{
			_smallByCode[small.Code] = small;
			_middleCodeBySmallCode[small.Code] = small.MiddleCode;
			_majorCodeBySmallCode[small.Code] = small.MajorCode;

			if (!_smallCodesByMajorCode.TryGetValue(small.MajorCode, out var smallSet))
			{
				smallSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				_smallCodesByMajorCode[small.MajorCode] = smallSet;
			}
			smallSet.Add(small.Code);

			if (!_middleCodesByMajorCode.TryGetValue(small.MajorCode, out var middleSet))
			{
				middleSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				_middleCodesByMajorCode[small.MajorCode] = middleSet;
			}
			middleSet.Add(small.MiddleCode);
		}
	}

	public IReadOnlyCollection<MajorCategory> GetMajorsBySmallCodes(IEnumerable<string> smallCodes)
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

	public IReadOnlyCollection<MiddleCategory> GetMiddlesBySmallCodes(IEnumerable<string> smallCodes)
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

	public IReadOnlyCollection<SmallCategory> GetSmallsByMajorCodes(IEnumerable<string> majorCodes)
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

	public IReadOnlyCollection<MiddleCategory> GetMiddlesByMajorCodes(IEnumerable<string> majorCodes)
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
	new MajorCategory("1", "管理幕僚／人資／行政"),
};

var middles = new[]
{
	new MiddleCategory("1-1", "管理幕僚", "1"),
	new MiddleCategory("1-2", "人力資源", "1"),
};

var smalls = new[]
{
	new SmallCategory("1-1-1", "經營管理主管", "1-1", "1"),
	new SmallCategory("1-1-2", "特別助理", "1-1", "1"),
	new SmallCategory("1-2-1", "人力助理", "1-2", "1"),
	new SmallCategory("1-2-2", "就業服務員", "1-2", "1"),
};

var index = new JobCategoryIndex(majors, middles, smalls);

// 1) 用一個或多個小類找到大類
var majors1 = index.GetMajorsBySmallCodes(new[] { "1-2-1", "1-1-2" });

// 2) 用一個或多個小類找到中類
var middles1 = index.GetMiddlesBySmallCodes(new[] { "1-2-1", "1-1-2" });

// 3) 用一個或多個大類找到小類集合
var smalls1 = index.GetSmallsByMajorCodes(new[] { "1" });

// 4) 用一個或多個大類找到中類集合
var middles2 = index.GetMiddlesByMajorCodes(new[] { "1" });
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
	private readonly Dictionary<string, HashSet<string>> _smallCodesByJobCode;
	private readonly Dictionary<string, HashSet<string>> _middleCodesByJobCode;
	private readonly Dictionary<string, HashSet<string>> _majorCodesByJobCode;

	public JobCodeCategoryIndex(
		Dictionary<string, HashSet<string>> smallCodesByJobCode,
		Dictionary<string, HashSet<string>> middleCodesByJobCode,
		Dictionary<string, HashSet<string>> majorCodesByJobCode)
	{
		_smallCodesByJobCode = smallCodesByJobCode;
		_middleCodesByJobCode = middleCodesByJobCode;
		_majorCodesByJobCode = majorCodesByJobCode;
	}

	public IReadOnlyCollection<string> GetSmallCodes(IEnumerable<string> jobCodes)
		=> UnionAll(_smallCodesByJobCode, jobCodes);

	public IReadOnlyCollection<string> GetMiddleCodes(IEnumerable<string> jobCodes)
		=> UnionAll(_middleCodesByJobCode, jobCodes);

	public IReadOnlyCollection<string> GetMajorCodes(IEnumerable<string> jobCodes)
		=> UnionAll(_majorCodesByJobCode, jobCodes);

	private static IReadOnlyCollection<string> UnionAll(
		Dictionary<string, HashSet<string>> map,
		IEnumerable<string> keys)
	{
		var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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
	["JOB-001"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "1-1-2", "1-2-1" },
	["JOB-002"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "1-1-1" },
};

var middleByJob = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
{
	["JOB-001"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "1-1", "1-2" },
	["JOB-002"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "1-1" },
};

var majorByJob = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
{
	["JOB-001"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "1" },
	["JOB-002"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "1" },
};

var jobCodeIndex = new JobCodeCategoryIndex(smallByJob, middleByJob, majorByJob);

// 1) 用一個或多個職缺代碼找到多筆職務小類
var smallCodes = jobCodeIndex.GetSmallCodes(new[] { "JOB-001", "JOB-002" });

// 2) 用一個或多個職缺代碼找到多筆職務中類
var middleCodes = jobCodeIndex.GetMiddleCodes(new[] { "JOB-001", "JOB-002" });

// 3) 用一個或多個職缺代碼找到職務大類（此範例為 {"1"}）
var majorCodes = jobCodeIndex.GetMajorCodes(new[] { "JOB-001", "JOB-002" });
```
