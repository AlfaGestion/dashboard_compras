using AlfaCore.Models;

namespace AlfaCore.Services;

public interface IEntitySaveValidator<T>
{
    Task<ValidationResult> ValidateForSaveAsync(T request, CancellationToken ct = default);
}
