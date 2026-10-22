namespace Staybnb.Services;

public class OperationResult
{
    public bool Succeeded { get; private init; }
    public string Message { get; private init; } = string.Empty;

    public static OperationResult Success(string message) => new() { Succeeded = true, Message = message };
    public static OperationResult Fail(string message) => new() { Message = message };
}