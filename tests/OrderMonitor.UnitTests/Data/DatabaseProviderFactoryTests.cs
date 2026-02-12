using FluentAssertions;
using OrderMonitor.Infrastructure.Data;

namespace OrderMonitor.UnitTests.Data;

public class DatabaseProviderFactoryTests
{
    [Theory]
    [InlineData("SqlServer", DatabaseProvider.SqlServer)]
    [InlineData("sqlserver", DatabaseProvider.SqlServer)]
    [InlineData("SQLSERVER", DatabaseProvider.SqlServer)]
    [InlineData("MySql", DatabaseProvider.MySql)]
    [InlineData("mysql", DatabaseProvider.MySql)]
    [InlineData("PostgreSql", DatabaseProvider.PostgreSql)]
    [InlineData("postgresql", DatabaseProvider.PostgreSql)]
    [InlineData("postgres", DatabaseProvider.PostgreSql)]
    public void ParseProvider_ValidProvider_ReturnsCorrectEnum(string input, DatabaseProvider expected)
    {
        var result = DatabaseProviderFactory.ParseProvider(input);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ParseProvider_NullOrEmpty_ThrowsArgumentException(string? input)
    {
        var act = () => DatabaseProviderFactory.ParseProvider(input!);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("provider");
    }

    [Theory]
    [InlineData("Oracle")]
    [InlineData("SQLite")]
    [InlineData("MongoDB")]
    public void ParseProvider_InvalidProvider_ThrowsWithAllowedValues(string input)
    {
        var act = () => DatabaseProviderFactory.ParseProvider(input);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("provider")
            .WithMessage($"*'{input}'*Allowed values*");
    }

    [Fact]
    public void ParseProvider_WhitespaceAroundValid_TrimsAndParses()
    {
        var result = DatabaseProviderFactory.ParseProvider("  SqlServer  ");
        result.Should().Be(DatabaseProvider.SqlServer);
    }
}
