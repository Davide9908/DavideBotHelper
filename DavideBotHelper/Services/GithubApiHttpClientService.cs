using System.Net.Http.Json;
using DavideBotHelper.Database.Context;
using DavideBotHelper.Services.ClassesAndUtilities;


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

    public async Task<byte[]> DownloadAssetFromUri(Uri downloadUri, CancellationToken ct = default)
    {
     
        var request = new HttpRequestMessage(HttpMethod.Get, downloadUri);

        // Imposto header per scaricare l'asset
        request.Headers.Add("Accept", "application/octet-stream");

        using HttpResponseMessage response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

        response.EnsureSuccessStatusCode();
        
        //Ottengo l'array di byte
        using var stream =await response.Content.ReadAsStreamAsync(ct); 
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, ct);
        return memoryStream.ToArray();
        
    }
}