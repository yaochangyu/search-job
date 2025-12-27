using System.Text.Json;
using SearchJob.Models;

namespace SearchJob;

public static class JobPostingJsonLoader
{
    private sealed class JobPostingNode
    {
        public int? JobId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public List<int>? MinorCodes { get; set; }
    }

    /// <summary>
    /// 讀取 job.json，轉成 <see cref="JobPosting"/> 清單。
    /// </summary>
    /// <remarks>
    /// Spec v2（spec-2.md 第 3 章）需求：
    /// - jobId：整數（必填）
    /// - title：字串（必填）
    /// - description：字串（可為 null）
    /// - minorCodes：0..N；同一職缺不可重複（由 <see cref="JobPosting"/> 內部去重）
    /// </remarks>
    public static IReadOnlyList<JobPosting> LoadJobPostings(string jsonFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jsonFilePath);

        using var stream = File.OpenRead(jsonFilePath);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };

        var nodes = JsonSerializer.Deserialize<List<JobPostingNode>>(stream, options)
            ?? throw new JsonException($"Failed to deserialize JSON array. Path={jsonFilePath}");

        var result = new List<JobPosting>(nodes.Count);

        foreach (var node in nodes)
        {
            if (node is null)
            {
                continue;
            }

            if (node.JobId is null)
            {
                throw new JsonException("Missing jobId.");
            }

            if (string.IsNullOrWhiteSpace(node.Title))
            {
                throw new JsonException($"Missing or empty title for jobId={node.JobId}.");
            }

            IEnumerable<int> minorCodes = (IEnumerable<int>?)node.MinorCodes ?? Array.Empty<int>();
            result.Add(new JobPosting(node.JobId.Value, node.Title, node.Description ?? string.Empty, minorCodes));
        }

        return result;
    }
}
