using Testcontainers.PostgreSql;

namespace Npgsql.BugRepro;

[SetUpFixture]
public class PostgreSqlServerSetup
{
    private static PostgreSqlContainer PostgreSqlContainer { get; }
        = new PostgreSqlBuilder()
            .WithDatabase("service_data")
            .Build();

    public static string ConnectionString { get; private set; }

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        await PostgreSqlContainer.StartAsync();
        ConnectionString = PostgreSqlContainer.GetConnectionString();
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
        if (PostgreSqlContainer is not null)
        {
            await PostgreSqlContainer.StopAsync();
            await PostgreSqlContainer.DisposeAsync();
        }
    }
}
