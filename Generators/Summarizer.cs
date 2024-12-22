using System.Threading.Tasks;
using UnityEngine;

public class Summarizer : MonoBehaviour, ISubGenerator
{
    public static string GroundState => _bucket.Get();
    private static MemoryBucket _bucket = new MemoryBucket("#general");

    [SerializeField]
    private TextAsset _prompt;

    private async void Awake()
    {
        await _bucket.Load();
    }

    public async Task<Chat> Generate(Chat chat)
    {
        var memory = await OpenAiIntegration.CompleteAsync(
            _prompt.Format(chat.Log, GroundState), true);
        await _bucket.Add(memory);

        _bucket.Clean();
        await _bucket.Save();

        return chat;
    }
}