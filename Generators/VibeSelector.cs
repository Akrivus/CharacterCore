using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class VibeSelector : MonoBehaviour, ISubGenerator
{
    [SerializeField]
    private AudioClip[] vibes;

    [SerializeField]
    private TextAsset _prompt;

    public async Task<Chat> Generate(Chat chat)
    {
        var options = string.Join(", ", vibes.Select(vibe => vibe.name));

        try
        {
            var prompt = _prompt.Format(options, chat.Log, chat.Topic);
            var output = await LLM.CompleteAsync(prompt);
            var vibe = vibes.FirstOrDefault(vibe => output.Contains(vibe.name)).name;

            chat.Vibe = vibe;
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }

        return chat;
    }
}