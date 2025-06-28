using System.IO.Compression;
using DavideBotHelper.Database;
using DavideBotHelper.Database.Context;
using DavideBotHelper.Services.ClassesAndUtilities;
using DavideBotHelper.Services.Extensions;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Telegram.Bot.Types;

namespace DavideBotHelper.Services.Tasks;

public class SendReleaseAssetTask : TransactionalTask
{
    private readonly ILogger<SendReleaseAssetTask> _log;
    private readonly DavideBotDbContext _dbContext;
    private readonly TelegramBotService _telegramBotService;

    private readonly ChatId _chatId = new ChatId(38076310);

    public SendReleaseAssetTask(ILogger<SendReleaseAssetTask> log, DavideBotDbContext dbContext,
        TelegramBotService telegramBotService) : base(log, dbContext)
    {
        _log = log;
        _dbContext = dbContext;
        _telegramBotService = telegramBotService;
    }

    protected override async Task Run()
    {
        var releasesToSend = await _dbContext.RepositoryReleases.Include(release => release.GithubRepository)
            .Where(release => release.ToSend)
            .ToListAsync(_ct);

        foreach (RepositoryRelease assetToSend in releasesToSend)
        {
            if (assetToSend.Data == null)
            {
                _log.Error("Asset data non presente, ma asset settato per essere mandato");
                assetToSend.ToSend = false;
                _ = await _dbContext.SaveChangesAsync();
                continue;
            }

            if (assetToSend.Data.Length > Constants.MaxUncompressedAssetDataSize)
            {
                _log.Warning("Asset {assetName} repository {repositoryName} supera il limite massimo da non compresso", assetToSend.FileName, assetToSend.GithubRepository.Name);
                _ = await _telegramBotService.SendMessage(_chatId,
                    $"Asset {assetToSend.FileName} repository {assetToSend.GithubRepository.Name} supera il limite massimo da non compresso");
                assetToSend.ToSend = false;
                _ = await _dbContext.SaveChangesAsync(CancellationToken.None);
                continue;
            }

            if (assetToSend.IsCompressed && assetToSend.Data.Length > Constants.MaxCompressedAssetDataSize)
            {
                _ = await _telegramBotService.SendMessage(_chatId,
                    $"Asset {assetToSend.FileName} repository {assetToSend.GithubRepository.Name} troppo grande per essere mandato anche dopo la compressione");
                _log.Warning(
                    "Asset {assetName} repository {repositoryName} troppo grande per essere mandato anche dopo la compressione",
                    assetToSend.FileName, assetToSend.GithubRepository.Name);
                
                assetToSend.ToSend = false;
                _ = await _dbContext.SaveChangesAsync(CancellationToken.None);
                continue;
            }
            
            if (assetToSend.Data.Length > Constants.MaxCompressedAssetDataSize)
            {
                byte[] compressedData = await CompressAssetData(assetToSend.Data, assetToSend.FileName);
                if (compressedData.Length > Constants.MaxCompressedAssetDataSize)
                {
                    _ = await _telegramBotService.SendMessage(_chatId,
                        $"Asset {assetToSend.FileName} repository {assetToSend.GithubRepository.Name} troppo grande per essere mandato anche dopo la compressione");
                    _log.Warning(
                        "Asset {assetName} repository {repositoryName} troppo grande per essere mandato anche dopo la compressione",
                        assetToSend.FileName, assetToSend.GithubRepository.Name);
                    
                    assetToSend.ToSend = false;
                    _ = await _dbContext.SaveChangesAsync(CancellationToken.None);
                    continue;
                }

                assetToSend.Data = compressedData;
                assetToSend.FileName += Constants.CompressedDataFileExtension;
                assetToSend.Size = compressedData.Length;
                assetToSend.IsCompressed = true;
                _ = await _dbContext.SaveChangesAsync();
            }

            var assetStream = new MemoryStream(assetToSend.Data);
            try
            {
                _ = await _telegramBotService.SendDocumentAsync(new ChatId(38076310), assetStream, assetToSend.FileName,
                    $"Nuova release da {assetToSend.GithubRepository.Name}, versione {assetToSend.Version}!");
            }
            finally
            {
                await assetStream.DisposeAsync();
            }
            assetToSend.ToSend = false;
        }
        _ = await _dbContext.SaveChangesAsync();
    }

    private async Task<byte[]> CompressAssetData(byte[] assetDataUncompressed, string assetFileName)
    {
        using MemoryStream uncompressedMemoryStream = new MemoryStream(assetDataUncompressed);
        using MemoryStream compressedMemoryStream = new MemoryStream();

        using (var zipArchive = new ZipArchive(compressedMemoryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = zipArchive.CreateEntry(assetFileName);
            await using var entryStream = entry.Open();
            
            //metto lo stream non compresso a posizione 0, non so il problema fosse qui o sotto, ma senza l'entry risulta a 0 byte 
            uncompressedMemoryStream.Position = 0;
            
            //copio l'asset dentro l'entry dello zip
            await uncompressedMemoryStream.CopyToAsync(entryStream);
        }
        //metto lo stream compresso a posizione 0, non so il problema fosse qui o sopra, ma senza l'entry risulta a 0 byte 
        compressedMemoryStream.Position = 0;

        return compressedMemoryStream.ToArray();
    }
}