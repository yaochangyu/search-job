using SearchJob.Indexes;
using SearchJob.Models;

namespace SearchJob.Test.Indexes;

public sealed class JobVacancyIndexTests
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
    public void GetCodesByJobIds_WhenJobsContainDuplicatesNullAndUnknown_ReturnsExpectedSets()
    {
        // Protects: JobVacancyIndex 會把多個 jobId 的 minorCodes union 起來、忽略重複與未知 jobId，
        // 並透過 JobCategoryHierarchyIndex 從 minor 正確推導 middle/major。

        var categoryIndex = CreateCategoryIndex();

        var jobs = new List<JobVacancy>
        {
            new JobVacancy(1, "Job 1", "desc", new[] { 100101, 100101, 100205 }),
            new JobVacancy(2, "Job 2", "desc", minorCodes: null),
            new JobVacancy(3, "Job 3", "desc", new[] { 100206 }),
        };

        var jobIndex = new JobVacancyIndex(categoryIndex, jobs);

        var minorCodes = jobIndex.GetMinorCodesByJobIds(new[] { 1, 3, 3, 999 });
        AssertSetEquals(new[] { 100101, 100205, 100206 }, minorCodes);

        var middleCodes = jobIndex.GetMiddleCodesByJobIds(new[] { 1, 3, 999 });
        AssertSetEquals(new[] { 100100, 100200 }, middleCodes);

        var majorCodes = jobIndex.GetMajorCodesByJobIds(new[] { 1, 3, 999 });
        AssertSetEquals(new[] { 100000 }, majorCodes);
    }

    [Fact]
    public void GetJobIdsByMajorCodes_WhenInputHasDuplicatesOrUnknown_IgnoresDuplicatesAndUnknown()
    {
        // Protects: JobVacancyIndex 會忽略重複/未知 major code，且只回傳隸屬該 major(含後代 minor) 的 jobIds。

        var categoryIndex = CreateCategoryIndex();

        var jobs = new List<JobVacancy>
        {
            new JobVacancy(1, "Job 1", "desc", new[] { 100101, 999999 }),
            new JobVacancy(2, "Job 2", "desc", minorCodes: null),
            new JobVacancy(3, "Job 3", "desc", new[] { 100206 }),
            new JobVacancy(4, "Job 4", "desc", new[] { 999999 }),
        };

        var index = new JobVacancyIndex(categoryIndex, jobs);
        var jobIds = index.GetJobIdsByMajorCodes(new[] { 100000, 100000, 999999 });

        AssertSetEquals(new[] { 1, 3 }, jobIds);
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
        // Protects: JobVacancyIndex 的 majorCodes 來自「job minors -> hierarchy 推導」，且忽略未知 minor。
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

    [Fact]
    public void LoadRealJobVacanciesAndFindMajorCategoriesByMinorCodes_ShouldReturnExpectedMajorCategories()
    {
        // Arrange: 載入真實的職務類別和職缺資料
        var categoryJsonPath = Path.Combine(AppContext.BaseDirectory, "TestData", "jobCategory.json");
        var jobJsonPath = Path.Combine(AppContext.BaseDirectory, "TestData", "jobVacancy.json");
        
        Assert.True(File.Exists(categoryJsonPath), $"Missing test data: {categoryJsonPath}");
        Assert.True(File.Exists(jobJsonPath), $"Missing test data: {jobJsonPath}");

        var categories = Loaders.JobCategoryJsonLoader.LoadJobCategories(categoryJsonPath);
        var jobs = Loaders.JobVacancyJsonLoader.LoadJobVacancies(jobJsonPath);
        
        var categoryIndex = new JobCategoryHierarchyIndex(categories);
        var jobIndex = new JobVacancyIndex(categoryIndex, jobs);

        // Act: 取得職缺 1 (新聞記者) 的職務小類代碼 [220106, 220105]，然後找出對應的職務大類
        var job1 = jobs.First(j => j.JobId == 1);
        var jobIds = new[] { job1.JobId };
        
        var minorCodes = jobIndex.GetMinorCodesByJobIds(jobIds);
        var majorCodes = jobIndex.GetMajorCodesByJobIds(jobIds);

        // Assert: 確認有找到職務小類和大類
        Assert.NotEmpty(minorCodes);
        Assert.Contains(220106, minorCodes); // 文字記者
        Assert.Contains(220105, minorCodes); // 攝影記者
        
        Assert.NotEmpty(majorCodes);
        Assert.Contains(220000, majorCodes); // 影視傳媒／出版翻譯
        
        // 驗證：透過小類能正確推導出大類（使用 categoryIndex 驗證）
        var expectedMajorCodes = categoryIndex.GetMajorCodesByMinorCodes(minorCodes);
        Assert.Equal(expectedMajorCodes, majorCodes);
    }
}
