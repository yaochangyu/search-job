using SearchJob.Indexes;
using SearchJob.Models;

namespace SearchJob.Test.Indexes;

public sealed class JobCategoryHierarchyIndexTests
{
    private static IReadOnlyList<JobCategory> CreateSpec2Categories() =>
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
        new JobCategoryHierarchyIndex(CreateSpec2Categories());

    [Fact]
    public void GetMajorCodesByMinorCodes_ReturnsMajorCodes()
    {
        var index = CreateCategoryIndex();

        var result = index.GetMajorCodesByMinorCodes(new[] { 100101, 100205 });

        Assert.Equal(new HashSet<int> { 100000 }, result);
    }

    [Fact]
    public void GetMiddleCodesByMinorCodes_ReturnsMiddleCodes()
    {
        var index = CreateCategoryIndex();

        var result = index.GetMiddleCodesByMinorCodes(new[] { 100101, 100205 });

        Assert.Equal(new HashSet<int> { 100100, 100200 }, result);
    }

    [Fact]
    public void GetMinorCodesByMiddleCodes_ReturnsMinorCodes()
    {
        var index = CreateCategoryIndex();

        var result = index.GetMinorCodesByMiddleCodes(new[] { 100100 });

        Assert.Equal(new HashSet<int> { 100101, 100105 }, result);
    }

    [Fact]
    public void GetMajorCodesByMiddleCodes_ReturnsMajorCodes()
    {
        var index = CreateCategoryIndex();

        var result = index.GetMajorCodesByMiddleCodes(new[] { 100100, 100200 });

        Assert.Equal(new HashSet<int> { 100000 }, result);
    }

    [Fact]
    public void GetMinorCodesByMajorCodes_ReturnsMinorCodes()
    {
        var index = CreateCategoryIndex();

        var result = index.GetMinorCodesByMajorCodes(new[] { 100000 });

        Assert.Equal(new HashSet<int> { 100101, 100105, 100205, 100206 }, result);
    }

    [Fact]
    public void GetMiddleCodesByMajorCodes_ReturnsMiddleCodes()
    {
        var index = CreateCategoryIndex();

        var result = index.GetMiddleCodesByMajorCodes(new[] { 100000 });

        Assert.Equal(new HashSet<int> { 100100, 100200 }, result);
    }

    [Fact]
    public void DuplicatesAndUnknownCodes_AreIgnoredAndDeDuplicated()
    {
        var index = CreateCategoryIndex();

        var majors = index.GetMajorCodesByMinorCodes(new[] { 100101, 100101, 999999 });
        var middles = index.GetMiddleCodesByMajorCodes(new[] { 100000, 100000, 999999 });
        var minors = index.GetMinorCodesByMiddleCodes(new[] { 100100, 100100, 999999 });

        Assert.Equal(new HashSet<int> { 100000 }, majors);
        Assert.Equal(new HashSet<int> { 100100, 100200 }, middles);
        Assert.Equal(new HashSet<int> { 100101, 100105 }, minors);
    }

    [Fact]
    public void JobToCategoryIndex_GetMinorCodesByJobIds_ReturnsUnionedMinorCodes()
    {
        var categoryIndex = CreateCategoryIndex();

        var jobs = new List<JobPosting>
        {
            new JobPosting(1, "job-1", "desc", new[] { 100101, 100101, 100105 }),
            new JobPosting(2, "job-2", "desc", Array.Empty<int>()),
            new JobPosting(3, "job-3", "desc", new[] { 100205 }),
        };

        var index = new JobToCategoryIndex(categoryIndex, jobs);
        var result = index.GetMinorCodesByJobIds(new[] { 1, 3, 9999 });

        Assert.Equal(new HashSet<int> { 100101, 100105, 100205 }, result);
    }

    [Fact]
    public void JobToCategoryIndex_GetMiddleCodesByJobIds_IsDerivedFromMinorViaHierarchyIndex()
    {
        var categoryIndex = CreateCategoryIndex();

        var jobs = new List<JobPosting>
        {
            new JobPosting(1, "job-1", "desc", new[] { 100101, 100105 }),
            new JobPosting(3, "job-3", "desc", new[] { 100205, 999999 }),
        };

        var index = new JobToCategoryIndex(categoryIndex, jobs);
        var result = index.GetMiddleCodesByJobIds(new[] { 1, 3 });

        Assert.Equal(new HashSet<int> { 100100, 100200 }, result);
    }

    [Fact]
    public void JobToCategoryIndex_GetMajorCodesByJobIds_IsDerivedFromMinorViaHierarchyIndex()
    {
        var categoryIndex = CreateCategoryIndex();

        var jobs = new List<JobPosting>
        {
            new JobPosting(1, "job-1", "desc", new[] { 100101, 100105 }),
            new JobPosting(3, "job-3", "desc", new[] { 100205, 999999 }),
        };

        var index = new JobToCategoryIndex(categoryIndex, jobs);
        var result = index.GetMajorCodesByJobIds(new[] { 1, 3 });

        Assert.Equal(new HashSet<int> { 100000 }, result);
    }
}
