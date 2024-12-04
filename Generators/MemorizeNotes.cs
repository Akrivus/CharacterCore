using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class MemorizeNotes : MonoBehaviour, ISubGenerator
{
    [SerializeField]
    private TextAsset _prompt;

    public async Task<Chat> Generate(Chat chat)
    {
        foreach (var actor in chat.Actors)
        {
            var note = new StringBuilder();
            foreach (var node in chat.Nodes)
                note.AppendLine(node.Notes);
            var memory = note.ToString();

            var output = await OpenAiIntegration.CompleteAsync(_prompt.Format(memory), true);
            actor.AddMemory(output);
        }
        return chat;
    }
}