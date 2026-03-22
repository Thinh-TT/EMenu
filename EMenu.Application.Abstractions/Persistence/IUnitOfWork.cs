namespace EMenu.Application.Abstractions.Persistence
{
    public interface IUnitOfWork
    {
        ITransaction BeginTransaction();
        int SaveChanges();
    }
}
