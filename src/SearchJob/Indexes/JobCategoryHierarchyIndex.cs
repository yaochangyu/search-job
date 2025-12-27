using SearchJob.Models;

namespace SearchJob.Indexes;

/// <summary>
/// 職務類別（大／中／小）層級索引。
/// 
/// 設計目的（對應 spec-2 第 4 章）：
/// - 以 <see cref="Dictionary{TKey,TValue}"/> 與 <see cref="HashSet{T}"/> 建立索引。
/// - 單次查找以 Hash 結構為主，平均時間複雜度期望為 O(1)（TryGetValue/Contains）。
/// - 支援「輸入一個或多個代碼」查出「多筆關聯代碼集合」。
/// 
/// 回傳集合規則（對應 spec-2 第 4.3 節）：
/// - 輸入若包含重複代碼：等同同一代碼，輸出不重複。
/// - 輸入若包含不存在代碼：忽略該代碼，不應導致查詢失敗。
/// - 輸出使用集合語意（HashSet），不保證順序。
/// 
/// 注意：建構子會對類別資料做層級約束檢查（對應 spec-2 第 2.1 節），
/// 若資料本身違反「父代碼必須存在」等規則，會直接丟出例外。
/// </summary>
public sealed class JobCategoryHierarchyIndex
{
    // Code -> Entity（若未來要回傳完整物件可使用；目前查詢需求主要回傳 code 集合）
    private readonly Dictionary<int, JobCategory> _majorByCode;
    private readonly Dictionary<int, JobCategory> _middleByCode;
    private readonly Dictionary<int, JobCategory> _minorByCode;

    // 反查：小類 -> 中類；中類 -> 大類
    private readonly Dictionary<int, int> _middleCodeByMinorCode;
    private readonly Dictionary<int, int> _majorCodeByMiddleCode;

    // 聚合：中類 -> 小類集合；大類 -> 中類集合；大類 -> 小類集合
    private readonly Dictionary<int, HashSet<int>> _minorCodesByMiddleCode;
    private readonly Dictionary<int, HashSet<int>> _middleCodesByMajorCode;
    private readonly Dictionary<int, HashSet<int>> _minorCodesByMajorCode;

    // 用來偵測是否有重複 code（同一份資料內不允許重複）。
    private readonly HashSet<int> _allCodes;

    /// <summary>
    /// 建立層級索引。
    /// </summary>
    /// <param name="categories">所有職務類別（大／中／小）的集合。</param>
    /// <exception cref="ArgumentNullException">categories 為 null。</exception>
    /// <exception cref="ArgumentException">
    /// 類別資料違反層級規則：
    /// - 大類不允許 ParentCode
    /// - 中類/小類必須有 ParentCode 且 ParentCode 必須指向存在的上層類別
    /// - 類別代碼不可重複
    /// </exception>
    public JobCategoryHierarchyIndex(IEnumerable<JobCategory> categories)
    {
        ArgumentNullException.ThrowIfNull(categories);

        // Avoid multiple enumeration: callers may pass single-pass iterators.
        // Materialize once and build indexes from the in-memory list.
        var categoryList = categories as IList<JobCategory> ?? categories.ToList();

        _majorByCode = new Dictionary<int, JobCategory>();
        _middleByCode = new Dictionary<int, JobCategory>();
        _minorByCode = new Dictionary<int, JobCategory>();

        _middleCodeByMinorCode = new Dictionary<int, int>();
        _majorCodeByMiddleCode = new Dictionary<int, int>();

        _minorCodesByMiddleCode = new Dictionary<int, HashSet<int>>();
        _middleCodesByMajorCode = new Dictionary<int, HashSet<int>>();
        _minorCodesByMajorCode = new Dictionary<int, HashSet<int>>();

        _allCodes = new HashSet<int>();

        // 對應 spec-2 4.0 #2：把職務類別分別寫入到索引物件（建議順序：Major -> Middle -> Minor）。
        WriteAllCategories(categoryList);
    }

