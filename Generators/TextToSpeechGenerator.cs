using Newtonsoft.Json;
using System.Text;
using UnityEngine;
using System.Threading.Tasks;
using System.Net.Http;
using System;
using OpenAI.Audio;
using OpenAI;
using System.Linq;

public class TextToSpeechGenerator : MonoBehaviour, ISubGenerator, IConfigurable<TTSConfigs>
{
    private static string[] OpenAiVoices = new string[] { "alloy", "ash", "ballad", "coral", "echo", "fable", "onyx", "nova", "sage", "shimmer", "verse" };

    private string _googleApiKey;
    private string _openAiApiKey;

    private OpenAIClient _api;

    public void Configure(TTSConfigs config)
    {
        _googleApiKey = config.GoogleApiKey;
        _openAiApiKey = config.OpenAiApiKey;

        if (!string.IsNullOrEmpty(_openAiApiKey))
            _api = new OpenAIClient(new OpenAIAuthentication(_openAiApiKey));
    }

    private void Awake()
    {
        ConfigManager.Instance.RegisterConfig(typeof(TTSConfigs), "tts", (config) => Configure((TTSConfigs) config));
    }

    public async Task<Chat> Generate(Chat chat)
    {
        foreach (var node in chat.Nodes)
            await GenerateTextToSpeech(node);
        return chat;
    }

    private async Task GenerateTextToSpeech(ChatNode node, int delay = 1)
    {
        if (OpenAiVoices.Contains(node.Actor.Voice))
            await GenerateWithOpenAI(node);
        else
            await GenerateWithGoogle(node, delay);
    }

    private async Task GenerateWithGoogle(ChatNode node, int delay = 1)
    {
        var url = $"https://texttospeech.googleapis.com/v1/text:synthesize?key={_googleApiKey}";
        var json = JsonConvert.SerializeObject(new Request(node.Say, node.Actor.Voice));

        var client = new HttpClient();
        var response = await client.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
        if (!response.IsSuccessStatusCode)
        {
            await Task.Delay(delay * 1000);
            await GenerateTextToSpeech(node, ++delay);
        }

        var text = await response.Content.ReadAsStringAsync();
        var output = JsonConvert.DeserializeObject<Output>(text);
        node.AudioData = output.AudioData;
    }

    private async Task GenerateWithOpenAI(ChatNode node)
    {
        var response = await _api.AudioEndpoint.GetSpeechAsync(new SpeechRequest(node.Say,
            voice: new OpenAI.Voice(node.Actor.Voice),
            responseFormat: SpeechResponseFormat.PCM));
        node.Frequency = response.AudioClip.frequency;
        node.AudioClip = response.AudioClip;
    }

    class Request
    {
        public TextInput input { get; set; }
        public AudioConfig audioConfig { get; set; }
        public Voice voice { get; set; }

        public Request(string text, string name)
        {
            audioConfig = new AudioConfig();
            input = new TextInput() { text = text.Scrub() };
            voice = new Voice() { name = name };
        }
    }

    class TextInput
    {
        public string text { get; set; }
    }

    class AudioConfig
    {
        public string audioEncoding { get; set; } = "LINEAR16";
        public float sampleRateHertz { get; set; } = 48000;
        public float volumeGainDb { get; set; } = 1;
        public float pitch { get; set; } = 1;
        public float speakingRate { get; set; } = 1.1f;
    }

    class Voice
    {
        public string name { get; set; } = "en-US-Standard-D";
        public string languageCode { get; set; } = "en-US";
    }

    class Output
    {
        [JsonProperty("audioContent")]
        public string AudioData { get; set; }
    }
}