namespace EgitimPlatform.BuildingBlocks.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException()
        : base("The requested resource was not found.")
    {
    }

    public NotFoundException(string message)
        : base(message)
    {
    }

    public NotFoundException(string entityName, object key)
        : base($"Entity '{entityName}' with key '{key}' was not found.")
    {
    }

    public NotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
