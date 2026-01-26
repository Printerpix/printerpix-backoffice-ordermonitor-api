using FluentAssertions;
using Moq;
using OrderMonitor.Core.Interfaces;
using OrderMonitor.Core.Models;
using OrderMonitor.Infrastructure.Data;

namespace OrderMonitor.UnitTests.Data;

public class OrderRepositoryTests
{
    [Fact]
    public void Constructor_WithNullConnectionFactory_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new OrderRepository(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("connectionFactory");
    }

    [Fact]
    public void Constructor_WithValidConnectionFactory_CreatesInstance()
    {
        // Arrange
        var connectionFactoryMock = new Mock<IDbConnectionFactory>();

        // Act
        var repository = new OrderRepository(connectionFactoryMock.Object);

        // Assert
        repository.Should().NotBeNull();
    }
}
