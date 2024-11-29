using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class MemoryGenerator : MonoBehaviour, ISubGenerator
{
    [SerializeField]
    private TextAsset _prompt;

    public async Task<Chat> Generate(Chat chat)
    {
        var output = await ChatClient.CompleteAsync(_prompt.Format(chat.Log, chat.Context), true);
        var memories = output.Parse(chat.Actors.Select(actor => actor.Name).ToArray());

        foreach (var actor in chat.Actors)
            actor.SaveMemories(memories[actor.Name]);
        
        return chat;
    }
}