using EMenu.Application.Abstractions.DTOs;
using EMenu.Application.Abstractions.Persistence;
using EMenu.Application.Abstractions.Repositories;
using EMenu.Domain.Entities;

namespace EMenu.Application.Services
{
    public class HrService
    {
        private readonly ITimekeepingRepository _timekeepingRepository;
        private readonly IWageRepository _wageRepository;
        private readonly IStaffRepository _staffRepository;
        private readonly IUnitOfWork _unitOfWork;

        public HrService(
            ITimekeepingRepository timekeepingRepository,
            IWageRepository wageRepository,
            IStaffRepository staffRepository,
            IUnitOfWork unitOfWork)
        {
            _timekeepingRepository = timekeepingRepository;
            _wageRepository = wageRepository;
            _staffRepository = staffRepository;
            _unitOfWork = unitOfWork;
        }

        public Timekeeping CheckIn(int staffId, DateTime? now = null)
        {
            var checkInTime = now ?? DateTime.Now;
            var date = DateOnly.FromDateTime(checkInTime);

            EnsureStaffExists(staffId);

            var existing = _timekeepingRepository.GetByStaffAndDate(staffId, date);

            if (existing != null)
            {
                throw new InvalidOperationException("Staff has already checked in for today.");
            }

            var record = new Timekeeping
            {
                StaffID = staffId,
                Date = date,
                CheckIn = checkInTime
            };

            _timekeepingRepository.Add(record);
            _unitOfWork.SaveChanges();

            return record;
        }

        public Timekeeping CheckOut(int staffId, DateTime? now = null)
        {
            var checkOutTime = now ?? DateTime.Now;
            var date = DateOnly.FromDateTime(checkOutTime);

            EnsureStaffExists(staffId);

            var record = _timekeepingRepository.GetByStaffAndDate(staffId, date);

            if (record == null)
            {
                throw new InvalidOperationException("No check-in found for today.");
            }

            if (record.CheckOut.HasValue)
            {
                throw new InvalidOperationException("Staff has already checked out for today.");
            }

            if (checkOutTime < record.CheckIn)
            {
                throw new InvalidOperationException("Check-out time cannot be earlier than check-in.");
            }

            record.CheckOut = checkOutTime;

            _timekeepingRepository.Update(record);
            _unitOfWork.SaveChanges();

            return record;
        }

        public IReadOnlyList<Timekeeping> GetTimekeepingByStaffAndMonth(int staffId, int year, int month)
        {
            ValidateYearMonth(year, month);
            EnsureStaffExists(staffId);

            return _timekeepingRepository.GetByStaffAndMonth(staffId, year, month);
        }

        public IReadOnlyList<Timekeeping> GetTimekeepingByMonth(int year, int month)
        {
            ValidateYearMonth(year, month);

            return _timekeepingRepository.GetByMonth(year, month);
        }

        public Wage? GetWageByStaffId(int staffId)
        {
            EnsureStaffExists(staffId);

            return _wageRepository.GetByStaffId(staffId);
        }

        public IReadOnlyList<Wage> GetAllWages()
        {
            return _wageRepository.GetAllWithStaff();
        }

        public Wage UpsertWage(int staffId, decimal baseSalary, decimal hourlyRate)
        {
            EnsureStaffExists(staffId);

            if (baseSalary < 0)
            {
                throw new InvalidOperationException("Base salary cannot be negative.");
            }

            if (hourlyRate < 0)
            {
                throw new InvalidOperationException("Hourly rate cannot be negative.");
            }

            var existing = _wageRepository.GetByStaffId(staffId);

            if (existing == null)
            {
                var wage = new Wage
                {
                    StaffID = staffId,
                    BaseSalary = baseSalary,
                    HourlyRate = hourlyRate
                };

                _wageRepository.Add(wage);
                _unitOfWork.SaveChanges();

                return wage;
            }

            existing.BaseSalary = baseSalary;
            existing.HourlyRate = hourlyRate;

            _wageRepository.Update(existing);
            _unitOfWork.SaveChanges();

            return existing;
        }

        public Staff? GetStaffByUserId(int userId)
        {
            return _staffRepository.GetByUserId(userId);
        }

        public IReadOnlyList<MonthlyWageReportDto> GetMonthlyWageReport(int year, int month)
        {
            ValidateYearMonth(year, month);

            var records = _timekeepingRepository.GetByMonth(year, month);
            var wages = _wageRepository
                .GetAllWithStaff()
                .ToDictionary(x => x.StaffID);
            var staffs = _staffRepository.GetAllWithUser();

            var results = new List<MonthlyWageReportDto>(staffs.Count);

            foreach (var staff in staffs)
            {
                var staffRecords = records
                    .Where(x => x.StaffID == staff.StaffID)
                    .ToList();

                var totalHours = staffRecords.Sum(CalculateWorkedHours);
                var workDays = staffRecords.Count(x => x.CheckOut.HasValue);

                wages.TryGetValue(staff.StaffID, out var wageProfile);

                var baseSalary = wageProfile?.BaseSalary ?? 0m;
                var hourlyRate = wageProfile?.HourlyRate ?? 0m;

                results.Add(new MonthlyWageReportDto
                {
                    StaffId = staff.StaffID,
                    StaffName = staff.StaffName,
                    Year = year,
                    Month = month,
                    WorkDays = workDays,
                    TotalHours = totalHours,
                    BaseSalary = baseSalary,
                    HourlyRate = hourlyRate,
                    EstimatedWage = baseSalary + hourlyRate * totalHours
                });
            }

            return results
                .OrderBy(x => x.StaffName)
                .ToList();
        }

        public MonthlyWageReportDto GetStaffMonthlyWageReport(int staffId, int year, int month)
        {
            ValidateYearMonth(year, month);

            var staff = EnsureStaffExists(staffId);
            var records = _timekeepingRepository.GetByStaffAndMonth(staffId, year, month);
            var wage = _wageRepository.GetByStaffId(staffId);

            var totalHours = records.Sum(CalculateWorkedHours);
            var workDays = records.Count(x => x.CheckOut.HasValue);
            var baseSalary = wage?.BaseSalary ?? 0m;
            var hourlyRate = wage?.HourlyRate ?? 0m;

            return new MonthlyWageReportDto
            {
                StaffId = staff.StaffID,
                StaffName = staff.StaffName,
                Year = year,
                Month = month,
                WorkDays = workDays,
                TotalHours = totalHours,
                BaseSalary = baseSalary,
                HourlyRate = hourlyRate,
                EstimatedWage = baseSalary + hourlyRate * totalHours
            };
        }

        private Staff EnsureStaffExists(int staffId)
        {
            var staff = _staffRepository.GetById(staffId);

            if (staff == null)
            {
                throw new InvalidOperationException("Staff not found.");
            }

            return staff;
        }

        private static decimal CalculateWorkedHours(Timekeeping record)
        {
            if (!record.CheckOut.HasValue)
            {
                return 0m;
            }

            var duration = record.CheckOut.Value - record.CheckIn;

            if (duration <= TimeSpan.Zero)
            {
                return 0m;
            }

            return Math.Round((decimal)duration.TotalHours, 2);
        }

        private static void ValidateYearMonth(int year, int month)
        {
            if (year < 2000 || year > 9999)
            {
                throw new InvalidOperationException("Year is out of range.");
            }

            if (month < 1 || month > 12)
            {
                throw new InvalidOperationException("Month must be between 1 and 12.");
            }
        }
    }
}
