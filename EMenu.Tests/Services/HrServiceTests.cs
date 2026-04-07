using EMenu.Application.Abstractions.Persistence;
using EMenu.Application.Abstractions.Repositories;
using EMenu.Application.Services;
using EMenu.Domain.Entities;
using Moq;

namespace EMenu.Tests.Services;

public class HrServiceTests
{
    [Fact]
    public void CheckIn_NoRecordForToday_AddsTimekeepingAndSaves()
    {
        var timekeepingRepository = new Mock<ITimekeepingRepository>();
        var wageRepository = new Mock<IWageRepository>();
        var staffRepository = new Mock<IStaffRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var now = new DateTime(2026, 4, 7, 8, 0, 0);
        Timekeeping? added = null;

        staffRepository.Setup(x => x.GetById(5))
            .Returns(new Staff { StaffID = 5, StaffName = "Staff A" });
        timekeepingRepository.Setup(x => x.GetByStaffAndDate(5, DateOnly.FromDateTime(now)))
            .Returns((Timekeeping?)null);
        timekeepingRepository.Setup(x => x.Add(It.IsAny<Timekeeping>()))
            .Callback<Timekeeping>(x => added = x);

        var service = new HrService(
            timekeepingRepository.Object,
            wageRepository.Object,
            staffRepository.Object,
            unitOfWork.Object);

        var result = service.CheckIn(5, now);

        Assert.NotNull(result);
        Assert.NotNull(added);
        Assert.Equal(5, added!.StaffID);
        Assert.Equal(DateOnly.FromDateTime(now), added.Date);
        Assert.Equal(now, added.CheckIn);
        unitOfWork.Verify(x => x.SaveChanges(), Times.Once);
    }

    [Fact]
    public void CheckOut_BeforeCheckIn_Throws()
    {
        var timekeepingRepository = new Mock<ITimekeepingRepository>();
        var wageRepository = new Mock<IWageRepository>();
        var staffRepository = new Mock<IStaffRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var checkIn = new DateTime(2026, 4, 7, 10, 0, 0);
        var checkOut = new DateTime(2026, 4, 7, 9, 0, 0);

        staffRepository.Setup(x => x.GetById(5))
            .Returns(new Staff { StaffID = 5, StaffName = "Staff A" });
        timekeepingRepository.Setup(x => x.GetByStaffAndDate(5, DateOnly.FromDateTime(checkOut)))
            .Returns(new Timekeeping
            {
                Id = 1,
                StaffID = 5,
                Date = DateOnly.FromDateTime(checkOut),
                CheckIn = checkIn
            });

        var service = new HrService(
            timekeepingRepository.Object,
            wageRepository.Object,
            staffRepository.Object,
            unitOfWork.Object);

        var ex = Assert.Throws<InvalidOperationException>(() => service.CheckOut(5, checkOut));

        Assert.Equal("Check-out time cannot be earlier than check-in.", ex.Message);
        unitOfWork.Verify(x => x.SaveChanges(), Times.Never);
    }

    [Fact]
    public void UpsertWage_WhenWageExists_UpdatesExistingProfile()
    {
        var timekeepingRepository = new Mock<ITimekeepingRepository>();
        var wageRepository = new Mock<IWageRepository>();
        var staffRepository = new Mock<IStaffRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var existing = new Wage
        {
            Id = 9,
            StaffID = 5,
            BaseSalary = 1000m,
            HourlyRate = 100m
        };

        staffRepository.Setup(x => x.GetById(5))
            .Returns(new Staff { StaffID = 5, StaffName = "Staff A" });
        wageRepository.Setup(x => x.GetByStaffId(5))
            .Returns(existing);

        var service = new HrService(
            timekeepingRepository.Object,
            wageRepository.Object,
            staffRepository.Object,
            unitOfWork.Object);

        var result = service.UpsertWage(5, 2000m, 150m);

        Assert.Equal(9, result.Id);
        Assert.Equal(2000m, result.BaseSalary);
        Assert.Equal(150m, result.HourlyRate);
        wageRepository.Verify(x => x.Update(existing), Times.Once);
        wageRepository.Verify(x => x.Add(It.IsAny<Wage>()), Times.Never);
        unitOfWork.Verify(x => x.SaveChanges(), Times.Once);
    }

    [Fact]
    public void GetMonthlyWageReport_ReturnsHoursAndEstimatedWageByStaff()
    {
        var timekeepingRepository = new Mock<ITimekeepingRepository>();
        var wageRepository = new Mock<IWageRepository>();
        var staffRepository = new Mock<IStaffRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        staffRepository.Setup(x => x.GetAllWithUser())
            .Returns(new List<Staff>
            {
                new() { StaffID = 1, StaffName = "Alice" },
                new() { StaffID = 2, StaffName = "Bob" }
            });
        timekeepingRepository.Setup(x => x.GetByMonth(2026, 4))
            .Returns(new List<Timekeeping>
            {
                new()
                {
                    StaffID = 1,
                    Date = new DateOnly(2026, 4, 1),
                    CheckIn = new DateTime(2026, 4, 1, 8, 0, 0),
                    CheckOut = new DateTime(2026, 4, 1, 16, 0, 0)
                },
                new()
                {
                    StaffID = 1,
                    Date = new DateOnly(2026, 4, 2),
                    CheckIn = new DateTime(2026, 4, 2, 8, 30, 0),
                    CheckOut = new DateTime(2026, 4, 2, 15, 0, 0)
                }
            });
        wageRepository.Setup(x => x.GetAllWithStaff())
            .Returns(new List<Wage>
            {
                new()
                {
                    Id = 1,
                    StaffID = 1,
                    BaseSalary = 1000m,
                    HourlyRate = 100m
                }
            });

        var service = new HrService(
            timekeepingRepository.Object,
            wageRepository.Object,
            staffRepository.Object,
            unitOfWork.Object);

        var report = service.GetMonthlyWageReport(2026, 4);
        var alice = report.Single(x => x.StaffId == 1);
        var bob = report.Single(x => x.StaffId == 2);

        Assert.Equal(2, alice.WorkDays);
        Assert.Equal(14.5m, alice.TotalHours);
        Assert.Equal(2450m, alice.EstimatedWage);

        Assert.Equal(0, bob.WorkDays);
        Assert.Equal(0m, bob.TotalHours);
        Assert.Equal(0m, bob.EstimatedWage);
    }
}
