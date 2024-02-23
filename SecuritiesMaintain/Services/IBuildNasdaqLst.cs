using Models;

namespace SecuritiesMaintain.Services;

internal interface IBuildNasdaqLst
{
    Task<List<IndexComponent>?> GetListAsync();
}
