﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class DialogueEditorProcess : MonoBehaviour, ISubGenerator
{
    [SerializeField]
    private bool fastMode = false;

    [SerializeField]
    private TextAsset _prompt;

    public async Task<Chat> Generate(Chat chat)
    {
        if (chat == null || chat.IsLocked)
            return chat;
        var prompt = _prompt.Format(chat.Context, chat.Log, chat.Idea.Prompt);
        var content = await LLM.CompleteAsync(prompt, fastMode);
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length != chat.Nodes.Count)
            Debug.LogWarning("Number of lines does not match number of nodes.");

        var max = Math.Min(lines.Length, chat.Nodes.Count);

        for (int i = 0; i < max; i++)
        {
            var line = lines[i];
            var parts = line.Split(':');
            if (parts.Length <= 1)
                continue;

            var name = parts[0];
            var text = string.Join(":", parts.Skip(1));

            var node = chat.Nodes[i];
            if (node.Actor.Name == name)
                node.SetText(text);
        }

        return chat;
    }
}