namespace SearchJob.Models;

public sealed record JobCategory(
    int Code,
    string Name,
    int? ParentCode,
    JobCategoryLevel Level
);
