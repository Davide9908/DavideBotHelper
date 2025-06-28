using Coravel.Invocable;
using DavideBotHelper.Database;
using DavideBotHelper.Database.Context;
using DavideBotHelper.Services.Extensions;
using Microsoft.EntityFrameworkCore;

namespace DavideBotHelper.Services.Tasks;

public class GithubReleaseDownloadTask : TransactionalTask
{
    private readonly ILogger<GithubReleaseDownloadTask> _log;
    private readonly DavideBotDbContext _dbContext;
    private readonly GithubApiHttpClientService _apiClient;

    public GithubReleaseDownloadTask(ILogger<GithubReleaseDownloadTask> log, DavideBotDbContext dbContext, GithubApiHttpClientService apiClient) : base(log, dbContext)
    {
        _log = log;
        _dbContext = dbContext;
        _apiClient = apiClient;
    }


    protected override async Task Run()
    {
        var assetToDownload = await _dbContext.RepositoryReleases
            .Where(r => r.RequireDownload && r.DownloadUrl != null)
            .ToListAsync(_ct);
        
        if (!assetToDownload.Any())
        {
            _log.Debug("Nessuna asset release da scaricare");
            return;
        }

        Dictionary<int, Task<byte[]>> downloadTasks = new Dictionary<int, Task<byte[]>>();
        foreach (RepositoryRelease asset in assetToDownload)
        {
            var task = _apiClient.DownloadAssetFromUri(asset.DownloadUrl!, _ct);
            downloadTasks.Add(asset.AssetId, task);
        }

        try
        {
            await Task.WhenAll(downloadTasks.Values);
        }
        catch (Exception ex) //evito che WhenAll rilanci l'eccezione del/dei task che fallisce, le gestisco io separatamente
        {
            
        }


        foreach (RepositoryRelease asset in assetToDownload)
        {
            var task = downloadTasks[asset.AssetId];
            if (task.IsFaulted)
            {
                _log.Error(task.Exception, "Errore nel download dell'asset {Id}-{Url}", asset.AssetId, asset.DownloadUrl);
                continue;
            }

            asset.Data = task.Result;
            asset.IsCompressed = false; //resetto anche la compressione nel caso lo abbia richiesto manualmente
            asset.RequireDownload = false;
            asset.ToSend = true;
        }

        _ = await _dbContext.SaveChangesAsync(CancellationToken.None);
    }
}