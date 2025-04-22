using System.Threading.Tasks;
using UnityEngine;

public class SequelGeneration : MonoBehaviour, ISubGenerator
{
    [SerializeField]
    private bool fastMode = false;

    [SerializeField]
    private TextAsset _prompt;

    [SerializeField]
    private ChatGenerator[] iterations;

    private int iteration = 0;

    private string slug => name.Replace(' ', '-').ToLower();

    public async Task<Chat> Generate(Chat chat)
    {
        if (chat.Idea.Prompt.Contains("[SEQUEL]") && iteration < iterations.Length)
        {
            var states = "";
            foreach (var actor in chat.Actors)
                states += $"#### {actor.Name}\n\n" + actor.Memory + "\n\n";
            var generator = iterations[iteration % iterations.Length];
            var context = await MemoryBucket.GetContext(slug);
            var prompt = await LLM.CompleteAsync(
                _prompt.Format(context, states), fastMode);
            generator.AddPromptToQueue(prompt);
            iteration++;
        }
        else
        {
            iteration = 0;
        }
        return chat;
    }
}