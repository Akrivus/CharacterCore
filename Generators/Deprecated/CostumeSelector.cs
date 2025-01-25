using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class CostumeSelector : MonoBehaviour, ISubGenerator
{
    [SerializeField]
    private string[] costumes;

    [SerializeField]
    private TextAsset _prompt;

    public async Task<Chat> Generate(Chat chat)
    {
        var names = chat.Actors.Select(actor => $"{actor.Name} ({actor.Reference.Pronouns})").ToArray();
        var topic = chat.Topic;

        var options = string.Join("\n- ", costumes);
        var context = options + "\n\n### Additional Information:\n\n" + topic;

        try
        {
            var set = await _prompt.ExtractSet(names, context, chat.Names)
                .ContinueWith(task => task.Result
                    .Where(o => chat.Actors.Get(o.Key) != null && costumes.Contains(o.Value))
                    .ToDictionary(o => chat.Actors.Get(o.Key), o => o.Value));
            foreach (var o in set)
                o.Key.Costume = o.Value;
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }

        return chat;
    }
}