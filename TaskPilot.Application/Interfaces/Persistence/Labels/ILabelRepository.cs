using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Interfaces.Persistence.Labels;

public interface ILabelRepository : IGenericRepository<Label>
{
    Task<List<Label>> GetLabelsByProjectIdAsync(
        int projectId,
        CancellationToken cancellationToken);

    Task<bool> ExistsByNameInProjectAsync(
        int projectId,
        string name,
        CancellationToken cancellationToken);
}
