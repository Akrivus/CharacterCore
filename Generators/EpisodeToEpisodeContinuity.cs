using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class EpisodeToEpisodeContinuity : MonoBehaviour, ISubGenerator
{
    public static string GroundState => _bucket.Get();
    private static MemoryBucket _bucket = new MemoryBucket("#general");

    [SerializeField]
    private bool fastMode = false;

    [SerializeField]
    private TextAsset _prompt;

    private async void Awake()
    {
        await _bucket.Load();
    }

    public async Task<Chat> Generate(Chat chat)
    {
        var memory = await LLM.CompleteAsync(
            _prompt.Format(chat.Log, GroundState), fastMode);
        await _bucket.Add(memory);

        var buckets = MemoryBucket.Buckets.Values.ToArray();
        foreach (var bucket in buckets)
            await bucket.Save();

        return chat;
    }

    public static async Task<string> GetLastEpisodeContext()
    {
        await _bucket.Load();
        return _bucket.Get();
    }
}