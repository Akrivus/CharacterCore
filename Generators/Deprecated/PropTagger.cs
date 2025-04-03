using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class PropTagger : MonoBehaviour, ISubGenerator
{
    [SerializeField]
    private string[] items;

    [SerializeField]
    private TextAsset _prompt;

    private void Awake()
    {
        if (items.Length == 0)
            items = Resources.LoadAll("Items", typeof(Texture2D))
                .Select(t => t.name)
                .ToArray();
    }

    public async Task<Chat> Generate(Chat chat)
    {
        var names = chat.Names;
        var itemSet = await _prompt.ExtractSet(names, chat.Log)
            .ContinueWith(task => task.Result
                .ToDictionary(
                    line => chat.Actors.Get(line.Key),
                    line => line.Value));
        foreach (var item in itemSet)
            item.Key.Item = item.Value;

        return chat;
    }
}
