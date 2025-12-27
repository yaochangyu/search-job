using SearchJob.Indexes;
using SearchJob.Models;

namespace SearchJob.Test.Indexes;

public sealed class JobCategoryHierarchyIndexBuildWorkflowTests
{
    [Fact]
    public void Constructor_WhenInputOrderIsReversed_BuildsHierarchyInMajorMiddleMinorOrder()
    {
        // Protects: 建置不依賴輸入順序；即使 categories 亂序仍能成功建立階層索引。
        // Arrange: 刻意把輸入順序反過來（Minors -> Middles -> Major）
        // 若建置是「單一 foreach 依輸入順序寫入」，會先遇到 Minor 而因找不到 Middle 而失敗。
        // 依 spec-2 4.0 #2，建置應分層級寫入（Major -> Middle -> Minor），因此仍可成功建置。
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

        // Act
        var index = new JobCategoryHierarchyIndex(categories);

        // Assert: 能正常查出關聯，代表建置成功。
        Assert.Equal(new HashSet<int> { 100000 }, index.GetMajorCodesByMinorCodes(new[] { 100101, 100205 }));
        Assert.Equal(new HashSet<int> { 100100, 100200 }, index.GetMiddleCodesByMajorCodes(new[] { 100000 }));
        Assert.Equal(new HashSet<int> { 100101, 100105 }, index.GetMinorCodesByMiddleCodes(new[] { 100100 }));
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
}
