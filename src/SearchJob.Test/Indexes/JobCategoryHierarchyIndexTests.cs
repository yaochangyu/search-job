using SearchJob.Indexes;
using SearchJob.Models;

namespace SearchJob.Test.Indexes;

public sealed class JobCategoryHierarchyIndexTests
{
    private static IReadOnlyList<JobCategory> CreateJobCategories() =>
        new List<JobCategory>
        {
            new(100000, "管理幕僚／人資／行政", null, JobCategoryLevel.Major),

            new(100100, "管理幕僚", 100000, JobCategoryLevel.Middle),
            new(100200, "人力資源", 100000, JobCategoryLevel.Middle),

            new(100101, "經營管理主管", 100100, JobCategoryLevel.Minor),
            new(100105, "特別助理", 100100, JobCategoryLevel.Minor),
            new(100205, "人事助理", 100200, JobCategoryLevel.Minor),
            new(100206, "就業服務員", 100200, JobCategoryLevel.Minor),
        };

    private static JobCategoryHierarchyIndex CreateCategoryIndex() =>
        new JobCategoryHierarchyIndex(CreateJobCategories());

    [Fact]
    public void GetMajorCodesByMinorCodes_WhenMinorCodesProvided_ReturnsMajorCodes()
    {
        // Protects: minorCodes -> majorCodes 的祖先映射正確（多個 minor 可 union）。
        var index = CreateCategoryIndex();

        var result = index.GetMajorCodesByMinorCodes(new[] { 100101, 100205 });

        Assert.Equal(new HashSet<int> { 100000 }, result);
    }

    [Fact]
    public void GetMiddleCodesByMinorCodes_WhenMinorCodesProvided_ReturnsMiddleCodes()
    {
        // Protects: minorCodes -> middleCodes 的祖先映射正確（可跨多個 middle 匯總）。
        var index = CreateCategoryIndex();

        var result = index.GetMiddleCodesByMinorCodes(new[] { 100101, 100205 });

        Assert.Equal(new HashSet<int> { 100100, 100200 }, result);
    }

    [Fact]
    public void GetMinorCodesByMiddleCodes_WhenMiddleCodesProvided_ReturnsMinorCodes()
    {
        // Protects: middleCodes -> minorCodes 的子集合映射正確。
        var index = CreateCategoryIndex();

        var result = index.GetMinorCodesByMiddleCodes(new[] { 100100 });

        Assert.Equal(new HashSet<int> { 100101, 100105 }, result);
    }

    [Fact]
    public void GetMajorCodesByMiddleCodes_WhenMiddleCodesProvided_ReturnsMajorCodes()
    {
        // Protects: middleCodes -> majorCodes 的祖先映射正確（可去重）。
        var index = CreateCategoryIndex();

        var result = index.GetMajorCodesByMiddleCodes(new[] { 100100, 100200 });

        Assert.Equal(new HashSet<int> { 100000 }, result);
    }

    [Fact]
    public void GetMinorCodesByMajorCodes_WhenMajorCodesProvided_ReturnsMinorCodes()
    {
        // Protects: majorCodes -> 所有後代 minorCodes 的展開正確（跨 middle 匯總）。
        var index = CreateCategoryIndex();

        var result = index.GetMinorCodesByMajorCodes(new[] { 100000 });

        Assert.Equal(new HashSet<int> { 100101, 100105, 100205, 100206 }, result);
    }

    [Fact]
    public void GetMiddleCodesByMajorCodes_WhenMajorCodesProvided_ReturnsMiddleCodes()
    {
        // Protects: majorCodes -> middleCodes 的子集合映射正確。
        var index = CreateCategoryIndex();

        var result = index.GetMiddleCodesByMajorCodes(new[] { 100000 });

        Assert.Equal(new HashSet<int> { 100100, 100200 }, result);
    }

