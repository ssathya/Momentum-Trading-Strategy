using Models;

namespace SecuritiesMaintain.Services;
internal interface IBuildDowLst
{
    Task<List<IndexComponent>?> GetListAsync();
}