    private void WriteAllCategories(IEnumerable<JobCategory> categories)
    {
        // 1) majors：先建立大類索引，因為中類會引用到大類。
        foreach (var category in categories.Where(c => c.Level == JobCategoryLevel.Major))
        {
            if (category.ParentCode is not null)
            {
                throw new ArgumentException($"Major category must not have ParentCode. Code={category.Code}", nameof(categories));
            }

            if (!_allCodes.Add(category.Code))
            {
                throw new ArgumentException($"Duplicate category code found. Code={category.Code}", nameof(categories));
            }

            _majorByCode[category.Code] = category;
            // 預先建立空集合，避免後續 Add 時需要額外判斷。
            _middleCodesByMajorCode.TryAdd(category.Code, new HashSet<int>());
            _minorCodesByMajorCode.TryAdd(category.Code, new HashSet<int>());
        }

        // 2) middles：建立中類索引與「中類 -> 大類」映射。
        foreach (var category in categories.Where(c => c.Level == JobCategoryLevel.Middle))
        {
            if (category.ParentCode is null)
            {
                throw new ArgumentException($"Middle category must have ParentCode. Code={category.Code}", nameof(categories));
            }

            var majorCode = category.ParentCode.Value;
            if (!_majorByCode.ContainsKey(majorCode))
            {
                throw new ArgumentException(
                    $"Middle category ParentCode must reference an existing major code. Middle={category.Code}, Parent={majorCode}",
                    nameof(categories));
            }

            if (!_allCodes.Add(category.Code))
            {
                throw new ArgumentException($"Duplicate category code found. Code={category.Code}", nameof(categories));
            }

            _middleByCode[category.Code] = category;
            _majorCodeByMiddleCode[category.Code] = majorCode;

            // 預先建立：中類 -> 小類集合。
            _minorCodesByMiddleCode.TryAdd(category.Code, new HashSet<int>());

            if (!_middleCodesByMajorCode.TryGetValue(majorCode, out var middleSet))
            {
                middleSet = new HashSet<int>();
                _middleCodesByMajorCode[majorCode] = middleSet;
            }

            middleSet.Add(category.Code);
        }

        // 3) minors：建立小類索引與「小類 -> 中類」映射，並同步更新中類/大類的聚合集合。
        foreach (var category in categories.Where(c => c.Level == JobCategoryLevel.Minor))
        {
            if (category.ParentCode is null)
            {
                throw new ArgumentException($"Minor category must have ParentCode. Code={category.Code}", nameof(categories));
            }

            var middleCode = category.ParentCode.Value;
            if (!_middleByCode.ContainsKey(middleCode))
            {
                throw new ArgumentException(
                    $"Minor category ParentCode must reference an existing middle code. Minor={category.Code}, Parent={middleCode}",
                    nameof(categories));
            }

            if (!_allCodes.Add(category.Code))
            {
                throw new ArgumentException($"Duplicate category code found. Code={category.Code}", nameof(categories));
            }

            _minorByCode[category.Code] = category;
            _middleCodeByMinorCode[category.Code] = middleCode;

            if (!_minorCodesByMiddleCode.TryGetValue(middleCode, out var minorSetInMiddle))
            {
                minorSetInMiddle = new HashSet<int>();
                _minorCodesByMiddleCode[middleCode] = minorSetInMiddle;
            }

            minorSetInMiddle.Add(category.Code);

            // 由「中類 -> 大類」推導出此小類所屬的大類。
            var majorCode = _majorCodeByMiddleCode[middleCode];
            if (!_minorCodesByMajorCode.TryGetValue(majorCode, out var minorSetInMajor))
            {
                minorSetInMajor = new HashSet<int>();
                _minorCodesByMajorCode[majorCode] = minorSetInMajor;
            }

            minorSetInMajor.Add(category.Code);
        }
    }

    /// <summary>
    /// 必備查詢 #1：用一個或多個「職務小類」找到多個「職務大類」。
    /// </summary>
    /// <param name="minorCodes">小類代碼集合（允許重複；不存在的代碼會被忽略）。</param>
    /// <returns>大類代碼集合（不重複、不保證順序）。</returns>
    public IReadOnlySet<int> GetMajorCodesByMinorCodes(IEnumerable<int> minorCodes)
    {
        ArgumentNullException.ThrowIfNull(minorCodes);

        var result = new HashSet<int>();
        foreach (var minorCode in minorCodes)
        {
            if (!_middleCodeByMinorCode.TryGetValue(minorCode, out var middleCode))
            {
                continue;
            }

            if (_majorCodeByMiddleCode.TryGetValue(middleCode, out var majorCode))
            {
                result.Add(majorCode);
            }
        }

        return result;
    }

