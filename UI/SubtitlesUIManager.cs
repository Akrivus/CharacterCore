using System.Linq;
using TMPro;
using UnityEngine;

public class SubtitlesUIManager : MonoBehaviour
{
    public static SubtitlesUIManager Instance { get; private set; }

    [SerializeField]
    private TextMeshProUGUI _spot;

    [SerializeField]
    private TextMeshProUGUI _subtitle;

    [SerializeField]
    private TextMeshProUGUI _shadow;

    private void Awake()
    {
        Instance = this;
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
}