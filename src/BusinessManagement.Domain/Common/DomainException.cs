namespace BusinessManagement.Domain.Common;
// Common/DomainException.cs
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

