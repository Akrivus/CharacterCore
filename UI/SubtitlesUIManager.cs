using System.Linq;
using TMPro;
using UnityEngine;

public class SubtitlesUIManager : MonoBehaviour
{
    public static SubtitlesUIManager Instance => _instance ?? (_instance = FindFirstObjectByType<SubtitlesUIManager>());
    private static SubtitlesUIManager _instance;

    [SerializeField]
    private TextMeshProUGUI _title;

    [SerializeField]
    private TextMeshProUGUI _spot;

    [SerializeField]
    private TextMeshProUGUI _subtitle;

    [SerializeField]
    private TextMeshProUGUI _shadow;

    private void Awake()
    {
        _instance = this;
    }

    private void Start()
    {
        ChatManager.Instance.AfterIntermission += OnQueueTaken;
        ChatManager.Instance.OnChatNodeActivated += OnNodeActivated;
        ChatManager.Instance.OnChatQueueEmpty += ClearSubtitle;
    }

    private void OnQueueTaken(Chat chat)
    {
        if (_spot != null)
            _spot.enabled = chat.NewEpisode;
        ClearSubtitle();
        SetChatTitle(chat);
    }

    public void OnNodeActivated(ChatNode node)
    {
        SetSubtitle(node.Actor.Title, node.Say, node.Actor.Color
            .Lighten()
            .Lighten());
    }

    public void SetSubtitle(string name, string text, Color color)
    {
        var content = $"<b><u>{name}</u></b>\n{text.Scrub()}";
        _subtitle.text = content;
        _subtitle.color = color;
        _shadow.text = "<mark=#000000aa>" + content;
    }

    public void SetSubtitle(string name, string text)
    {
        SetSubtitle(name, text, Color.white);
    }

    public void ClearSubtitle()
    {
        _subtitle.text = string.Empty;
        _shadow.text = string.Empty;
    }

    public void SetChatTitle(Chat chat)
    {
        var prompt = chat.Idea.Prompt.Split('\n')[0];
        if (prompt.Length > 160)
            prompt = prompt.Substring(0, 160) + "...";
        if (_title != null)
            _title.text = $"<u><b>{chat.Idea.Source}</b></u> • {prompt}";
    }
}