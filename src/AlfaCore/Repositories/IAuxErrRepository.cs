using AlfaCore.Models;

namespace AlfaCore.Repositories;

public interface IAuxErrRepository
{
    Task<int> InsertAsync(AuxErrEntry entry, CancellationToken ct = default);
}
