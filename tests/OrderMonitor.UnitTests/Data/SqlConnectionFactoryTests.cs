using FluentAssertions;
using Microsoft.Data.SqlClient;
using OrderMonitor.Infrastructure.Data;

namespace OrderMonitor.UnitTests.Data;

public class SqlConnectionFactoryTests
{
    [Fact]
    public void Constructor_WithNullConnectionString_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => new SqlConnectionFactory(null!);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("connectionString");
    }

    [Fact]
    public void Constructor_WithEmptyConnectionString_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => new SqlConnectionFactory(string.Empty);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("connectionString");
    }

    [Fact]
    public void Constructor_WithWhitespaceConnectionString_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => new SqlConnectionFactory("   ");
        act.Should().Throw<ArgumentException>()
            .WithParameterName("connectionString");
    }

    [Fact]
    public void Constructor_WithValidConnectionString_CreatesInstance()
    {
        // Arrange
        const string connectionString = "Server=localhost;Database=TestDb;";

        // Act
        var factory = new SqlConnectionFactory(connectionString);

        // Assert
        factory.Should().NotBeNull();
    }

    [Fact]
    public void CreateConnection_ReturnsSqlConnection()
    {
        // Arrange
        const string connectionString = "Server=localhost;Database=TestDb;";
        var factory = new SqlConnectionFactory(connectionString);

        // Act
        var connection = factory.CreateConnection();

        // Assert
        connection.Should().NotBeNull();
        connection.Should().BeOfType<SqlConnection>();
    }

    [Fact]
    public void CreateConnection_ReturnsNewConnectionEachTime()
    {
        // Arrange
        const string connectionString = "Server=localhost;Database=TestDb;";
        var factory = new SqlConnectionFactory(connectionString);

        // Act
        var connection1 = factory.CreateConnection();
        var connection2 = factory.CreateConnection();

        // Assert
        connection1.Should().NotBeSameAs(connection2);

        // Cleanup
        connection1.Dispose();
        connection2.Dispose();
    }
}
