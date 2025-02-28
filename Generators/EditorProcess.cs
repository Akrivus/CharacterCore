using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class EditorProcess : MonoBehaviour, ISubGenerator
{
    [SerializeField]
    private bool fastMode = false;

    [SerializeField]
    private TextAsset _prompt;

    [SerializeField]
    private bool autoComplete = false;

    public async Task<Chat> Generate(Chat chat)
    {
        chat.Characters = string.Join("\n", chat.Actors
            .Select(actor => string.Format("#### {0} ({1})\n\n{2}\n",
                actor.Name,
                actor.Reference.Pronouns,
                actor.Context))
            .Distinct()
            .ToArray());
        if (autoComplete)
            chat.Context = chat.Topic + "\n\n"
                + "### Character Behaviors\n\n"
                + chat.Characters;
        else
            chat.Context = await LLM.CompleteAsync(
                _prompt.Format(
                    chat.Topic,
                    chat.Idea.Prompt,
                    chat.Characters),
                fastMode);
        return chat;
    }
}