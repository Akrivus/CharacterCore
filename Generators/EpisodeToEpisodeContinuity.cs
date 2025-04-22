using System.Threading.Tasks;
using UnityEngine;

public class EpisodeToEpisodeContinuity : MonoBehaviour, ISubGenerator
{
    [SerializeField]
    private bool fastMode = false;

    [SerializeField]
    private TextAsset _prompt;

    private string slug => "#" + name.Replace(' ', '-').ToLower();

    public async Task<Chat> Generate(Chat chat)
    {
        var bucket = await MemoryBucket.Get(slug);
        var memory = await LLM.CompleteAsync(
            _prompt.Format(chat.Log, bucket.Get(), chat.Idea.Prompt), fastMode);
        await bucket.Add(memory);

        return chat;
    }
}