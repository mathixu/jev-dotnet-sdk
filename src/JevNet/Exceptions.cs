namespace Jev;

/// <summary>Base class for failures reported by the SDK.</summary>
public class TypeSafeException : Exception
{
    /// <summary>Creates an SDK exception.</summary>
    public TypeSafeException(string message)
        : base(message)
    {
    }

    /// <summary>Creates an SDK exception with its underlying cause.</summary>
    public TypeSafeException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>Invalid or incomplete client configuration.</summary>
public sealed class TypeSafeConfigurationException : TypeSafeException
{
    /// <summary>Creates a configuration exception.</summary>
    public TypeSafeConfigurationException(string message)
        : base(message)
    {
    }
}
