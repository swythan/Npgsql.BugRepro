namespace Npgsql.BugRepro.DataAccess;

public class ServiceMetadata(string serviceName)
{
    public Guid Id { get; set; }

    public string ServiceName { get; set; } = serviceName;

    public ICollection<ServiceAttribute> Attributes { get; } = [];
    
    public ICollection<ServiceMetadata> DependsOn { get; } = [];

    public bool HasAttribute(string name, string value)
    {
        return Attributes.Contains(new ServiceAttribute(name, value));
    }
}

public class ServiceAttribute(string name, string value)
{
    public string Name { get; set; } = name;
    public string Value { get; set; } = value;
}
