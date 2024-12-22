using System.Threading.Tasks;

public interface ISubGenerator
{
    Task<Chat> Generate(Chat chat);

    public interface Sync : ISubGenerator
    {
        Task<Chat> ISubGenerator.Generate(Chat chat)
        {
            return Task.FromResult(Generate(chat));
        }

        new Chat Generate(Chat chat);
    }
}