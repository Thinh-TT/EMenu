using EMenu.Application.Abstractions.Persistence;
using EMenu.Application.Abstractions.Repositories;
using EMenu.Domain.Entities;
using EMenu.Domain.Enums;

namespace EMenu.Application.Services
{
    public class ReservationService
    {
        private readonly IReservationRepository _reservationRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly ITableRepository _tableRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ReservationService(
            IReservationRepository reservationRepository,
            ICustomerRepository customerRepository,
            ITableRepository tableRepository,
            IUnitOfWork unitOfWork)
        {
            _reservationRepository = reservationRepository;
            _customerRepository = customerRepository;
            _tableRepository = tableRepository;
            _unitOfWork = unitOfWork;
        }

        public IReadOnlyList<Reservation> GetReservations(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int? tableId = null,
            int? status = null)
        {
            if (fromDate.HasValue && toDate.HasValue && fromDate.Value > toDate.Value)
            {
                throw new InvalidOperationException("From date cannot be later than to date.");
            }

            if (tableId.HasValue)
            {
                EnsureTableExists(tableId.Value);
            }

            if (status.HasValue && !Enum.IsDefined(typeof(ReservationStatus), status.Value))
            {
                throw new InvalidOperationException("Invalid reservation status.");
            }

            return _reservationRepository.GetByFilter(fromDate, toDate, tableId, status);
        }

        public IReadOnlyList<RestaurantTable> GetTables()
        {
            return _tableRepository.GetAll();
        }

        public Reservation CreateReservation(
            int customerId,
            int tableId,
            DateTime reservationTime,
            int numberOfGuests)
        {
            EnsureCustomerExists(customerId);
            var table = EnsureTableExists(tableId);
            var normalizedReservationTime = NormalizeReservationTime(reservationTime);

            ValidateReservationValues(table, normalizedReservationTime, numberOfGuests);
            EnsureNoConflict(tableId, normalizedReservationTime);

            var reservation = new Reservation
            {
                CustomerID = customerId,
                TableID = tableId,
                ReservationTime = normalizedReservationTime,
                NumberOfGuests = numberOfGuests,
                Status = ReservationStatus.Pending
            };

            _reservationRepository.Add(reservation);
            _unitOfWork.SaveChanges();

            return reservation;
        }

        public Reservation CreateReservationForCustomer(
            string customerName,
            string? phone,
            string? email,
            int tableId,
            DateTime reservationTime,
            int numberOfGuests)
        {
            if (string.IsNullOrWhiteSpace(customerName))
            {
                throw new InvalidOperationException("Customer name is required.");
            }

            var table = EnsureTableExists(tableId);
            var normalizedReservationTime = NormalizeReservationTime(reservationTime);

            ValidateReservationValues(table, normalizedReservationTime, numberOfGuests);
            EnsureNoConflict(tableId, normalizedReservationTime);

            using var transaction = _unitOfWork.BeginTransaction();

            var customer = new Customer
            {
                Name = customerName.Trim(),
                Phone = NormalizeOptionalValue(phone),
                Email = NormalizeOptionalValue(email),
                CreatedAt = DateTime.Now
            };

            _customerRepository.Add(customer);
            _unitOfWork.SaveChanges();

            var reservation = new Reservation
            {
                CustomerID = customer.CustomerID,
                TableID = tableId,
                ReservationTime = normalizedReservationTime,
                NumberOfGuests = numberOfGuests,
                Status = ReservationStatus.Pending
            };

            _reservationRepository.Add(reservation);
            _unitOfWork.SaveChanges();
            transaction.Commit();

            return reservation;
        }

        public Reservation ConfirmReservation(int reservationId)
        {
            var reservation = EnsureReservationExists(reservationId);

            if (reservation.Status == ReservationStatus.Cancelled)
            {
                throw new InvalidOperationException("Cancelled reservation cannot be confirmed.");
            }

            if (reservation.Status == ReservationStatus.Confirmed)
            {
                return reservation;
            }

            EnsureNoConflict(reservation.TableID, reservation.ReservationTime, reservation.ReservationID);

            reservation.Status = ReservationStatus.Confirmed;
            _reservationRepository.Update(reservation);
            _unitOfWork.SaveChanges();

            return reservation;
        }

        public Reservation CancelReservation(int reservationId)
        {
            var reservation = EnsureReservationExists(reservationId);

            if (reservation.Status == ReservationStatus.Cancelled)
            {
                return reservation;
            }

            reservation.Status = ReservationStatus.Cancelled;
            _reservationRepository.Update(reservation);
            _unitOfWork.SaveChanges();

            return reservation;
        }

        public bool HasConflict(int tableId, DateTime reservationTime, int? ignoredReservationId = null)
        {
            EnsureTableExists(tableId);
            var normalizedReservationTime = NormalizeReservationTime(reservationTime);

            return _reservationRepository.HasConflict(tableId, normalizedReservationTime, ignoredReservationId);
        }

        private void EnsureNoConflict(int tableId, DateTime reservationTime, int? ignoredReservationId = null)
        {
            if (_reservationRepository.HasConflict(tableId, reservationTime, ignoredReservationId))
            {
                throw new InvalidOperationException("This table already has a reservation at the selected time.");
            }
        }

        private Reservation EnsureReservationExists(int reservationId)
        {
            var reservation = _reservationRepository.GetByIdWithDetails(reservationId);

            if (reservation == null)
            {
                throw new InvalidOperationException("Reservation not found.");
            }

            return reservation;
        }

        private void EnsureCustomerExists(int customerId)
        {
            if (!_customerRepository.Exists(customerId))
            {
                throw new InvalidOperationException("Customer not found.");
            }
        }

        private RestaurantTable EnsureTableExists(int tableId)
        {
            var table = _tableRepository.GetById(tableId);

            if (table == null)
            {
                throw new InvalidOperationException("Table not found.");
            }

            return table;
        }

        private static void ValidateReservationValues(
            RestaurantTable table,
            DateTime reservationTime,
            int numberOfGuests)
        {
            if (reservationTime < DateTime.Now)
            {
                throw new InvalidOperationException("Reservation time must be in the future.");
            }

            if (numberOfGuests <= 0)
            {
                throw new InvalidOperationException("Number of guests must be greater than zero.");
            }

            if (numberOfGuests > table.Capacity)
            {
                throw new InvalidOperationException("Number of guests exceeds table capacity.");
            }
        }

        private static DateTime NormalizeReservationTime(DateTime reservationTime)
        {
            return new DateTime(
                reservationTime.Year,
                reservationTime.Month,
                reservationTime.Day,
                reservationTime.Hour,
                reservationTime.Minute,
                0);
        }

        private static string? NormalizeOptionalValue(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }
    }
}
