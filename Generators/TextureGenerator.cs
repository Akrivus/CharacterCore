using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class TextureGenerator : MonoBehaviour, ISubGenerator
{
    [SerializeField]
    private TextAsset _prompt;

    public async Task<Chat> Generate(Chat chat)
    {
        var prompt = await ChatClient.CompleteAsync(_prompt.Format(chat.Topic), false);
        var request = await ChatClient.API.ImagesEndPoint.GenerateImageAsync(
            new OpenAI.Images.ImageGenerationRequest(prompt, model: "dall-e-3", size: "1792x1024"));
        var image = request.First();
        
        chat.Texture = image.Texture;

        return chat;
    }
}