    [Fact]
    public void GetCodesByVariousLevels_WhenInputHasDuplicatesOrUnknown_IgnoresAndDeDuplicates()
    {
        // Protects: 任何層級查詢在輸入含重複/未知 codes 時，皆忽略未知並輸出去重。
        var index = CreateCategoryIndex();

        var majors = index.GetMajorCodesByMinorCodes(new[] { 100101, 100101, 999999 });
        var middles = index.GetMiddleCodesByMajorCodes(new[] { 100000, 100000, 999999 });
        var minors = index.GetMinorCodesByMiddleCodes(new[] { 100100, 100100, 999999 });

        Assert.Equal(new HashSet<int> { 100000 }, majors);
        Assert.Equal(new HashSet<int> { 100100, 100200 }, middles);
        Assert.Equal(new HashSet<int> { 100101, 100105 }, minors);
    }

    [Fact]
    public void GetMajorCodesByMinorCodes_WhenInputHasDuplicatesOrUnknown_IgnoresDuplicatesAndUnknown()
    {
        // Protects: minorCodes 輸入含重複/未知時，仍能正確推導 majorCodes，且忽略無效輸入。
        var index = CreateCategoryIndex();

        var result = index.GetMajorCodesByMinorCodes(new[] { 100101, 100105, 100101, 999999 });

        Assert.Equal(new HashSet<int> { 100000 }, result);
    }

    [Fact]
    public void GetMiddleCodesByMinorCodes_WhenInputHasDuplicatesOrUnknown_IgnoresDuplicatesAndUnknown()
    {
        // Protects: minorCodes 輸入含重複/未知時，仍能正確推導 middleCodes，且忽略無效輸入。
        var index = CreateCategoryIndex();

        var result = index.GetMiddleCodesByMinorCodes(new[] { 100101, 100205, 100205, 999999 });

        Assert.Equal(new HashSet<int> { 100100, 100200 }, result);
    }

    [Fact]
    public void GetMinorCodesByMiddleCodes_WhenInputHasDuplicatesOrUnknown_IgnoresDuplicatesAndUnknown()
    {
        // Protects: middleCodes 輸入含重複/未知時，仍能正確取得所有對應 minorCodes，且輸出去重。
        var index = CreateCategoryIndex();

        var result = index.GetMinorCodesByMiddleCodes(new[] { 100100, 100100, 999999 });

        Assert.Equal(new HashSet<int> { 100101, 100105 }, result);
    }

    [Fact]
    public void GetMajorCodesByMiddleCodes_WhenInputHasDuplicatesOrUnknown_IgnoresDuplicatesAndUnknown()
    {
        // Protects: middleCodes 輸入含重複/未知時，仍能正確推導 majorCodes，且忽略無效輸入。
        var index = CreateCategoryIndex();

        var result = index.GetMajorCodesByMiddleCodes(new[] { 100100, 100200, 100200, 999999 });

        Assert.Equal(new HashSet<int> { 100000 }, result);
    }

    [Fact]
    public void GetMinorCodesByMajorCodes_WhenInputHasDuplicatesOrUnknown_IgnoresDuplicatesAndUnknown()
    {
        // Protects: majorCodes 輸入含重複/未知時，仍能正確展開其後代 minorCodes，且輸出去重。
        var index = CreateCategoryIndex();

        var result = index.GetMinorCodesByMajorCodes(new[] { 100000, 100000, 999999 });

        Assert.Equal(new HashSet<int> { 100101, 100105, 100205, 100206 }, result);
    }

    [Fact]
    public void GetMiddleCodesByMajorCodes_WhenInputHasDuplicatesOrUnknown_IgnoresDuplicatesAndUnknown()
    {
        // Protects: majorCodes 輸入含重複/未知時，仍能正確取得 middleCodes，且輸出去重。
        var index = CreateCategoryIndex();

        var result = index.GetMiddleCodesByMajorCodes(new[] { 100000, 100000, 999999 });

        Assert.Equal(new HashSet<int> { 100100, 100200 }, result);
    }

