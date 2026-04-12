using EMenu.Application.Abstractions.Repositories;
using EMenu.Domain.Entities;
using EMenu.Domain.Enums;
using EMenu.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EMenu.Infrastructure.Repositories
{
    public class ReservationRepository : IReservationRepository
    {
        private readonly AppDbContext _context;

        public ReservationRepository(AppDbContext context)
        {
            _context = context;
        }

        public Reservation? GetByIdWithDetails(int reservationId)
        {
            return BuildDetailsQuery()
                .FirstOrDefault(x => x.ReservationID == reservationId);
        }

        public IReadOnlyList<Reservation> GetByFilter(DateTime? fromDate, DateTime? toDate, int? tableId, int? status)
        {
            var query = BuildDetailsQuery();

            if (fromDate.HasValue)
            {
                query = query.Where(x => x.ReservationTime >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(x => x.ReservationTime <= toDate.Value);
            }

            if (tableId.HasValue)
            {
                query = query.Where(x => x.TableID == tableId.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(x => (int)x.Status == status.Value);
            }

            return query
                .OrderBy(x => x.ReservationTime)
                .ThenBy(x => x.TableID)
                .ToList();
        }

        public bool HasConflict(int tableId, DateTime reservationTime, int? ignoredReservationId = null)
        {
            var query = _context.Reservations
                .Where(x =>
                    x.TableID == tableId &&
                    x.ReservationTime == reservationTime &&
                    x.Status != ReservationStatus.Cancelled);

            if (ignoredReservationId.HasValue)
            {
                query = query.Where(x => x.ReservationID != ignoredReservationId.Value);
            }

            return query.Any();
        }

        public void Add(Reservation reservation)
        {
            _context.Reservations.Add(reservation);
        }

        public void Update(Reservation reservation)
        {
            _context.Reservations.Update(reservation);
        }

        private IQueryable<Reservation> BuildDetailsQuery()
        {
            return _context.Reservations
                .Include(x => x.Customer)
                .Include(x => x.RestaurantTable);
        }
    }
}
