using System;

namespace EdgePcConfigurationApp.Exceptions;

public class DuplicateConnectionException : Exception
{
    public DuplicateConnectionException() : base() { }
    
    public DuplicateConnectionException(string message) : base(message) { }
    
    public DuplicateConnectionException(string message, Exception innerException) : base(message, innerException) { }
}