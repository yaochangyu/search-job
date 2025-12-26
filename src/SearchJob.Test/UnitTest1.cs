using SearchJob.JobCategories;
using SearchJob.Jobs;

namespace SearchJob.Test;

public sealed class UnitTest1
{
    [Fact]
    public void JobCategoryIndex_spec_example_works()
    {
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

        var majors1 = index.GetMajorsBySmallCodes(new[] { 100205L, 100105L });
        Assert.Single(majors1);
        Assert.Contains(new MajorCategory(100000, "管理幕僚／人資／行政"), majors1);

        var middles1 = index.GetMiddlesBySmallCodes(new[] { 100205L, 100105L });
        Assert.Equal(2, middles1.Count);
        Assert.Contains(new MiddleCategory(100100, "管理幕僚", 100000), middles1);
        Assert.Contains(new MiddleCategory(100200, "人力資源", 100000), middles1);

        var smalls1 = index.GetSmallsByMajorCodes(new[] { 100000L });
        Assert.Equal(4, smalls1.Count);
        Assert.Contains(new SmallCategory(100101, "經營管理主管", 100100, 100000), smalls1);
        Assert.Contains(new SmallCategory(100105, "特別助理", 100100, 100000), smalls1);
        Assert.Contains(new SmallCategory(100205, "人事助理", 100200, 100000), smalls1);
        Assert.Contains(new SmallCategory(100206, "就業服務員", 100200, 100000), smalls1);

        var middles2 = index.GetMiddlesByMajorCodes(new[] { 100000L });
        Assert.Equal(2, middles2.Count);
        Assert.Contains(new MiddleCategory(100100, "管理幕僚", 100000), middles2);
        Assert.Contains(new MiddleCategory(100200, "人力資源", 100000), middles2);

        var smalls2 = index.GetSmallsByMiddleCodes(new[] { 100100L, 100200L });
        Assert.Equal(4, smalls2.Count);
        Assert.Contains(new SmallCategory(100101, "經營管理主管", 100100, 100000), smalls2);
        Assert.Contains(new SmallCategory(100105, "特別助理", 100100, 100000), smalls2);
        Assert.Contains(new SmallCategory(100205, "人事助理", 100200, 100000), smalls2);
        Assert.Contains(new SmallCategory(100206, "就業服務員", 100200, 100000), smalls2);

        var majors2 = index.GetMajorsByMiddleCodes(new[] { 100100L, 100200L });
        Assert.Single(majors2);
        Assert.Contains(new MajorCategory(100000, "管理幕僚／人資／行政"), majors2);
    }

    [Fact]
    public void JobCodeCategoryIndex_spec_example_works()
    {
        var smallByJob = new Dictionary<string, HashSet<long>>(StringComparer.OrdinalIgnoreCase)
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

        var smallCodes = jobCodeIndex.GetSmallCodes(new[] { "JOB-001", "JOB-002" });
        Assert.Equal(3, smallCodes.Count);
        Assert.Contains(100105L, smallCodes);
        Assert.Contains(100205L, smallCodes);
        Assert.Contains(100101L, smallCodes);

        var middleCodes = jobCodeIndex.GetMiddleCodes(new[] { "JOB-001", "JOB-002" });
        Assert.Equal(2, middleCodes.Count);
        Assert.Contains(100100L, middleCodes);
        Assert.Contains(100200L, middleCodes);

        var majorCodes = jobCodeIndex.GetMajorCodes(new[] { "JOB-001", "JOB-002" });
        Assert.Single(majorCodes);
        Assert.Contains(100000L, majorCodes);
    }

    [Fact]
    public void JobPosting_has_spec_fields()
    {
        var job = new JobPosting(
            JobId: 1L,
            Title: "軟體工程師",
            Description: "工作說明",
            SmallCategoryCodes: new[] { 100205L, 100105L });

        Assert.Equal(1L, job.JobId);
        Assert.Equal("軟體工程師", job.Title);
        Assert.Equal("工作說明", job.Description);
        Assert.Equal(2, job.SmallCategoryCodes.Count);
    }
}