    [Fact]
    public void Constructor_WhenInputOrderIsReversed_BuildsHierarchyInMajorMiddleMinorOrder()
    {
        // Protects: 建置不依賴輸入順序；即使 categories 亂序仍能成功建立階層索引。
        var categories = new List<JobCategory>
        {
            new(100101, "經營管理主管", 100100, JobCategoryLevel.Minor),
            new(100105, "特別助理", 100100, JobCategoryLevel.Minor),
            new(100205, "人事助理", 100200, JobCategoryLevel.Minor),
            new(100206, "就業服務員", 100200, JobCategoryLevel.Minor),

            new(100100, "管理幕僚", 100000, JobCategoryLevel.Middle),
            new(100200, "人力資源", 100000, JobCategoryLevel.Middle),

            new(100000, "管理幕僚／人資／行政", null, JobCategoryLevel.Major),
        };

        var index = new JobCategoryHierarchyIndex(categories);

        Assert.Equal(new HashSet<int> { 100000 }, index.GetMajorCodesByMinorCodes(new[] { 100101, 100205 }));
        Assert.Equal(new HashSet<int> { 100100, 100200 }, index.GetMiddleCodesByMajorCodes(new[] { 100000 }));
        Assert.Equal(new HashSet<int> { 100101, 100105 }, index.GetMinorCodesByMiddleCodes(new[] { 100100 }));
    }

    [Fact]
    public void Constructor_WhenCategoriesIsSinglePassEnumerable_DoesNotEnumerateMultipleTimes()
    {
        // Protects: 建置流程不應依賴可重複列舉的 IEnumerable。
        var categories = new List<JobCategory>
        {
            new(100101, "經營管理主管", 100100, JobCategoryLevel.Minor),
            new(100105, "特別助理", 100100, JobCategoryLevel.Minor),
            new(100205, "人事助理", 100200, JobCategoryLevel.Minor),
            new(100206, "就業服務員", 100200, JobCategoryLevel.Minor),

            new(100100, "管理幕僚", 100000, JobCategoryLevel.Middle),
            new(100200, "人力資源", 100000, JobCategoryLevel.Middle),

            new(100000, "管理幕僚／人資／行政", null, JobCategoryLevel.Major),
        };

        var singlePass = new SinglePassEnumerable<JobCategory>(categories);

        var index = new JobCategoryHierarchyIndex(singlePass);

        Assert.Equal(new HashSet<int> { 100000 }, index.GetMajorCodesByMinorCodes(new[] { 100101, 100205 }));
    }

    [Fact]
    public void Constructor_WhenMiddleReferencesMissingMajor_ThrowsArgumentException()
    {
        // Protects: Middle 指向不存在的 Major 時必須 fail fast，避免建立不一致索引。
        var categories = new List<JobCategory>
        {
            new(100100, "管理幕僚", 999999, JobCategoryLevel.Middle),
        };

        Assert.Throws<ArgumentException>(() => new JobCategoryHierarchyIndex(categories));
    }

    [Fact]
    public void Constructor_WhenDuplicateCodesExist_ThrowsArgumentException()
    {
        // Protects: JobCategory code 必須唯一；重複 code 應丟出例外避免覆蓋/不確定。
        var categories = new List<JobCategory>
        {
            new(100000, "管理幕僚／人資／行政", null, JobCategoryLevel.Major),
            new(100000, "重複代碼", null, JobCategoryLevel.Major),
        };

        Assert.Throws<ArgumentException>(() => new JobCategoryHierarchyIndex(categories));
    }

    private sealed class SinglePassEnumerable<T> : IEnumerable<T>
    {
        private readonly IEnumerable<T> _source;
        private bool _hasBeenEnumerated;

        public SinglePassEnumerable(IEnumerable<T> source)
        {
            _source = source;
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (_hasBeenEnumerated)
            {
                throw new InvalidOperationException("This sequence can only be enumerated once.");
            }

            _hasBeenEnumerated = true;
            return _source.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
