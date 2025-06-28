using System.Text.RegularExpressions;
using Coravel.Invocable;
using DavideBotHelper.Database;
using DavideBotHelper.Database.Context;
using DavideBotHelper.Services.ClassesAndUtilities;
using DavideBotHelper.Services.Extensions;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Types;

namespace DavideBotHelper.Services.Tasks;

public sealed class GithubReleasesCheckerTask : TransactionalTask
{
    private readonly ILogger<GithubReleasesCheckerTask> _log;
    private readonly DavideBotDbContext _dbContext;
    private readonly GithubApiHttpClientService _apiClient;
    private const string DefaultRegPattern = ".*";
    private readonly TelegramBotService _telegramBotService;
    private readonly ChatId _chatId = new ChatId(38076310);

    public GithubReleasesCheckerTask(ILogger<GithubReleasesCheckerTask> log, DavideBotDbContext dbContext,
        GithubApiHttpClientService apiClient, TelegramBotService telegramBotService) : base(log, dbContext)
    {
        _log = log;
        _dbContext = dbContext;
        _apiClient = apiClient;
        _telegramBotService = telegramBotService;
    }

    protected override async Task Run()
    {
        var repos = await _dbContext.GithubRepositories.Where(r => r.IsEnabled).ToListAsync(_ct);

        var tasks = new Dictionary<string, Task<List<GithubReleases>>>(repos.Count);

        foreach (var repo in repos)
        {
            tasks.Add(repo.Name, _apiClient.GetGHRepositoryReleases(repo.RepositoryUrl, _ct));
        }

        try
        {
            await Task.WhenAll(tasks.Values);
        }
        catch (Exception e) //evito che WhenAll rilanci l'eccezione del/dei task che fallisce, le gestisco io separatamente
        {
            
        }
        
        List<RepositoryRelease> releasesToInsert = [];

        foreach (var repo in repos)
        {
            var task = tasks[repo.Name];
            var tagRegex = new Regex(repo.TagRegexPattern ?? DefaultRegPattern);
            var versionRegex = new Regex(repo.VersionRegexPattern ?? DefaultRegPattern, RegexOptions.Compiled);

            if (task.IsFaulted)
            {
                _log.Error(task.Exception, "Error retrieving releases from repository {repository} - {url}", repo.Name,
                    repo.RepositoryUrl);
                return;
            }

            var releases = task.Result.Where(gr => tagRegex.IsMatch(gr.TagName)).ToList();

            if (!releases.Any())
            {
                _log.Warning("Non ho ricevuto release. Repo:{repositoryName}-{repositoryUrl}", repo.Name,
                    repo.RepositoryUrl);
                return;
            }

            List<RepositoryRelease> releaseToInsertPart;
            if (repo.ResetReleaseCache)
            {
                releaseToInsertPart = await ResetReleasesCache(repo, releases, versionRegex);
                repo.ResetReleaseCache = false;
            }
            else
            {
                releaseToInsertPart = await AddAndGetLastRelease(repo, releases, versionRegex, _ct);
            }

            releasesToInsert.AddRange(releaseToInsertPart);
            if (releaseToInsertPart.Any(r => r.RequireDownload))
            {
                await _telegramBotService.SendMessage(_chatId, $"Trovata una release da scaricare. Repo: {repo.Name}");
            }
        }
        
        if (releasesToInsert.Any())
        {
            await _dbContext.RepositoryReleases.AddRangeAsync(releasesToInsert, CancellationToken.None);
            _ = await _dbContext.SaveChangesAsync(CancellationToken.None);
            
        }
        else
        {
            _log.Debug("Nessuna release da scaricare");
        }
        
        
    }

