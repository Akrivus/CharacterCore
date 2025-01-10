using uLipSync;
using UnityEngine;

public class LipsController : AutoActor, ISubChats
{
    [SerializeField]
    private MeshRenderer lipsRenderer;

    [SerializeField]
    private uLipSyncTexture lipSync;

    private Sentiment sentiment;

    private void Update()
    {
        UpdateLips();
    }

    private void UpdateLips()
    {
        lipSync.textures[0].texture = ActorController.Sentiment.Lips;
    }

    public void Initialize(Chat chat)
    {
        var context = chat.Actors.Get(Actor);
        if (context != null)
            sentiment = context.Sentiment;
        UpdateLips();
    }
}
