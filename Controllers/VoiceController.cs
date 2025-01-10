using System.Linq;
using UnityEngine;

public class VoiceController : AutoActor, ISubNode
{
    public void Activate(ChatNode node)
    {
        var reaction = node.Reactions.FirstOrDefault(r => r.Actor == Actor);
        var score = reaction?.Sentiment.Score ?? 0.0f;
        var pitch = score / 12.5f + 1.0f;
        var volume = score / 5.0f + 1.0f;

        ActorController.Voice.pitch = pitch;
        ActorController.Voice.volume = volume;
    }
}
