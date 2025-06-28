using DavideBotHelper.Database.Context;
using DavideBotHelper.Services.Extensions;

namespace DavideBotHelper.Services.Tasks;

public abstract class TransactionalTask : BaseTask
{
    private readonly ILogger _log;
    private readonly DavideBotDbContext? _dbContext;

    protected TransactionalTask(ILogger log, DavideBotDbContext context) : base(log)
    {
        _log = log;
        _dbContext = context;
    }

    public sealed override async Task Invoke()
    {
        
        if (_dbContext is null)
        {
            await Run();
            return;
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(_ct);
        try
        {
            await Run();
            await transaction.CommitAsync(CancellationToken.None);
        }
        catch (OperationCanceledException oce)
        {
            _log.Warning(oce, "Task cancelled");
            await transaction.RollbackAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Errore nell'esecuzione del task {taskName}", GetType().Name);
            await transaction.RollbackAsync(CancellationToken.None);
        }

    }

    protected abstract override Task Run();

}