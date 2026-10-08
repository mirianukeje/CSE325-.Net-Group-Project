using System.ComponentModel.DataAnnotations;

namespace SocialMediaApp.Data;

public class PostLike
{
    public int Id { get; set; }

    [Required]
    public int PostId { get; set; }

    public Post Post { get; set; } = null!;

    [Required]
    public string UserId { get; set; } = "";

    public ApplicationUser User { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; }
}