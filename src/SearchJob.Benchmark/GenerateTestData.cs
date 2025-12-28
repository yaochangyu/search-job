using SearchJob.Benchmark;

Console.WriteLine("開始產生測試資料...");

// 產生職務類別資料
var categories = TestDataGenerator.GenerateCategories(targetMinorCount: 10000);
Console.WriteLine($"已產生 {categories.Count} 筆職務類別資料");

var minorCodes = categories
    .Where(c => c.Level == SearchJob.Models.JobCategoryLevel.Minor)
    .Select(c => c.Code)
    .ToList();

// 產生職缺資料
var vacancies = TestDataGenerator.GenerateVacancies(count: 10000, availableMinorCodes: minorCodes);
Console.WriteLine($"已產生 {vacancies.Count} 筆職缺資料");

// 儲存測試資料
var testDataDir = "test-data";
var categoriesFile = Path.Combine(testDataDir, "test-categories-10k.json");
var vacanciesFile = Path.Combine(testDataDir, "test-vacancies-10k.json");

await TestDataPersistence.SaveCategoriesAsync(categoriesFile, categories);
Console.WriteLine($"職務類別資料已儲存至: {categoriesFile}");

await TestDataPersistence.SaveVacanciesAsync(vacanciesFile, vacancies);
Console.WriteLine($"職缺資料已儲存至: {vacanciesFile}");

Console.WriteLine("測試資料產生完成！");
