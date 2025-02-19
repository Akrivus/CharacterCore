using System.Threading.Tasks;
using UnityEngine;

public class MemoryGeneration : MonoBehaviour, ISubGenerator
{
    [SerializeField]
    private bool fastMode = false;

    [SerializeField]
    private TextAsset _prompt;

    public async Task<Chat> Generate(Chat chat)
    {
        foreach (var actor in chat.Actors)
        {
            var bucket = MemoryBucket.Get(actor.Name);
            var memory = await LLM.CompleteAsync(
                _prompt.Format(chat.Log, actor.Prompt, bucket.Get()), fastMode);
            await bucket.Add(memory);
        }

        return chat;
    }
}