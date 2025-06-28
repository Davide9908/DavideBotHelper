using System.Transactions;
using Coravel.Invocable;
using DavideBotHelper.Database.Context;
using DavideBotHelper.Services.Extensions;
using Microsoft.EntityFrameworkCore;

namespace DavideBotHelper.Services.Tasks;

public abstract class TransactionalTask : IInvocable, ICancellableInvocable
{
    private readonly ILogger _log;
    private readonly DavideBotDbContext? _dbContext;

    protected TransactionalTask(ILogger log, DavideBotDbContext? context = null)
    {
        _log = log;
        _dbContext = context;
    }

    async Task IInvocable.Invoke()
    {
        //var transScope = new TransactionScope(TransactionScopeOption.Required, new  TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled);
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

    protected abstract Task Run();

    protected CancellationToken _ct;

    public CancellationToken CancellationToken { get => _ct; set  => _ct = value; }
}