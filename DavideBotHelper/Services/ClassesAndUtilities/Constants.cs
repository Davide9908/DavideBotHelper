namespace DavideBotHelper.Services.ClassesAndUtilities;

public static class Constants
{
    //Faccio 70MB nella speranza che una volta compresso sia < 50MB
    public const int MaxUncompressedAssetDataSize = 70 * 1024 * 1024;
    //I bot di telegram possono mandare al massimo 50MB, per sicurezza limito a 49 
    public const int MaxCompressedAssetDataSize = 49 * 1024 * 1024;
    public const string CompressedDataFileExtension = ".zip";
    public const string HeaderUserAgent = "dotNET HTTP Client/1.0 personal bot agent";
    public const string Every25MinutesCron = "*/25 * * * *";
    public const int Every3Seconds = 3;
}