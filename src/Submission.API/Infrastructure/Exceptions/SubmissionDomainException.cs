namespace Inked.Submission.API.Infrastructure.Exceptions;

/// <summary>
///     Exception type for app exceptions
/// </summary>
public class SubmissionDomainException : Exception
{
    public SubmissionDomainException()
    {
    }

    public SubmissionDomainException(string message)
        : base(message)
    {
    }

    public SubmissionDomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