    /// <summary>
    /// 必備查詢 #2：用一個或多個「職務小類」找到多個「職務中類」。
    /// </summary>
    /// <param name="minorCodes">小類代碼集合（允許重複；不存在的代碼會被忽略）。</param>
    /// <returns>中類代碼集合（不重複、不保證順序）。</returns>
    public IReadOnlySet<int> GetMiddleCodesByMinorCodes(IEnumerable<int> minorCodes)
    {
        ArgumentNullException.ThrowIfNull(minorCodes);

        var result = new HashSet<int>();
        foreach (var minorCode in minorCodes)
        {
            if (_middleCodeByMinorCode.TryGetValue(minorCode, out var middleCode))
            {
                result.Add(middleCode);
            }
        }

        return result;
    }

    /// <summary>
    /// 必備查詢 #3：用一個或多個「職務中類」找到多個「職務小類」。
    /// </summary>
    /// <param name="middleCodes">中類代碼集合（允許重複；不存在的代碼會被忽略）。</param>
    /// <returns>小類代碼集合（不重複、不保證順序）。</returns>
    public IReadOnlySet<int> GetMinorCodesByMiddleCodes(IEnumerable<int> middleCodes)
    {
        ArgumentNullException.ThrowIfNull(middleCodes);

        var result = new HashSet<int>();
        foreach (var middleCode in middleCodes)
        {
            if (!_minorCodesByMiddleCode.TryGetValue(middleCode, out var minorCodes))
            {
                continue;
            }

            result.UnionWith(minorCodes);
        }

        return result;
    }

    /// <summary>
    /// 必備查詢 #4：用一個或多個「職務中類」找到多個「職務大類」。
    /// </summary>
    /// <param name="middleCodes">中類代碼集合（允許重複；不存在的代碼會被忽略）。</param>
    /// <returns>大類代碼集合（不重複、不保證順序）。</returns>
    public IReadOnlySet<int> GetMajorCodesByMiddleCodes(IEnumerable<int> middleCodes)
    {
        ArgumentNullException.ThrowIfNull(middleCodes);

        var result = new HashSet<int>();
        foreach (var middleCode in middleCodes)
        {
            if (_majorCodeByMiddleCode.TryGetValue(middleCode, out var majorCode))
            {
                result.Add(majorCode);
            }
        }

        return result;
    }

    /// <summary>
    /// 必備查詢 #5：用一個或多個「職務大類」找到多個「職務小類」。
    /// </summary>
    /// <param name="majorCodes">大類代碼集合（允許重複；不存在的代碼會被忽略）。</param>
    /// <returns>小類代碼集合（不重複、不保證順序）。</returns>
    public IReadOnlySet<int> GetMinorCodesByMajorCodes(IEnumerable<int> majorCodes)
    {
        ArgumentNullException.ThrowIfNull(majorCodes);

        var result = new HashSet<int>();
        foreach (var majorCode in majorCodes)
        {
            if (!_minorCodesByMajorCode.TryGetValue(majorCode, out var minorCodes))
            {
                continue;
            }

            result.UnionWith(minorCodes);
        }

        return result;
    }

    /// <summary>
    /// 必備查詢 #6：用一個或多個「職務大類」找到多個「職務中類」。
    /// </summary>
    /// <param name="majorCodes">大類代碼集合（允許重複；不存在的代碼會被忽略）。</param>
    /// <returns>中類代碼集合（不重複、不保證順序）。</returns>
    public IReadOnlySet<int> GetMiddleCodesByMajorCodes(IEnumerable<int> majorCodes)
    {
        ArgumentNullException.ThrowIfNull(majorCodes);

        var result = new HashSet<int>();
        foreach (var majorCode in majorCodes)
        {
            if (!_middleCodesByMajorCode.TryGetValue(majorCode, out var middleCodes))
            {
                continue;
            }

            result.UnionWith(middleCodes);
        }

        return result;
    }
}
