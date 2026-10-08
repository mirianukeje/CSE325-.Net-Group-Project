using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using SocialMediaApp.Data;

namespace SocialMediaApp.Services;

public class LikeService(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    AuthenticationStateProvider authenticationStateProvider)
{
    private async Task<string> GetUserIdAsync()
    {
        var user = (await authenticationStateProvider.GetAuthenticationStateAsync()).User;
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (user.Identity?.IsAuthenticated != true || string.IsNullOrEmpty(id))
            throw new UnauthorizedAccessException("Please log in to like posts.");

        return id;
    }

    public async Task<bool> HasLikedAsync(int postId)
    {
        var userId = await GetUserIdAsync();

        await using var db = await contextFactory.CreateDbContextAsync();

        return await db.PostLikes.AnyAsync(
            like => like.PostId == postId && like.UserId == userId);
    }

    public async Task<int> GetLikeCountAsync(int postId)
    {
        await GetUserIdAsync();

        await using var db = await contextFactory.CreateDbContextAsync();

        return await db.PostLikes.CountAsync(
            like => like.PostId == postId);
    }

    public async Task ToggleLikeAsync(int postId)
    {
        var userId = await GetUserIdAsync();

        await using var db = await contextFactory.CreateDbContextAsync();

        if (!await db.Posts.AnyAsync(post => post.Id == postId))
            throw new InvalidOperationException("This post is no longer available.");

        var existingLike = await db.PostLikes.SingleOrDefaultAsync(
            like => like.PostId == postId && like.UserId == userId);

        if (existingLike is null)
        {
            db.PostLikes.Add(new PostLike
            {
                PostId = postId,
                UserId = userId,
                CreatedAtUtc = DateTime.UtcNow
            });
        }
        else
        {
            db.PostLikes.Remove(existingLike);
        }

        await db.SaveChangesAsync();
    }

    public async Task<List<Post>> GetLikedPostsAsync(string userId)
    {
        await using var db = await contextFactory.CreateDbContextAsync();

        return await db.Posts
            .AsNoTracking()
            .Include(post => post.User)
            .Where(post => db.PostLikes.Any(
                like => like.PostId == post.Id && like.UserId == userId))
            .OrderByDescending(post => post.CreatedAtUtc)
            .ThenByDescending(post => post.Id)
            .ToListAsync();
    }
}
