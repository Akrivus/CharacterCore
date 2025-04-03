using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class SequelGeneration : MonoBehaviour, ISubGenerator
{
    [SerializeField]
    private ChatGenerator generator;

    [SerializeField]
    private bool fastMode = false;

    [SerializeField]
    private TextAsset _prompt;

    [SerializeField]
    private int maxSequels = 1;

    private int sequels = 0;

    public async Task<Chat> Generate(Chat chat)
    {
        if (chat.Idea.Prompt.Contains("[SEQUEL]") && sequels < maxSequels)
        {
            var states = "";
            foreach (var actor in chat.Actors)
                states += $"#### {actor.Name}\n\n" + actor.Memory + "\n\n";
            var prompt = await LLM.CompleteAsync(
                _prompt.Format(EpisodeToEpisodeContinuity.GroundState, states), fastMode);
            generator.AddPromptToQueue(prompt);
            sequels++;
        }
        else
        {
            sequels = 0;
        }
        return chat;
    }
}