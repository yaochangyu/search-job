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
    /// 
    /// 若資料同時包含巢狀 children，本方法會展開 children 並回傳扁平清單。
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

        var allNodes = FlattenNodes(nodes);
        var result = new List<JobCategory>(allNodes.Count);

        foreach (var node in allNodes)
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

    private static List<JobPositionNode> FlattenNodes(IEnumerable<JobPositionNode> rootNodes)
    {
        var result = new List<JobPositionNode>();
        var stack = new Stack<JobPositionNode>();

        foreach (var root in rootNodes)
        {
            if (root is null)
            {
                continue;
            }

            stack.Push(root);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                result.Add(current);

                if (current.Children is null || current.Children.Count == 0)
                {
                    continue;
                }

                // Reverse push to preserve original order as much as possible.
                for (var index = current.Children.Count - 1; index >= 0; index--)
                {
                    var child = current.Children[index];
                    if (child is not null)
                    {
                        stack.Push(child);
                    }
                }
            }
        }

        return result;
    }
}
