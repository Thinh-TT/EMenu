using EMenu.Domain.Entities;

namespace EMenu.Application.Abstractions.Repositories
{
    public interface IReservationRepository
    {
        Reservation? GetByIdWithDetails(int reservationId);
        IReadOnlyList<Reservation> GetByFilter(DateTime? fromDate, DateTime? toDate, int? tableId, int? status);
        bool HasConflict(int tableId, DateTime reservationTime, int? ignoredReservationId = null);
        void Add(Reservation reservation);
        void Update(Reservation reservation);
    }
}
