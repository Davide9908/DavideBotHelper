using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DavideBotHelper.Services;
using DavideBotHelper.Services.ClassesAndUtilities;

namespace DavideBotHelper.Database;

[Table("rc_repository_release")]
public class RepositoryRelease
{
    [Column("release_id"), Key]
    public int AssetId { get; init; }
    
    [Column("repository_id"), ForeignKey(nameof(GithubRepository)), Required]
    public int RepositoryId { get; init; }
    public GithubRepository GithubRepository { get; set; }
    
    [Column("filename"), Required]
    public string FileName { get; set; }
    
    [Column("version"), Required]
    public string Version { get; set; }
    
    [Column("download_url")]
    public Uri? DownloadUrl { get; set; }
    
    [Column("data")]
    public byte[]? Data { get; set; }
    
    [Column("size")]
    public long? Size { get; set; }
    
    [Column("is_compressed")]
    public bool IsCompressed { get; set; }
    
    [Column("require_download")]
    public bool RequireDownload { get; set; }
    
    [Column("to_send")]
    public bool ToSend { get; set; }
    
    [Column("added_at")]
    public DateTime? AddedAt { get; set; }
}