using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class EditorProcess : MonoBehaviour, ISubGenerator
{
    [SerializeField]
    private TextAsset _prompt;

    public async Task<Chat> Generate(Chat chat)
    {
        chat.Characters = string.Join("\n", chat.Actors
            .Where(a => a.Reference.Aliases.Any(n => chat.Topic.Contains(n)))
            .Select(actor => string.Format("- {0} ({1})",
                actor.Name,
                actor.Reference.Pronouns))
            .Distinct()
            .ToArray());
        chat.Context = await OpenAiIntegration.CompleteAsync(
            _prompt.Format(chat.Topic, chat.Characters), false);
        return chat;
    }
}