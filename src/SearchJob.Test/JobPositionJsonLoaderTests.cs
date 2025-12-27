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

    [Fact]
    public void LoadJobCategories_WhenJsonHasNestedChildren_FlattensNodes()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jobPosition-nested-{Guid.NewGuid():N}.json");

        File.WriteAllText(tempFile,
            "[" +
            "  { \"code\": 100000, \"name\": \"管理幕僚／人資／行政\", \"parentCode\": null, \"level\": 1, \"children\": [" +
            "      { \"code\": 100100, \"name\": \"管理幕僚\", \"parentCode\": 100000, \"level\": 2, \"children\": [" +
            "          { \"code\": 100101, \"name\": \"經營管理主管\", \"parentCode\": 100100, \"level\": 3, \"children\": [] }" +
            "      ] }" +
            "  ] }" +
            "]");

        try
        {
            var categories = JobPositionJsonLoader.LoadJobCategories(tempFile);

            Assert.Contains(categories, c => c.Code == 100000 && c.Level == Models.JobCategoryLevel.Major);
            Assert.Contains(categories, c => c.Code == 100100 && c.Level == Models.JobCategoryLevel.Middle);
            Assert.Contains(categories, c => c.Code == 100101 && c.Level == Models.JobCategoryLevel.Minor);

            _ = new JobCategoryHierarchyIndex(categories);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
