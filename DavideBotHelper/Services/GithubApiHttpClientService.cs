using System.Net.Http.Json;
using DavideBotHelper.Database.Context;


namespace DavideBotHelper.Services;

public class GithubApiHttpClientService
{
    private readonly ILogger<GithubApiHttpClientService> _log;
    private readonly DavideBotDbContext _dbcontext;
    private readonly HttpClient _client;

    public GithubApiHttpClientService(ILogger<GithubApiHttpClientService> log, DavideBotDbContext dbContext, HttpClient client)
    {
        _log = log;
        _dbcontext = dbContext;
        _client = client;
    }

    public async Task<List<GithubReleases>> GetGHRepositoryReleases(Uri requestUri, CancellationToken ct = default)
    {
        return await _client.GetFromJsonAsync<List<GithubReleases>>(requestUri, options: Converter.Settings, ct) ?? [];
    }
}