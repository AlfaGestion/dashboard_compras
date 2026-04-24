using DashboardCompras.Models;

namespace DashboardCompras.Repositories;

public interface IAuxErrRepository
{
    Task<int> InsertAsync(AuxErrEntry entry, CancellationToken ct = default);
}
