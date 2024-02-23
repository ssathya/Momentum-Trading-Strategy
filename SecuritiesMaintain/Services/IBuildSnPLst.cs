using Models;

namespace SecuritiesMaintain.Services;

internal interface IBuildSnPLst
{
    Task<List<IndexComponent>?> GetListAsync();
}
