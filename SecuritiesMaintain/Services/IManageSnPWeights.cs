using Models;

namespace SecuritiesMaintain.Services;

internal interface IManageIndexWeights
{
    Task<bool> UpdateIndexWeight(List<IndexComponent>? indices);
}