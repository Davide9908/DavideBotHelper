using Coravel.Invocable;
using DavideBotHelper.Services.Extensions;

namespace DavideBotHelper.Services.Tasks;

public abstract class BaseTask : IInvocable, ICancellableInvocable
{
    private readonly ILogger _log;

    protected BaseTask(ILogger log)
    {
        _log = log;
    }

    public virtual async Task Invoke()
    {
        try
        {
            await Run();
        }
        catch (OperationCanceledException oce)
        {
            _log.Warning(oce, "Task cancelled");
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Errore nell'esecuzione del task {taskName}", GetType().Name);
        }

    }

    protected abstract Task Run();

    protected CancellationToken _ct;

    public CancellationToken CancellationToken { get => _ct; set  => _ct = value; }
}