    /// <returns>The last release of the repository according to regex</returns>
    private async Task<List<RepositoryRelease>> ResetReleasesCache(GithubRepository repo, List<GithubReleases> releasesList,
        Regex versionRegex)
    {
        await _dbContext.RepositoryReleases
            .Where(r => r.RepositoryId == repo.RepositoryId)
            .ExecuteDeleteAsync(CancellationToken.None);

        List<RepositoryRelease> repositoryReleases = [];
        foreach (var release in releasesList)
        {
            var repoRelease = new RepositoryRelease()
            {
                AddedAt = DateTime.UtcNow,
                RepositoryId = repo.RepositoryId,
                Version = release.TagNameNoV,
                RequireDownload = false
            };

            var asset = release.Assets.FirstOrDefault(a => versionRegex.IsMatch(a.Name));
            if (asset is null)
            {
                _log.Warning(
                    "Repository {repo}, release {release} - Non sono riuscito a recuperare l'asset secondo la regex della versione, i dati specifici dell'asset non verranno caricati",
                    repo.Name, release.Name);
                repositoryReleases.Add(repoRelease);
                continue;
            }

            repoRelease.FileName = asset.Name;
            repoRelease.DownloadUrl = asset.Url;
            repoRelease.Size = asset.Size;
            repoRelease.Data = null;
            repoRelease.IsCompressed = false;
            repositoryReleases.Add(repoRelease);
        }

        return repositoryReleases;
        // await _dbcontext.RepositoryReleases.AddRangeAsync(repositoryReleases, CancellationToken.None);
        // await _dbcontext.SaveChangesAsync(CancellationToken.None);

    }

    private async Task<List<RepositoryRelease>> AddAndGetLastRelease(GithubRepository repo, List<GithubReleases> releasesList,
        Regex versionRegex, CancellationToken ct)
    {
        var lastDbReleaseNumber = await _dbContext.RepositoryReleases
            .Where(r => r.RepositoryId == repo.RepositoryId)
            .Select(r => r.Version)
            .OrderByDescending(version => version)
            .FirstOrDefaultAsync(ct);
        releasesList = releasesList.OrderByDescending(r => r.TagName).ToList();
        
        if (string.IsNullOrEmpty(lastDbReleaseNumber))
        {
            var release = releasesList.First();
            var repoRelease = new RepositoryRelease()
            {
                AddedAt = DateTime.UtcNow,
                RepositoryId = repo.RepositoryId,
                Version = release.TagNameNoV,
                RequireDownload = true
            };

            var asset = release.Assets.FirstOrDefault(a => versionRegex.IsMatch(a.Name));
            if (asset is null)
            {
                _log.Warning(
                    "Repository {repo}, release {release} - Non sono riuscito a recuperare l'asset secondo la regex della versione, i dati specifici dell'asset non verranno caricati",
                    repo.Name, release.Name);
                return [repoRelease];
            }

            repoRelease.FileName = asset.Name;
            repoRelease.DownloadUrl = asset.Url;
            repoRelease.Size = asset.Size;
            repoRelease.Data = null;
            repoRelease.IsCompressed = false;
            return [repoRelease];
        }

        var releasesToAdd = releasesList.Where(rl => lastDbReleaseNumber.CompareTo(rl.TagNameNoV) < 0).ToList();
        List<RepositoryRelease> repositoryReleases = [];
        
        foreach (var release in releasesToAdd)
        {
            var repoRelease = new RepositoryRelease()
            {
                AddedAt = DateTime.UtcNow,
                RepositoryId = repo.RepositoryId,
                Version = release.TagNameNoV,
                RequireDownload = false
            };

            var asset = release.Assets.FirstOrDefault(a => versionRegex.IsMatch(a.Name));
            if (asset is null)
            {
                _log.Warning(
                    "Repository {repo}, release {release} - Non sono riuscito a recuperare l'asset secondo la regex della versione, i dati specifici dell'asset non verranno caricati",
                    repo.Name, release.Name);
                repositoryReleases.Add(repoRelease);
                continue;
            }

            repoRelease.FileName = asset.Name;
            repoRelease.DownloadUrl = asset.Url;
            repoRelease.Size = asset.Size;
            repoRelease.Data = null;
            repoRelease.IsCompressed = false;
            repositoryReleases.Add(repoRelease);
        }
        
        //Faccio scaricare solo l'ultimo per ordine di versione
        if (repositoryReleases.Count > 0)
        {
            repositoryReleases.OrderByDescending(r => r.Version).First().RequireDownload = true;
        }

        return repositoryReleases;
        
        // await _dbcontext.RepositoryReleases.AddRangeAsync(repositoryReleases, CancellationToken.None);
        // await _dbcontext.SaveChangesAsync(CancellationToken.None);

    }
}