using System.Text.Json;
using SearchJob.Models;

namespace SearchJob.Loaders;

public static class JobCategoryJsonLoader
{
    private sealed class JobCategoryNode
    {
        public int Code { get; set; }
        public string? Name { get; set; }
        public int? ParentCode { get; set; }
        public int Level { get; set; }

        // 資料檔可能同時提供巢狀 children 與扁平清單；目前轉換只需要上面欄位。
        public List<JobCategoryNode>? Children { get; set; }
    }

    /// <summary>
    /// 讀取 jobCategory.json，轉成 <see cref="JobCategory"/> 清單。
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

        var nodes = JsonSerializer.Deserialize<List<JobCategoryNode>>(stream, options)
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

    private static List<JobCategoryNode> FlattenNodes(IEnumerable<JobCategoryNode> rootNodes)
    {
        var result = new List<JobCategoryNode>();
        var stack = new Stack<JobCategoryNode>();
        var seenByCode = new Dictionary<int, JobCategoryNode>();

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

                // 資料可能同時提供扁平清單與巢狀 children，或 children 形成循環；以 code 去重可避免重複與無限遍歷。
                if (seenByCode.TryGetValue(current.Code, out var existing))
                {
                    // 若同 code 的資料內容不一致，代表來源資料自相矛盾：應 fail fast。
                    // Children 不納入一致性判斷（同一節點可出現在 tree 與 flat list）。
                    if (!string.Equals(existing.Name, current.Name, StringComparison.Ordinal)
                        || existing.ParentCode != current.ParentCode
                        || existing.Level != current.Level)
                    {
                        throw new JsonException(
                            $"Duplicate code with inconsistent data. code={current.Code}, " +
                            $"existing=(name='{existing.Name}', parentCode={existing.ParentCode}, level={existing.Level}), " +
                            $"current=(name='{current.Name}', parentCode={current.ParentCode}, level={current.Level}).");
                    }

                    continue;
                }

                seenByCode.Add(current.Code, current);

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
