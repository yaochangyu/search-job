using SearchJob.Indexes;
using SearchJob.Models;

namespace SearchJob.Test.Indexes;

public sealed class JobCategoryHierarchyIndexTests
{
    private static IReadOnlyList<JobCategory> CreateCategories() =>
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
        new JobCategoryHierarchyIndex(CreateCategories());

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
    public void GetMinorCodesByJobIds_WhenJobsContainDuplicatesEmptyAndUnknownJobIds_ReturnsUnionedMinorCodes()
    {
        // Protects: JobVacancyIndex 會 union 多個 job 的 minorCodes，忽略重複 minor 與不存在的 jobId。
        var categoryIndex = CreateCategoryIndex();

        var jobs = new List<JobVacancy>
        {
            new JobVacancy(1, "job-1", "desc", new[] { 100101, 100101, 100105 }),
            new JobVacancy(2, "job-2", "desc", Array.Empty<int>()),
            new JobVacancy(3, "job-3", "desc", new[] { 100205 }),
        };

        var index = new JobVacancyIndex(categoryIndex, jobs);
        var result = index.GetMinorCodesByJobIds(new[] { 1, 3, 9999 });

        Assert.Equal(new HashSet<int> { 100101, 100105, 100205 }, result);
    }

    [Fact]
    public void GetMiddleCodesByJobIds_WhenDerivedFromMinorCodesViaHierarchyIndex_ReturnsExpectedMiddleCodes()
    {
        // Protects: JobVacancyIndex 的 middleCodes 來自「job minors -> hierarchy 推導」，且忽略未知 minor。
        var categoryIndex = CreateCategoryIndex();

        var jobs = new List<JobVacancy>
        {
            new JobVacancy(1, "job-1", "desc", new[] { 100101, 100105 }),
            new JobVacancy(3, "job-3", "desc", new[] { 100205, 999999 }),
        };

        var index = new JobVacancyIndex(categoryIndex, jobs);
        var result = index.GetMiddleCodesByJobIds(new[] { 1, 3 });

        Assert.Equal(new HashSet<int> { 100100, 100200 }, result);
    }

    [Fact]
    public void GetMajorCodesByJobIds_WhenDerivedFromMinorCodesViaHierarchyIndex_ReturnsExpectedMajorCodes()
    {
        // Protects: JobVacancyIndex 的 majorCodes 來自「job minors -> hierarchy 推導」,且忽略未知 minor。
        var categoryIndex = CreateCategoryIndex();

        var jobs = new List<JobVacancy>
        {
            new JobVacancy(1, "job-1", "desc", new[] { 100101, 100105 }),
            new JobVacancy(3, "job-3", "desc", new[] { 100205, 999999 }),
        };

        var index = new JobVacancyIndex(categoryIndex, jobs);
        var result = index.GetMajorCodesByJobIds(new[] { 1, 3 });

        Assert.Equal(new HashSet<int> { 100000 }, result);
    }
}
