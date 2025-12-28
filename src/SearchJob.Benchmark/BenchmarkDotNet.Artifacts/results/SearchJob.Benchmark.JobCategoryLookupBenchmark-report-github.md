```

BenchmarkDotNet v0.15.8, Windows 10 (10.0.19045.6456/22H2/2022Update)
Intel Core i5-8250U CPU 1.60GHz (Max: 1.80GHz) (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.101
  [Host]     : .NET 10.0.1 (10.0.1, 10.0.125.57005), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.1 (10.0.1, 10.0.125.57005), X64 RyuJIT x86-64-v3


```
| Method                      | Mean        | Error     | StdDev    | Median      | Min         | Max        | Gen0   | Allocated |
|---------------------------- |------------:|----------:|----------:|------------:|------------:|-----------:|-------:|----------:|
| GetMajorCodes_SingleMinor   |    86.11 ns |  3.663 ns |  10.33 ns |    82.50 ns |    69.18 ns |   114.8 ns | 0.0535 |     168 B |
| GetMajorCodes_TenMinors     |   445.17 ns |  9.028 ns |  20.19 ns |   443.03 ns |   395.13 ns |   494.8 ns | 0.1068 |     336 B |
| GetMajorCodes_HundredMinors | 3,306.36 ns | 86.419 ns | 249.34 ns | 3,263.73 ns | 2,738.20 ns | 3,875.1 ns | 0.2060 |     664 B |
