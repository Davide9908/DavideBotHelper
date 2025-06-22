using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DavideBotHelper.Database;

[Table("rc_repository_release")]
public class RepositoryRelease
{
    [Column("release_id"), Key]
    public int Id { get; init; }
    
    [Column("filename"), Required]
    public string FileName { get; set; }
    
    [Column("version"), Required]
    public string Version { get; set; }
    
    [Column("download_url"), Required]
    public Uri DownloadUrl { get; set; }
    
    [Column("data")]
    public byte[]? Data { get; set; }
    
    [Column("size")]
    public long? Size { get; set; }
    
    [Column("is_compressed")]
    public bool IsCompressed { get; set; }
    
    [Column("added_at")]
    public DateTime? AddedAt { get; set; }
}