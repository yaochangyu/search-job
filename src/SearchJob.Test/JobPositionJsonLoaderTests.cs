using SearchJob.Indexes;

namespace SearchJob.Test;

public sealed class JobPositionJsonLoaderTests
{
    [Fact]
    public void LoadJobCategories_Reads_Real_JobPosition_Json_File()
    {
        // Arrange
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "TestData", "jobPosition.json");
        Assert.True(File.Exists(jsonPath), $"Missing test data file: {jsonPath}");

        // Act
        var categories = JobPositionJsonLoader.LoadJobCategories(jsonPath);

        // Assert (basic sanity)
        Assert.NotNull(categories);
        Assert.NotEmpty(categories);

        Assert.Contains(categories, c => c.Level == Models.JobCategoryLevel.Major);
        Assert.Contains(categories, c => c.Level == Models.JobCategoryLevel.Middle);
        Assert.Contains(categories, c => c.Level == Models.JobCategoryLevel.Minor);

        // Assert (stable known node from the real dataset)
        var node = categories.SingleOrDefault(c => c.Code == 220000);
        Assert.NotNull(node);
        Assert.Equal("影視傳媒／出版翻譯", node!.Name);
        Assert.Equal(Models.JobCategoryLevel.Major, node.Level);
        Assert.Null(node.ParentCode);

        // Optional: ensure the loaded data can build the hierarchy index (may throw if source data violates spec constraints)
        _ = new JobCategoryHierarchyIndex(categories);
    }
}
