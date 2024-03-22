using Models;

namespace SecuritiesMaintain.Services;

internal interface IIndexToDbService
{
    Task<bool> DeleteAgedRecords();

    Task<bool> UpdateIndexList(List<IndexComponent>? indexComponents);
}