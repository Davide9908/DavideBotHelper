using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace DavideBotHelper.Database;

[Table("rc_github_repository")]
public class GithubRepository
{
    [Column("repo_id"), Key]
    public int Id { get; init; }
    
    [Column("name"), MaxLength(60), Required]
    public required string Name { get; init; }
    
    [Column("repository_url"), Required]
    public required Uri RepositoryUrl { get; init; }
    
    [Column("is_enabled"), Required]
    public bool IsEnabled { get; set; }
    
    [Column("last_checked")]
    public DateTime LastChecked { get; set; }
    
    [Column("tag_regex"), MaxLength(100)]
    public string? TagRegexPattern { get; set; }
    
    [Column("version_regex"), MaxLength(100)]
    public string? VersionRegexPattern { get; set; }
    
    [Column("flag_reset_release_cache")]
    public bool ResetReleaseCache { get; set; } = false;
}