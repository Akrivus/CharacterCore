using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class BackgroundNoiseGenerator : MonoBehaviour, ISubGenerator
{
    public static string[] SoundGroups;

    [SerializeField]
    private TextAsset _prompt;

    private string context;

    private void Awake()
    {
        if (SoundGroups == null)
            SoundGroups = Resources.LoadAll<SoundGroup>("SoundGroups")
                .Select(t => t.name)
                .ToArray();
    }

    public async Task<Chat> Generate(Chat chat)
    {
        var names = chat.Names;
        var topic = chat.Topic;

        var soundGroups = await SelectSoundGroup(chat, names, topic);
        foreach (var s in soundGroups)
            chat.Actors.Get(s.Key.Reference).SoundGroup = s.Value;

        return chat;
    }

    private async Task<Dictionary<ActorContext, string>> SelectSoundGroup(Chat chat, string[] names, string topic)
    {
        var options = string.Join(", ", SoundGroups);
        var characters = string.Join("\n- ", names);
        var prompt = _prompt.Format(options, characters, topic);
        var message = await OpenAiIntegration.CompleteAsync(prompt, true);

        var lines = message.Parse(names);

        return lines
            .Where(line => names.Contains(line.Key))
            .ToDictionary(
                line => chat.Actors.Get(line.Key),
                line => line.Value);
    }
}
