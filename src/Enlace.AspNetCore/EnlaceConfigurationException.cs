namespace Enlace.AspNetCore;

/// <summary>
/// Thrown at startup when Enlace cannot resolve an OpenAPI document. This should fail
/// loudly and clearly — never fall back to a silent empty canvas.
/// </summary>
public sealed class EnlaceConfigurationException : Exception
{
    /// <summary>Creates the exception with the given message.</summary>
    public EnlaceConfigurationException(string message) : base(message)
    {
    }

    /// <summary>Creates the exception with the given message and inner exception.</summary>
    public EnlaceConfigurationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
