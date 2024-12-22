using UnityEngine;

public class UnlockChat : MonoBehaviour, ISubGenerator.Sync
{
    public Chat Generate(Chat chat)
    {
        GetComponent<ChatGenerator>()
            .DisableLock = true;
        return chat;
    }
}
