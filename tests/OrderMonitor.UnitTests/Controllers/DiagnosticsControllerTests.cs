using System.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using OrderMonitor.Api.Controllers;
using OrderMonitor.Core.Interfaces;

namespace OrderMonitor.UnitTests.Controllers;

/// <summary>
/// Unit tests for DiagnosticsController.
/// Note: Tests for database operations that use Dapper are covered in integration tests.
/// Unit tests focus on input validation and constructor behavior.
/// </summary>
public class DiagnosticsControllerTests
{
    private readonly Mock<IDbConnectionFactory> _connectionFactoryMock;
    private readonly Mock<IDbConnection> _connectionMock;
    private readonly Mock<ILogger<DiagnosticsController>> _loggerMock;
    private readonly DiagnosticsController _controller;

    public DiagnosticsControllerTests()
    {
        _connectionFactoryMock = new Mock<IDbConnectionFactory>();
        _connectionMock = new Mock<IDbConnection>();
        _loggerMock = new Mock<ILogger<DiagnosticsController>>();

        _connectionFactoryMock
            .Setup(f => f.CreateConnection())
            .Returns(_connectionMock.Object);

        _controller = new DiagnosticsController(_connectionFactoryMock.Object, _loggerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidDependencies_CreatesInstance()
    {
        // Act
        var controller = new DiagnosticsController(_connectionFactoryMock.Object, _loggerMock.Object);

        // Assert
        controller.Should().NotBeNull();
    }

    #endregion

    #region RunQuery Input Validation Tests

    [Fact]
    public async Task RunQuery_WithNullRequest_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.RunQuery(null!);

        // Assert
        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task RunQuery_WithEmptySql_ReturnsBadRequest()
    {
        // Arrange
        var request = new QueryRequest { Sql = "" };

        // Act
        var result = await _controller.RunQuery(request);

        // Assert
        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task RunQuery_WithWhitespaceSql_ReturnsBadRequest()
    {
        // Arrange
        var request = new QueryRequest { Sql = "   " };

        // Act
        var result = await _controller.RunQuery(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task RunQuery_WithNullSql_ReturnsBadRequest()
    {
        // Arrange
        var request = new QueryRequest { Sql = null! };

        // Act
        var result = await _controller.RunQuery(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Theory]
    [InlineData("INSERT INTO Orders VALUES (1, 'test')")]
    [InlineData("insert into Orders values (1, 'test')")]
    [InlineData("INSERT INTO Orders (Id) VALUES (1)")]
    public async Task RunQuery_WithInsertStatement_ReturnsBadRequest(string sql)
    {
        // Arrange
        var request = new QueryRequest { Sql = sql };

        // Act
        var result = await _controller.RunQuery(request);

        // Assert
        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().NotBeNull();
    }

    [Theory]
    [InlineData("UPDATE Orders SET Status = 1")]
    [InlineData("update orders set status = 1")]
    [InlineData("UPDATE Orders SET Status = 1 WHERE Id = 1")]
    public async Task RunQuery_WithUpdateStatement_ReturnsBadRequest(string sql)
    {
        // Arrange
        var request = new QueryRequest { Sql = sql };

        // Act
        var result = await _controller.RunQuery(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Theory]
    [InlineData("DELETE FROM Orders WHERE Id = 1")]
    [InlineData("delete from orders")]
    [InlineData("DELETE Orders WHERE Id = 1")]
    public async Task RunQuery_WithDeleteStatement_ReturnsBadRequest(string sql)
    {
        // Arrange
        var request = new QueryRequest { Sql = sql };

        // Act
        var result = await _controller.RunQuery(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Theory]
    [InlineData("DROP TABLE Orders")]
    [InlineData("drop table orders")]
    [InlineData("DROP DATABASE TestDB")]
    public async Task RunQuery_WithDropStatement_ReturnsBadRequest(string sql)
    {
        // Arrange
        var request = new QueryRequest { Sql = sql };

        // Act
        var result = await _controller.RunQuery(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Theory]
    [InlineData("TRUNCATE TABLE Orders")]
    [InlineData("truncate table orders")]
    public async Task RunQuery_WithTruncateStatement_ReturnsBadRequest(string sql)
    {
        // Arrange
        var request = new QueryRequest { Sql = sql };

        // Act
        var result = await _controller.RunQuery(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Theory]
    [InlineData("ALTER TABLE Orders ADD Column1 INT")]
    [InlineData("alter table orders drop column Col1")]
    public async Task RunQuery_WithAlterStatement_ReturnsBadRequest(string sql)
    {
        // Arrange
        var request = new QueryRequest { Sql = sql };

        // Act
        var result = await _controller.RunQuery(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Theory]
    [InlineData("CREATE TABLE NewTable (Id INT)")]
    [InlineData("create table newtable (id int)")]
    public async Task RunQuery_WithCreateStatement_ReturnsBadRequest(string sql)
    {
        // Arrange
        var request = new QueryRequest { Sql = sql };

        // Act
        var result = await _controller.RunQuery(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Theory]
    [InlineData("EXEC sp_executesql @sql")]
    [InlineData("exec DeleteAllOrders")]
    [InlineData("EXECUTE sp_help")]
    public async Task RunQuery_WithExecStatement_ReturnsBadRequest(string sql)
    {
        // Arrange
        var request = new QueryRequest { Sql = sql };

        // Act
        var result = await _controller.RunQuery(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region QueryRequest Model Tests

    [Fact]
    public void QueryRequest_DefaultSql_IsEmptyString()
    {
        // Arrange & Act
        var request = new QueryRequest();

        // Assert
        request.Sql.Should().BeEmpty();
    }

    [Fact]
    public void QueryRequest_WithSql_StoresValue()
    {
        // Arrange & Act
        var request = new QueryRequest { Sql = "SELECT * FROM Orders" };

        // Assert
        request.Sql.Should().Be("SELECT * FROM Orders");
    }

    #endregion
}
