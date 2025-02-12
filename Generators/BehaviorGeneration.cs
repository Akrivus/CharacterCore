using System.Threading.Tasks;
using UnityEngine;

public class BehaviorGeneration : MonoBehaviour, ISubGenerator
{
    [SerializeField]
    private TextAsset _prompt;

    public async Task<Chat> Generate(Chat chat)
    {
        foreach (var actor in chat.Actors)
        {
            var bucket = new MemoryBucket(actor.Name);
            await bucket.Load();
            actor.Context = await LLM.CompleteAsync(
                _prompt.Format(
                    chat.Topic,
                    chat.Idea.Prompt,
                    actor.Prompt,
                    bucket.Get()),
                false);
        }
        return chat;
    }
}