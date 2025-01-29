using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

public class MemoryGeneration : MonoBehaviour, ISubGenerator
{
    [SerializeField]
    private TextAsset _prompt;

    public async Task<Chat> Generate(Chat chat)
    {
        foreach (var actor in chat.Actors)
        {
            var bucket = MemoryBucket.Buckets[actor.Name];
            var memory = await OpenAiIntegration.CompleteAsync(
                _prompt.Format(chat.Log, actor.Prompt, bucket.Get()), false);
            await bucket.Add(memory);
        }

        return chat;
    }
}