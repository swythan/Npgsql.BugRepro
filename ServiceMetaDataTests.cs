using System.Linq.Expressions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql.BugRepro.DataAccess;

namespace Npgsql.BugRepro;

public class ServiceMetaDataTests
{
    private ILoggerFactory _logger;
    
    private ServiceDbContext _dbContext;

    [SetUp]
    public void Setup()
    {
        _logger = LoggerFactory.Create(builder => builder
            .SetMinimumLevel(LogLevel.Information)
            .AddNUnit());

        var options = new DbContextOptionsBuilder<ServiceDbContext>()
            .UseNpgsql(PostgreSqlServerSetup.ConnectionString)
            .UseLoggerFactory(_logger)
            .EnableSensitiveDataLogging()
            .Options;

        _dbContext = new ServiceDbContext(options);

        _dbContext.Database.EnsureDeleted();
        _dbContext.Database.EnsureCreated();

        AddServices();
    }

    private void AddServices()
    {
        var service1 = AddService("Service1", "Foo");
        var service2 = AddService("Service2", "Bar");
        var service3 = AddService("Service3", "Baz");

        service1.DependsOn.Add(service2);
        service2.DependsOn.Add(service3);

        _dbContext.SaveChanges();
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
        _logger.Dispose();
    }

    private ServiceMetadata AddService(string name, string serviceType)
    {
        var service = new ServiceMetadata(name);
        service.Attributes.Add(new ServiceAttribute("ServiceType", serviceType));

        _dbContext.Services.Add(service);

        return service;
    }

    private static Expression<Func<ServiceMetadata, bool>> HasAttributeValue(string name, string value) =>
        x => x.Attributes.Any(a => a.Name == name && a.Value == value);

    [Test]
    public void NoFilter_SplitQuery_ReturnsAll()
    {
        var services = _dbContext.Services
            .Include(x => x.DependsOn)
            .AsSplitQuery()
            .ToList();

        services.Should().HaveCount(3);
    }

    [Test]
    public void NameFilter_SplitQuery_ReturnsSingle()
    {
        var services = _dbContext.Services
            .Where(x => x.ServiceName == "Service2")
            .Include(x => x.DependsOn)
            .AsSplitQuery()
            .ToList();

        services.Should().HaveCount(1);
        services.Single().ServiceName.Should().Be("Service2");
    }

    [Test]
    public void TypeFilter_NoInclude_ReturnsSingle()
    {
        var services = _dbContext.Services
            .Where(HasAttributeValue("ServiceType", "Bar"))
            .ToList();

        services.Should().HaveCount(1);
        services.Single().ServiceName.Should().Be("Service2");
    }

    [Test]
    public void TypeFilter_SingleQuery_ReturnsSingle()
    {
        var services = _dbContext.Services
            .Where(HasAttributeValue("ServiceType", "Bar"))
            .Include(x => x.DependsOn)
            .AsSingleQuery()
            .ToList();

        services.Should().HaveCount(1);
        services.Single().ServiceName.Should().Be("Service2");
    }

    [Test]
    public void TypeFilter_SplitQuery_ReturnsSingle()
    {
        // This fails with the message: a column definition list is required for functions returning "record"
        var services = _dbContext.Services
            .Where(HasAttributeValue("ServiceType", "Bar"))
            .Include(x => x.DependsOn)
            .AsSplitQuery()
            .ToList();

        services.Should().HaveCount(1);
        services.Single().ServiceName.Should().Be("Service2");
    }
}
