﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class Summarizer : MonoBehaviour, ISubGenerator
{
    public static string GroundStateContext => _groundStateContext;
    private static string _groundStateContext;

    [SerializeField]
    private TextAsset _prompt;

    [SerializeField]
    private TextAsset _defaultContext;

    [SerializeField]
    private string fileName = "context.txt";

    [SerializeField]
    private int _contextCount = 4;

    private string _context;
    private List<string> _contexts = new List<string>();

    private ChatGenerator ChatGenerator;

    private void Awake()
    {
        LoadGroundStateContext();

        ChatGenerator = GetComponent<ChatGenerator>();
        ChatGenerator.DefaultSummarizer += Summarize;
    }

    private async Task Summarize(Chat chat)
    {
        chat.AppendContext(_context);
        chat.FinalizeContext();
        SaveGroundStateContext();
        await Task.CompletedTask;
    }

    public async Task<Chat> Generate(Chat chat)
    {
        _contexts.Add(await OpenAiIntegration.CompleteAsync(
            _prompt.Format(chat.Log, _context), true));

        var context = string.Empty;
        for (var i = 0; i < Math.Min(_contextCount, _contexts.Count); i++)
            context += $"{i + 1}. " + _contexts[_contexts.Count - 1 - i] + "\n";

        _context = context;
        
        return chat;
    }

    private void LoadGroundStateContext()
    {
        if (!File.Exists(fileName))
            File.WriteAllText(fileName, _defaultContext.text);
        _context = File.ReadAllText(fileName);
        _contexts = _context.Split('\n').ToList();
        _groundStateContext = _context;
    }

    private void SaveGroundStateContext()
    {
        var context = string.Join("\n", _contexts.TakeLast(_contextCount));
        File.WriteAllText(fileName, context);
        _groundStateContext = context;
    }
}