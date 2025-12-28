using SearchJob.Models;

namespace SearchJob.Benchmark;

/// <summary>
/// 測試資料產生器，用於產生大量符合規格的職務類別與職缺資料。
/// </summary>
public static class TestDataGenerator
{
    /// <summary>
    /// 產生測試用的職務類別資料（大類、中類、小類）。
    /// </summary>
    /// <param name="targetMinorCount">目標產生的小類數量</param>
    /// <returns>所有職務類別的集合（包含大類、中類、小類）</returns>
    public static List<JobCategory> GenerateCategories(int targetMinorCount = 10000)
    {
        var categories = new List<JobCategory>();

        // 計算需要的大類與中類數量
        // 假設每個大類有 20 個中類，每個中類有 50 個小類
        var minorsPerMiddle = 50;
        var middlesPerMajor = 20;
        
        var requiredMiddles = (int)Math.Ceiling((double)targetMinorCount / minorsPerMiddle);
        var requiredMajors = (int)Math.Ceiling((double)requiredMiddles / middlesPerMajor);

        var minorCounter = 0;

        // 產生大類、中類、小類
        for (int majorIdx = 0; majorIdx < requiredMajors && minorCounter < targetMinorCount; majorIdx++)
        {
            // 大類代碼：從 1000000 開始，每個大類間隔 100000
            var majorCode = 1000000 + (majorIdx * 100000);
            categories.Add(new JobCategory(
                Code: majorCode,
                Name: $"測試大類_{majorCode}",
                ParentCode: null,
                Level: JobCategoryLevel.Major
            ));

            // 為每個大類產生中類
            for (int middleIdx = 0; middleIdx < middlesPerMajor && minorCounter < targetMinorCount; middleIdx++)
            {
                // 中類代碼：大類代碼 + (中類索引 + 1) * 1000
                var middleCode = majorCode + ((middleIdx + 1) * 1000);
                categories.Add(new JobCategory(
                    Code: middleCode,
                    Name: $"測試中類_{middleCode}",
                    ParentCode: majorCode,
                    Level: JobCategoryLevel.Middle
                ));

                // 為每個中類產生小類
                var minorsToGenerate = Math.Min(minorsPerMiddle, targetMinorCount - minorCounter);
                for (int minorIdx = 0; minorIdx < minorsToGenerate; minorIdx++)
                {
                    // 小類代碼：中類代碼 + (小類索引 + 1)
                    var minorCode = middleCode + minorIdx + 1;
                    categories.Add(new JobCategory(
                        Code: minorCode,
                        Name: $"測試小類_{minorCode}",
                        ParentCode: middleCode,
                        Level: JobCategoryLevel.Minor
                    ));

                    minorCounter++;
                }
            }
        }

        return categories;
    }

    /// <summary>
    /// 產生測試用的職缺資料。
    /// </summary>
    /// <param name="count">職缺數量</param>
    /// <param name="availableMinorCodes">可用的小類代碼集合</param>
    /// <returns>職缺集合</returns>
    public static List<JobVacancy> GenerateVacancies(int count, IReadOnlyCollection<int> availableMinorCodes)
    {
        var vacancies = new List<JobVacancy>(count);
        var random = new Random(42); // 固定種子確保可重現
        var minorCodesArray = availableMinorCodes.ToArray();

        for (int i = 1; i <= count; i++)
        {
            // 每個職缺隨機包含 1-5 個職務小類
            var minorCodeCount = random.Next(1, 6);
            var selectedMinorCodes = new HashSet<int>();

            for (int j = 0; j < minorCodeCount; j++)
            {
                var randomIndex = random.Next(minorCodesArray.Length);
                selectedMinorCodes.Add(minorCodesArray[randomIndex]);
            }

            var vacancy = new JobVacancy(
                jobId: i,
                title: $"測試職缺_{i}",
                description: $"這是測試職缺 {i} 的描述",
                minorCodes: selectedMinorCodes
            );

            vacancies.Add(vacancy);
        }

        return vacancies;
    }
}
