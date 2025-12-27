using SearchJob.Indexes;
using SearchJob.Models;

namespace SearchJob.Test.Indexes;

public sealed class JobIndexesTests
{
    private static JobCategoryHierarchyIndex CreateCategoryIndex()
    {
        var categories = new List<JobCategory>
        {
            // Major
            new JobCategory(100000, "管理幕僚／人資／行政", ParentCode: null, JobCategoryLevel.Major),

            // Middle
            new JobCategory(100100, "管理幕僚", ParentCode: 100000, JobCategoryLevel.Middle),
            new JobCategory(100200, "人力資源", ParentCode: 100000, JobCategoryLevel.Middle),

            // Minor
            new JobCategory(100101, "經營管理主管", ParentCode: 100100, JobCategoryLevel.Minor),
            new JobCategory(100105, "特別助理", ParentCode: 100100, JobCategoryLevel.Minor),
            new JobCategory(100205, "人事助理", ParentCode: 100200, JobCategoryLevel.Minor),
            new JobCategory(100206, "就業服務員", ParentCode: 100200, JobCategoryLevel.Minor),
        };

        return new JobCategoryHierarchyIndex(categories);
    }

    private static void AssertSetEquals(IEnumerable<int> expected, IReadOnlySet<int> actual)
    {
        var expectedSet = new HashSet<int>(expected);
        var actualSet = new HashSet<int>(actual);
        Assert.True(actualSet.SetEquals(expectedSet), $"Expected: [{string.Join(",", expectedSet)}], Actual: [{string.Join(",", actualSet)}]");
    }

    [Fact]
    public void CategoryIndex_GetMajorCodesByMinorCodes_IgnoresDuplicatesAndUnknown()
    {
        var index = CreateCategoryIndex();

        var result = index.GetMajorCodesByMinorCodes(new[] { 100101, 100105, 100101, 999999 });

        AssertSetEquals(new[] { 100000 }, result);
    }

    [Fact]
    public void CategoryIndex_GetMiddleCodesByMinorCodes_IgnoresDuplicatesAndUnknown()
    {
        var index = CreateCategoryIndex();

        var result = index.GetMiddleCodesByMinorCodes(new[] { 100101, 100205, 100205, 999999 });

        AssertSetEquals(new[] { 100100, 100200 }, result);
    }

    [Fact]
    public void CategoryIndex_GetMinorCodesByMiddleCodes_IgnoresDuplicatesAndUnknown()
    {
        var index = CreateCategoryIndex();

        var result = index.GetMinorCodesByMiddleCodes(new[] { 100100, 100100, 999999 });

        AssertSetEquals(new[] { 100101, 100105 }, result);
    }

    [Fact]
    public void CategoryIndex_GetMajorCodesByMiddleCodes_IgnoresDuplicatesAndUnknown()
    {
        var index = CreateCategoryIndex();

        var result = index.GetMajorCodesByMiddleCodes(new[] { 100100, 100200, 100200, 999999 });

        AssertSetEquals(new[] { 100000 }, result);
    }

    [Fact]
    public void CategoryIndex_GetMinorCodesByMajorCodes_IgnoresDuplicatesAndUnknown()
    {
        var index = CreateCategoryIndex();

        var result = index.GetMinorCodesByMajorCodes(new[] { 100000, 100000, 999999 });

        AssertSetEquals(new[] { 100101, 100105, 100205, 100206 }, result);
    }

    [Fact]
    public void CategoryIndex_GetMiddleCodesByMajorCodes_IgnoresDuplicatesAndUnknown()
    {
        var index = CreateCategoryIndex();

        var result = index.GetMiddleCodesByMajorCodes(new[] { 100000, 100000, 999999 });

        AssertSetEquals(new[] { 100100, 100200 }, result);
    }

    [Fact]
    public void JobToCategoryIndex_GetCodesByJobIds_ReturnsExpectedSets()
    {
        var categoryIndex = CreateCategoryIndex();

        var jobs = new List<JobPosting>
        {
            new JobPosting(1, "Job 1", "desc", new[] { 100101, 100101, 100205 }),
            new JobPosting(2, "Job 2", "desc", minorCodes: null),
            new JobPosting(3, "Job 3", "desc", new[] { 100206 }),
        };

        var jobIndex = new JobToCategoryIndex(categoryIndex, jobs);

        var minorCodes = jobIndex.GetMinorCodesByJobIds(new[] { 1, 3, 3, 999 });
        AssertSetEquals(new[] { 100101, 100205, 100206 }, minorCodes);

        var middleCodes = jobIndex.GetMiddleCodesByJobIds(new[] { 1, 3, 999 });
        AssertSetEquals(new[] { 100100, 100200 }, middleCodes);

        var majorCodes = jobIndex.GetMajorCodesByJobIds(new[] { 1, 3, 999 });
        AssertSetEquals(new[] { 100000 }, majorCodes);
    }

    [Fact]
    public void MajorToJobIndex_GetJobIdsByMajorCodes_IgnoresDuplicatesAndUnknown()
    {
        var categoryIndex = CreateCategoryIndex();

        var jobs = new List<JobPosting>
        {
            new JobPosting(1, "Job 1", "desc", new[] { 100101, 999999 }),
            new JobPosting(2, "Job 2", "desc", minorCodes: null),
            new JobPosting(3, "Job 3", "desc", new[] { 100206 }),
            new JobPosting(4, "Job 4", "desc", new[] { 999999 }),
        };

        var index = new MajorToJobIndex(categoryIndex, jobs);
        var jobIds = index.GetJobIdsByMajorCodes(new[] { 100000, 100000, 999999 });

        AssertSetEquals(new[] { 1, 3 }, jobIds);
    }
}
