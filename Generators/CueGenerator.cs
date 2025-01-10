using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class CueGenerator : MonoBehaviour, ISubGenerator
{
    [SerializeField]
    private TextAsset _prompt;

    [SerializeField, TextArea]
    private string _override;

    public async Task<Chat> Generate(Chat chat)
    {
        var prompt = _prompt.Format(chat.Topic, chat.Context, _override);
        var content = await OpenAiIntegration.CompleteAsync(prompt, true);
        
        var lines = content.Split('\n').Where(x => x.StartsWith("- ")).Select(x => x.Substring(2));
        chat.Cues = lines.ToArray();

        chat.EndingTrigger = content.Find("Ending Trigger");

        return chat;
    }
}