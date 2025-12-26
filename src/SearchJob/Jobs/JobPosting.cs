namespace SearchJob.Jobs;

/// <summary>
/// 職缺。
/// </summary>
/// <param name="JobId">職缺編號（長整數）。</param>
/// <param name="Title">工作標題。</param>
/// <param name="Description">工作說明。</param>
/// <param name="SmallCategoryCodes">職務小類（多筆，小類代碼集合）。</param>
public sealed record JobPosting(
	long JobId,
	string Title,
	string Description,
	IReadOnlyCollection<long> SmallCategoryCodes);
