using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

public class RedditIntegrator : MonoBehaviour
{
    private static Regex SubRedditFinder = new Regex(@"^- r\/([^\s]+)");

    [SerializeField]
    private RedditIntegration RedditIntegration;

    public async Task<string> ReplaceSubReddits(string context)
    {
        var subreddits = SubRedditFinder.Matches(context)
            .Select(x => x.Groups[1].Value)
            .ToArray();
        foreach (var subreddit in subreddits)
        {
            var tokens = await RedditIntegration.FetchAsync(subreddit, 1);
            var posts = tokens
                .Select((p) => p.Value<string>("subreddit_name_prefixed") + ": " + p.Value<string>("title"))
                .ToArray();
            if (posts.Length == 0)
                continue;
            var post = posts.FirstOrDefault();
            context = context.Replace($"- r/{subreddit}", post);
        }
        return context;
    }
}