using System.Text.Json;
using SearchJob.Models;

namespace SearchJob;

public static class JobPositionJsonLoader
{
    private sealed class JobPositionNode
    {
        public int Code { get; set; }
        public string? Name { get; set; }
        public int? ParentCode { get; set; }
        public int Level { get; set; }

        // 資料檔可能同時提供巢狀 children 與扁平清單；目前轉換只需要上面欄位。
        public List<JobPositionNode>? Children { get; set; }
    }

    /// <summary>
    /// 讀取 jobPosition.json，轉成 <see cref="JobCategory"/> 清單。
    /// </summary>
    /// <remarks>
    /// 目前假設 JSON 根節點是陣列，且每個節點至少包含：code、name、parentCode、level。
    /// level=1/2/3 對應 <see cref="JobCategoryLevel"/> 的 Major/Middle/Minor。
    /// </remarks>
    public static IReadOnlyList<JobCategory> LoadJobCategories(string jsonFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jsonFilePath);

        using var stream = File.OpenRead(jsonFilePath);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };

        var nodes = JsonSerializer.Deserialize<List<JobPositionNode>>(stream, options)
            ?? throw new JsonException($"Failed to deserialize JSON array. Path={jsonFilePath}");

        var result = new List<JobCategory>(nodes.Count);

        foreach (var node in nodes)
        {
            if (node is null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(node.Name))
            {
                throw new JsonException($"Missing or empty name for code={node.Code}.");
            }

            if (!Enum.IsDefined(typeof(JobCategoryLevel), node.Level))
            {
                throw new JsonException($"Invalid level={node.Level} for code={node.Code}.");
            }

            var level = (JobCategoryLevel)node.Level;
            result.Add(new JobCategory(node.Code, node.Name, node.ParentCode, level));
        }

        return result;
    }
}
