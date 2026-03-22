using EMenu.Application.Abstractions.Persistence;
using EMenu.Application.Abstractions.Repositories;
using EMenu.Application.Services;
using EMenu.Domain.Entities;
using Moq;

namespace EMenu.Tests.Services;

public class PasswordServiceTests
{
    [Fact]
    public void HashAndVerify_ReturnsTrueForCorrectPassword()
    {
        var service = new PasswordService(
            Mock.Of<IUserRepository>(),
            Mock.Of<IUnitOfWork>());

        var hashed = service.HashPassword("Pass1234");

        Assert.True(service.IsHashed(hashed));
        Assert.True(service.VerifyPassword("Pass1234", hashed));
        Assert.False(service.VerifyPassword("Wrong1234", hashed));
    }

    [Fact]
    public void IsHashed_DetectsLegacyPlainText()
    {
        var service = new PasswordService(
            Mock.Of<IUserRepository>(),
            Mock.Of<IUnitOfWork>());

        Assert.False(service.IsHashed("plain-password"));
        Assert.True(service.IsHashed(service.HashPassword("Pass1234")));
    }

    [Fact]
    public void EnsureValidPassword_WhenMissingNumber_Throws()
    {
        var service = new PasswordService(
            Mock.Of<IUserRepository>(),
            Mock.Of<IUnitOfWork>());

        var ex = Assert.Throws<ArgumentException>(() =>
            service.EnsureValidPassword("Password", "Password", required: true));

        Assert.Equal("Password must include at least 1 number.", ex.Message);
    }

    [Fact]
    public void MigrateLegacyPasswords_HashesLegacyAndReturnsCount()
    {
        var userRepository = new Mock<IUserRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var users = new List<User>
        {
            new() { UserID = 1, UserName = "a", Password = "oldpass1" },
            new() { UserID = 2, UserName = "b", Password = "oldpass2" }
        };

        userRepository.Setup(x => x.GetLegacyPasswordUsers()).Returns(users);

        var service = new PasswordService(userRepository.Object, unitOfWork.Object);

        var migrated = service.MigrateLegacyPasswords();

        Assert.Equal(2, migrated);
        Assert.All(users, user => Assert.True(service.IsHashed(user.Password)));
        unitOfWork.Verify(x => x.SaveChanges(), Times.Once);
    }
}
