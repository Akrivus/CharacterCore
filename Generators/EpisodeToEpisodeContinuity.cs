using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class EpisodeToEpisodeContinuity : MonoBehaviour, ISubGenerator
{
    public static string GroundState => _bucket.Get();
    private static MemoryBucket _bucket = new MemoryBucket("#general");

    [SerializeField]
    private TextAsset _prompt;

    private async void Awake()
    {
        await _bucket.Load();
    }

    public async Task<Chat> Generate(Chat chat)
    {
        var memory = await OpenAiIntegration.CompleteAsync(
            _prompt.Format(chat.Log, GroundState), false);
        await _bucket.Add(memory);

        var buckets = MemoryBucket.Buckets.Values.ToArray();
        foreach (var bucket in buckets)
        {
            bucket.Clean();
            await bucket.Save();
        }

        return chat;
    }
}