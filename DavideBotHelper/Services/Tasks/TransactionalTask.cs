using System.Transactions;
using Coravel.Invocable;
using DavideBotHelper.Database.Context;
using DavideBotHelper.Services.Extensions;
using Microsoft.EntityFrameworkCore;

namespace DavideBotHelper.Services.Tasks;

public abstract class TransactionalTask : IInvocable, ICancellableInvocable
{
    private readonly ILogger _log;

    async Task IInvocable.Invoke()
    {
        var transScope = new TransactionScope(TransactionScopeOption.Required, new  TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted });

        try
        {
            await Run();
            transScope.Complete();
        }
        catch (OperationCanceledException oce)
        {
            _log.Warning(oce, "Task cancelled");
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Errore nell'esecuzione del task {taskName}", GetType().Name);
        }
        finally
        {
            transScope.Dispose();
        }

    }

    protected abstract Task Run();

    protected CancellationToken _ct;

    public CancellationToken CancellationToken { get => _ct; set  => _ct = value; }
}