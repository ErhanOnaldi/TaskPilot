using Microsoft.EntityFrameworkCore;
using TaskPilot.Application.Interfaces.Persistence.Labels;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Persistence.EntityRepositories;

public sealed class LabelRepository : GenericRepository<Label>, ILabelRepository
{
    private readonly AppDbContext _dbContext;

    public LabelRepository(AppDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<Label>> GetLabelsByProjectIdAsync(int projectId, CancellationToken cancellationToken)
    {
        return _dbContext.Labels
            .AsNoTracking()
            .Where(label => label.ProjectId == projectId)
            .OrderBy(label => label.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByNameInProjectAsync(int projectId, string name, CancellationToken cancellationToken)
    {
        return _dbContext.Labels
            .AsNoTracking()
            .AnyAsync(label => label.ProjectId == projectId && label.Name == name, cancellationToken);
    }
}
