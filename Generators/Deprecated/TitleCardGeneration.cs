using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class TitleCardGeneration : MonoBehaviour, ISubGenerator
{
    [SerializeField]
    private TextAsset _prompt;

    public async Task<Chat> Generate(Chat chat)
    {
        var text = await LLM.CompleteAsync(_prompt.Format(chat.Log, chat.Characters, chat.Idea.Text), true);
        chat.Title = text.Find("Title");
        chat.Synopsis = text.Find("Synopsis");
        return chat;
    }
}