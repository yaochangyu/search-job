using System.Text.Json;
using SearchJob.Models;

namespace SearchJob.Benchmark;

/// <summary>
/// 測試資料持久化管理器，負責儲存與載入測試資料。
/// </summary>
public static class TestDataPersistence
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// 儲存職務類別資料到 JSON 檔案。
    /// </summary>
    public static async Task SaveCategoriesAsync(string filePath, IEnumerable<JobCategory> categories)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // 將 JobCategory 轉換為可序列化的 DTO
        var dtos = categories.Select(c => new JobCategoryDto
        {
            Code = c.Code,
            Name = c.Name,
            ParentCode = c.ParentCode,
            Level = c.Level.ToString()
        }).ToList();

        await using var fileStream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(fileStream, dtos, JsonOptions);
    }

    /// <summary>
    /// 從 JSON 檔案載入職務類別資料。
    /// </summary>
    public static async Task<List<JobCategory>> LoadCategoriesAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"找不到檔案: {filePath}");
        }

        await using var fileStream = File.OpenRead(filePath);
        var dtos = await JsonSerializer.DeserializeAsync<List<JobCategoryDto>>(fileStream, JsonOptions);

        if (dtos == null)
        {
            throw new InvalidOperationException("無法反序列化職務類別資料");
        }

        return dtos.Select(dto => new JobCategory(
            Code: dto.Code,
            Name: dto.Name,
            ParentCode: dto.ParentCode,
            Level: Enum.Parse<JobCategoryLevel>(dto.Level)
        )).ToList();
    }

    /// <summary>
    /// 儲存職缺資料到 JSON 檔案。
    /// </summary>
    public static async Task SaveVacanciesAsync(string filePath, IEnumerable<JobVacancy> vacancies)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // 將 JobVacancy 轉換為可序列化的 DTO
        var dtos = vacancies.Select(v => new JobVacancyDto
        {
            JobId = v.JobId,
            Title = v.Title,
            Description = v.Description,
            MinorCodes = v.MinorCodes.ToList()
        }).ToList();

        await using var fileStream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(fileStream, dtos, JsonOptions);
    }

    /// <summary>
    /// 從 JSON 檔案載入職缺資料。
    /// </summary>
    public static async Task<List<JobVacancy>> LoadVacanciesAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"找不到檔案: {filePath}");
        }

        await using var fileStream = File.OpenRead(filePath);
        var dtos = await JsonSerializer.DeserializeAsync<List<JobVacancyDto>>(fileStream, JsonOptions);

        if (dtos == null)
        {
            throw new InvalidOperationException("無法反序列化職缺資料");
        }

        return dtos.Select(dto => new JobVacancy(
            jobId: dto.JobId,
            title: dto.Title,
            description: dto.Description,
            minorCodes: dto.MinorCodes
        )).ToList();
    }

    // DTO 類別用於 JSON 序列化
    private sealed class JobCategoryDto
    {
        public int Code { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? ParentCode { get; set; }
        public string Level { get; set; } = string.Empty;
    }

    private sealed class JobVacancyDto
    {
        public int JobId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<int> MinorCodes { get; set; } = new();
    }
}
