using BenchmarkDotNet.Running;
using SearchJob.Benchmark;

// 執行效能測試
BenchmarkRunner.Run<JobCategoryLookupBenchmark>();
