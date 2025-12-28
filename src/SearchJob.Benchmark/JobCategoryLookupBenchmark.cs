using BenchmarkDotNet.Attributes;
using SearchJob.Indexes;
using SearchJob.Models;

namespace SearchJob.Benchmark;

/// <summary>
/// 職務類別查詢效能測試。
/// 測試目標：驗證「職務小類找職務大類」的效能。
/// </summary>
[MemoryDiagnoser]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class JobCategoryLookupBenchmark
{
    private JobCategoryHierarchyIndex _categoryIndex = null!;
    private List<int> _allMinorCodes = null!;
    private int[] _singleMinorCode = null!;
    private int[] _tenMinorCodes = null!;
    private int[] _hundredMinorCodes = null!;

    private const string TestDataDir = "test-data";
    private const string CategoriesFile = "test-categories-10k.json";
    private const string VacanciesFile = "test-vacancies-10k.json";

    /// <summary>
    /// 全域設定：在所有測試執行前載入測試資料。
    /// </summary>
    [GlobalSetup]
    public async Task GlobalSetup()
    {
        Console.WriteLine("開始載入測試資料...");

        var categoriesPath = Path.Combine(TestDataDir, CategoriesFile);
        var vacanciesPath = Path.Combine(TestDataDir, VacanciesFile);

        List<JobCategory> categories;
        List<JobVacancy> vacancies;

        // 檢查測試資料是否存在
        if (File.Exists(categoriesPath) && File.Exists(vacanciesPath))
        {
            Console.WriteLine("從檔案載入測試資料...");
            categories = await TestDataPersistence.LoadCategoriesAsync(categoriesPath);
            vacancies = await TestDataPersistence.LoadVacanciesAsync(vacanciesPath);
        }
        else
        {
            Console.WriteLine("產生新的測試資料...");
            
            // 產生 10000 筆小類
            categories = TestDataGenerator.GenerateCategories(targetMinorCount: 10000);
            
            var minorCodes = categories
                .Where(c => c.Level == JobCategoryLevel.Minor)
                .Select(c => c.Code)
                .ToList();

            // 產生 10000 筆職缺
            vacancies = TestDataGenerator.GenerateVacancies(count: 10000, availableMinorCodes: minorCodes);

            // 儲存測試資料供下次使用
            Console.WriteLine("儲存測試資料...");
            await TestDataPersistence.SaveCategoriesAsync(categoriesPath, categories);
            await TestDataPersistence.SaveVacanciesAsync(vacanciesPath, vacancies);
        }

        Console.WriteLine($"已載入 {categories.Count} 筆職務類別資料");
        Console.WriteLine($"已載入 {vacancies.Count} 筆職缺資料");

        // 建立職務類別索引
        Console.WriteLine("建立職務類別索引...");
        _categoryIndex = new JobCategoryHierarchyIndex(categories);

        // 準備測試用的小類代碼
        _allMinorCodes = categories
            .Where(c => c.Level == JobCategoryLevel.Minor)
            .Select(c => c.Code)
            .ToList();

        var random = new Random(42);
        
        // 準備不同數量的測試資料
        _singleMinorCode = new[] { _allMinorCodes[random.Next(_allMinorCodes.Count)] };
        
        _tenMinorCodes = Enumerable.Range(0, 10)
            .Select(_ => _allMinorCodes[random.Next(_allMinorCodes.Count)])
            .ToArray();
        
        _hundredMinorCodes = Enumerable.Range(0, 100)
            .Select(_ => _allMinorCodes[random.Next(_allMinorCodes.Count)])
            .ToArray();

        Console.WriteLine("測試資料準備完成！");
    }

    /// <summary>
    /// 測試項目 #1：使用單一職務小類查詢對應的職務大類。
    /// </summary>
    [Benchmark]
    public IReadOnlySet<int> GetMajorCodes_SingleMinor()
    {
        return _categoryIndex.GetMajorCodesByMinorCodes(_singleMinorCode);
    }

    /// <summary>
    /// 測試項目 #2：使用 10 個職務小類查詢對應的職務大類。
    /// </summary>
    [Benchmark]
    public IReadOnlySet<int> GetMajorCodes_TenMinors()
    {
        return _categoryIndex.GetMajorCodesByMinorCodes(_tenMinorCodes);
    }

    /// <summary>
    /// 測試項目 #3：使用 100 個職務小類查詢對應的職務大類。
    /// </summary>
    [Benchmark]
    public IReadOnlySet<int> GetMajorCodes_HundredMinors()
    {
        return _categoryIndex.GetMajorCodesByMinorCodes(_hundredMinorCodes);
    }
}
