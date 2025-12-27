using System.Text.Json;
using SearchJob.Models;

namespace SearchJob.Test;

public sealed class JobPostingJsonLoaderTests
{
    [Fact]
    public void LoadJobPostings_Reads_TestData_Job_Json_File()
    {
        // Arrange
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "TestData", "job.json");
        Assert.True(File.Exists(jsonPath), $"Missing test data file: {jsonPath}");

        // Act
        var jobs = JobPostingJsonLoader.LoadJobPostings(jsonPath);

        // Assert
        Assert.NotNull(jobs);
        Assert.Equal(4, jobs.Count);

        var job1 = jobs.Single(j => j.JobId == 1);
        Assert.Equal("HR Assistant", job1.Title);
        Assert.Equal(new HashSet<int> { 100205, 100206 }, job1.MinorCodes);

        var job4 = jobs.Single(j => j.JobId == 4);
        Assert.Empty(job4.MinorCodes);
    }

    [Fact]
    public void LoadJobPostings_Throws_When_Title_Is_Missing()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"job-invalid-{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "[{ \"jobId\": 1, \"description\": \"x\", \"minorCodes\": [100101] }]");

        try
        {
            // Act + Assert
            var ex = Assert.Throws<JsonException>(() => JobPostingJsonLoader.LoadJobPostings(tempFile));
            Assert.Contains("title", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void LoadJobPostings_Uses_Empty_Set_When_MinorCodes_Is_Null()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"job-null-minors-{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "[{ \"jobId\": 1, \"title\": \"t\", \"description\": \"d\" }]");

        try
        {
            // Act
            var jobs = JobPostingJsonLoader.LoadJobPostings(tempFile);

            // Assert
            Assert.Single(jobs);
            Assert.Empty(jobs[0].MinorCodes);
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
