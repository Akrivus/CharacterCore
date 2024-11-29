using OpenAI;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ChatClient : MonoBehaviour, IConfigurable<OpenAIConfigs>
{
    public static string OPENAI_API_KEY;
    public static string OPENAI_API_URI;

    public static string SLOW_MODEL;
    public static string FAST_MODEL;

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
        Debug.Log(messages[messages.Count - 1].Content);
        var model = fast ? FAST_MODEL : SLOW_MODEL;
        var request = await API.ChatEndpoint.GetCompletionAsync(new ChatRequest(messages, model));
        var response = request.FirstChoice;
        if (response.FinishReason != "stop")
            throw new Exception(response.FinishDetails);
        messages.Add(response.Message);

        Debug.Log(response.Message.Content);
        return messages;
    }

    public static async Task<List<Message>> ChatAsync(string prompt, bool fast = false)
    {
        return await ChatAsync(new List<Message> { new Message(Role.System, prompt) }, fast);
    }

    public static async Task<string> CompleteAsync(string prompt, bool fast = false)
    {
        var messages = await ChatAsync(prompt, fast);
        return messages[messages.Count - 1].Content.ToString();
    }
}