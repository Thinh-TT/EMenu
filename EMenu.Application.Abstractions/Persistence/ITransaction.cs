namespace EMenu.Application.Abstractions.Persistence
{
    public interface ITransaction : IDisposable
    {
        void Commit();
    }
}
