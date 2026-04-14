namespace AiClinic.Application.Commands;

// Command Pattern
public interface ICommand<TResult>
{
    Task<TResult> ExecuteAsync();
}

public interface ICommandHandler<TCommand, TResult> where TCommand : ICommand<TResult>
{
    Task<TResult> HandleAsync(TCommand command);
}
