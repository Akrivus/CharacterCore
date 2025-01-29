﻿using OpenAI;
using OpenAI.Chat;
using OpenAI.Embeddings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class OpenAiIntegration : MonoBehaviour, IConfigurable<OpenAIConfigs>
{
    public static string OPENAI_API_KEY = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    public static string OPENAI_API_URI = "https://api.openai.com";

    public static string SLOW_MODEL = "gpt-4o";
    public static string FAST_MODEL = "gpt-4o-mini";

    public static OpenAIClient API => _api ??= new OpenAIClient(new OpenAIAuthentication(OPENAI_API_KEY), new OpenAISettings(OPENAI_API_URI));
    private static OpenAIClient _api;

    public void Configure(OpenAIConfigs c)
    {
        OPENAI_API_URI = c.ApiUri;
        OPENAI_API_KEY = c.ApiKey;

        SLOW_MODEL = c.SlowModel;
        FAST_MODEL = c.FastModel;
    }

    private void Awake()
    {
        ConfigManager.Instance.RegisterConfig(typeof(OpenAIConfigs), "openai", (config) => Configure((OpenAIConfigs) config));
    }

    public static async Task<List<Message>> ChatAsync(List<Message> messages, bool fast = false)
    {
        try
        {
            var cts = new System.Threading.CancellationTokenSource();
            cts.CancelAfter(10000);

            Debug.Log(messages.Last().Content);

            var model = fast ? FAST_MODEL : SLOW_MODEL;
            var request = await API.ChatEndpoint.GetCompletionAsync(new ChatRequest(messages, model), cts.Token);

            var response = request.FirstChoice;
            if (response.FinishReason != "stop")
                throw new Exception(response.FinishDetails);
            messages.Add(response.Message);

            Debug.Log(response.Message.Content);
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
        return messages;
    }

    public static async Task<List<Message>> ChatAsync(string prompt, bool fast = false)
    {
        _.Clear();
        _.Add(new Message(Role.System, prompt));
        return await ChatAsync(_, fast);
    }

    public static async Task<string> CompleteAsync(string prompt, bool fast = false)
    {
        var messages = await ChatAsync(prompt, fast);
        return messages.Last().Content.ToString();
    }

    public static async Task<double[]> EmbedAsync(string text, int dimensions = 1532)
    {
        if (string.IsNullOrEmpty(text))
            return new double[0];
        try
        {
            var request = await API.EmbeddingsEndpoint.CreateEmbeddingAsync(new EmbeddingsRequest(text, "text-embedding-3-small", "me", dimensions));
            return request.Data.FirstOrDefault().Embedding.ToArray();
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
            return new double[0];
        }
    }

    private static List<Message> _ = new List<Message>();
}