namespace SearchJob.JobCategories;

/// <summary>
/// 職務大類。
/// </summary>
/// <param name="Code">大類代碼。</param>
/// <param name="Name">大類名稱。</param>
public sealed record MajorCategory(long Code, string Name);

/// <summary>
/// 職務中類。
/// </summary>
/// <param name="Code">中類代碼。</param>
/// <param name="Name">中類名稱。</param>
/// <param name="MajorCode">所屬大類代碼。</param>
public sealed record MiddleCategory(long Code, string Name, long MajorCode);

/// <summary>
/// 職務小類。
/// </summary>
/// <param name="Code">小類代碼。</param>
/// <param name="Name">小類名稱。</param>
/// <param name="MiddleCode">所屬中類代碼。</param>
/// <param name="MajorCode">所屬大類代碼。</param>
public sealed record SmallCategory(long Code, string Name, long MiddleCode, long MajorCode);
