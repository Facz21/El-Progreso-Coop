using ElProgreso.Coop.Domain.Enums;

namespace ElProgreso.Coop.Application.DTOs;

public enum BalanceFilter
{
    All,
    PositiveBalance,
    ZeroBalance,
    ActiveWithTransactions,
    InactiveDormant
}

public enum AssociateSortField
{
    NameAsc,
    NameDesc,
    BalanceDesc,
    BalanceAsc,
    RegistrationDateDesc,
    RegistrationDateAsc,
    DocumentAsc,
    DocumentDesc
}

public record AssociateFilterCriteria(
    DocumentType? DocumentType = null,
    BalanceFilter BalanceFilter = BalanceFilter.All,
    AssociateSortField SortBy = AssociateSortField.NameAsc
);
