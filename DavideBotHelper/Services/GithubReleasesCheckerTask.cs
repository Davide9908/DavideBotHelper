using System.Text.RegularExpressions;
using Coravel.Invocable;
using DavideBotHelper.Database;
using DavideBotHelper.Database.Context;
using DavideBotHelper.Services.Extensions;
using Microsoft.EntityFrameworkCore;

namespace DavideBotHelper.Services;

public sealed class GithubReleasesCheckerTask : IInvocable, ICancellableInvocable
{
    private readonly ILogger<GithubReleasesCheckerTask> _log;
    private readonly DavideBotDbContext _dbcontext;
    private readonly GithubApiHttpClientService _apiClient;
    private CancellationToken _cancellationToken;
    private const string DeafultRegPattern = ".*";

    public CancellationToken CancellationToken
    {
        get => _cancellationToken;
        set => _cancellationToken = value;
    }

    public GithubReleasesCheckerTask(ILogger<GithubReleasesCheckerTask> log, DavideBotDbContext dbContext,
        GithubApiHttpClientService apiClient)
    {
        _log = log;
        _dbcontext = dbContext;
        _apiClient = apiClient;
    }

    public async Task Invoke()
    {
        try
        {
            await Run();
        }
        catch (OperationCanceledException oce)
        {
            _log.Warning(oce, "Operation cancelled");
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Error in GithubReleasesCheckerTask");
        }
    }

    private async Task Run()
    {
        var repos = await _dbcontext.GithubRepositories.Where(r => r.IsEnabled).ToListAsync(_cancellationToken);

        var tasks = new Dictionary<string, Task<List<GithubReleases>>>(repos.Count);

        foreach (var repo in repos)
        {
            tasks.Add(repo.Name, _apiClient.GetGHRepositoryReleases(repo.RepositoryUrl, _cancellationToken));
        }

        await Task.WhenAll(tasks.Values);

        foreach (var repo in repos)
        {
            var task = tasks[repo.Name];
            var tagRegex = new Regex(repo.TagRegexPattern ?? DeafultRegPattern);
            var versionRegex = new Regex(repo.VersionRegexPattern ?? DeafultRegPattern, RegexOptions.Compiled);

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

            RepositoryRelease lastRelease;
            if (repo.ResetReleaseCache)
            {
                lastRelease = await ResetReleasesCache(repo, releases, versionRegex);
            }
            else
            {
                lastRelease = await AddAndGetLastRelease(repo, releases, versionRegex, _cancellationToken);
            }
            
        }
    }

    /// <returns>The last release of the repository according to regex</returns>
    private async Task<RepositoryRelease> ResetReleasesCache(GithubRepository repo, List<GithubReleases> releasesList,
        Regex versionRegex)
    {
        await _dbcontext.RepositoryReleases
            .Where(r => r.RepositoryId == repo.Id)
            .ExecuteDeleteAsync(CancellationToken.None);

        List<RepositoryRelease> repositoryReleases = [];
        foreach (var release in releasesList)
        {
            var repoRelease = new RepositoryRelease()
            {
                AddedAt = DateTime.Now,
                RepositoryId = repo.Id,
                Version = release.TagNameNoV
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

            repoRelease.DownloadUrl = asset.Url;
            repoRelease.Size = asset.Size;
            repoRelease.Data = null;
            repoRelease.IsCompressed = false;
            repositoryReleases.Add(repoRelease);
        }

        await _dbcontext.RepositoryReleases.AddRangeAsync(repositoryReleases, CancellationToken.None);
        await _dbcontext.SaveChangesAsync(CancellationToken.None);

        return repositoryReleases.OrderByDescending(r => r.Version).First();
    }

    private async Task<RepositoryRelease> AddAndGetLastRelease(GithubRepository repo, List<GithubReleases> releasesList,
        Regex versionRegex, CancellationToken ct)
    {
        var lastDbReleaseNumber = await _dbcontext.RepositoryReleases
            .Where(r => r.RepositoryId == repo.Id)
            .Select(r => r.Version)
            .OrderByDescending(version => version)
            .FirstOrDefaultAsync(ct);
        releasesList = releasesList.OrderByDescending(r => r.TagName).ToList();
        
        if (string.IsNullOrEmpty(lastDbReleaseNumber))
        {
            var release = releasesList.First();
            var repoRelease = new RepositoryRelease()
            {
                AddedAt = DateTime.Now,
                RepositoryId = repo.Id,
                Version = release.TagNameNoV
            };

            var asset = release.Assets.FirstOrDefault(a => versionRegex.IsMatch(a.Name));
            if (asset is null)
            {
                _log.Warning(
                    "Repository {repo}, release {release} - Non sono riuscito a recuperare l'asset secondo la regex della versione, i dati specifici dell'asset non verranno caricati",
                    repo.Name, release.Name);
                return repoRelease;
            }

            repoRelease.DownloadUrl = asset.Url;
            repoRelease.Size = asset.Size;
            repoRelease.Data = null;
            repoRelease.IsCompressed = false;
            return repoRelease;
        }

        var releasesToAdd = releasesList.Where(rl => lastDbReleaseNumber.CompareTo(rl.TagNameNoV) < 0).ToList();
        List<RepositoryRelease> repositoryReleases = [];
        
        foreach (var release in releasesToAdd)
        {
            var repoRelease = new RepositoryRelease()
            {
                AddedAt = DateTime.Now,
                RepositoryId = repo.Id,
                Version = release.TagNameNoV
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

            repoRelease.DownloadUrl = asset.Url;
            repoRelease.Size = asset.Size;
            repoRelease.Data = null;
            repoRelease.IsCompressed = false;
            repositoryReleases.Add(repoRelease);
        }
        
        await _dbcontext.RepositoryReleases.AddRangeAsync(repositoryReleases, CancellationToken.None);
        await _dbcontext.SaveChangesAsync(CancellationToken.None);

        return repositoryReleases.OrderByDescending(r => r.Version).First();

    }
}