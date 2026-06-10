namespace Materia.WebUi.Services;

public record ChangeFundDepositDto(
    Guid     Id,
    decimal  Amount,
    string   Source,
    string?  SourceReferenceNo,
    string?  Notes,
    string   RecordedBy,
    DateTime RecordedAt);

public record ChangeFundResultDto(
    List<ChangeFundDepositDto> Items,
    int     TotalCount,
    int     Page,
    int     PageSize,
    int     TotalPages,
    decimal TotalBalance);
