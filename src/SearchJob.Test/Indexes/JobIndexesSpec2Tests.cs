using SearchJob.Indexes;
using SearchJob.Models;

namespace SearchJob.Test.Indexes;

public sealed class JobIndexesSpec2Tests
{
    private static JobCategoryHierarchyIndex CreateCategoryIndex()
    {
        var categories = new List<JobCategory>
        {
            // Major
            new JobCategory(100000, "管理幕僚／人資／行政", parentCode: null, JobCategoryLevel.Major),

            // Middle
            new JobCategory(100100, "管理幕僚", parentCode: 100000, JobCategoryLevel.Middle),
            new JobCategory(100200, "人力資源", parentCode: 100000, JobCategoryLevel.Middle),

            // Minor
            new JobCategory(100101, "經營管理主管", parentCode: 100100, JobCategoryLevel.Minor),
            new JobCategory(100105, "特別助理", parentCode: 100100, JobCategoryLevel.Minor),
            new JobCategory(100205, "人事助理", parentCode: 100200, JobCategoryLevel.Minor),
            new JobCategory(100206, "就業服務員", parentCode: 100200, JobCategoryLevel.Minor),
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
    public void Spec2_4_2_1_GetMajorCodesByMinorCodes()
    {
        var index = CreateCategoryIndex();

        var result = index.GetMajorCodesByMinorCodes(new[] { 100101, 100105, 100101, 999999 });

        AssertSetEquals(new[] { 100000 }, result);
    }

    [Fact]
    public void Spec2_4_2_2_GetMiddleCodesByMinorCodes()
    {
        var index = CreateCategoryIndex();

        var result = index.GetMiddleCodesByMinorCodes(new[] { 100101, 100205, 100205, 999999 });

        AssertSetEquals(new[] { 100100, 100200 }, result);
    }

    [Fact]
    public void Spec2_4_2_3_GetMinorCodesByMiddleCodes()
    {
        var index = CreateCategoryIndex();

        var result = index.GetMinorCodesByMiddleCodes(new[] { 100100, 100100, 999999 });

        AssertSetEquals(new[] { 100101, 100105 }, result);
    }

    [Fact]
    public void Spec2_4_2_4_GetMajorCodesByMiddleCodes()
    {
        var index = CreateCategoryIndex();

        var result = index.GetMajorCodesByMiddleCodes(new[] { 100100, 100200, 100200, 999999 });

        AssertSetEquals(new[] { 100000 }, result);
    }

    [Fact]
    public void Spec2_4_2_5_GetMinorCodesByMajorCodes()
    {
        var index = CreateCategoryIndex();

        var result = index.GetMinorCodesByMajorCodes(new[] { 100000, 100000, 999999 });

        AssertSetEquals(new[] { 100101, 100105, 100205, 100206 }, result);
    }

    [Fact]
    public void Spec2_4_2_6_GetMiddleCodesByMajorCodes()
    {
        var index = CreateCategoryIndex();

        var result = index.GetMiddleCodesByMajorCodes(new[] { 100000, 100000, 999999 });

        AssertSetEquals(new[] { 100100, 100200 }, result);
    }

    [Fact]
    public void Spec2_5_2_JobId_To_Minor_Middle_Major_Codes()
    {
        var categoryIndex = CreateCategoryIndex();

        var jobs = new List<JobPosting>
        {
            new JobPosting(1, "Job 1", "desc", new[] { 100101, 100101, 100205 }),
            new JobPosting(2, "Job 2", "desc", minorCodes: null),
            new JobPosting(3, "Job 3", "desc", new[] { 100206 }),
        };

        var jobIndex = new JobToJobCategoryIndex(categoryIndex, jobs);

        var minorCodes = jobIndex.GetMinorCodesByJobIds(new[] { 1, 3, 3, 999 });
        AssertSetEquals(new[] { 100101, 100205, 100206 }, minorCodes);

        var middleCodes = jobIndex.GetMiddleCodesByJobIds(new[] { 1, 3, 999 });
        AssertSetEquals(new[] { 100100, 100200 }, middleCodes);

        var majorCodes = jobIndex.GetMajorCodesByJobIds(new[] { 1, 3, 999 });
        AssertSetEquals(new[] { 100000 }, majorCodes);
    }
}
