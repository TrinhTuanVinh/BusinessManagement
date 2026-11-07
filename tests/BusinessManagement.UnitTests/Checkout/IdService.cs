namespace BusinessManagement.UnitTests.Checkout;
public interface ITransientId { Guid Id { get; } }
public interface IScopedId    { Guid Id { get; } }
public interface ISingletonId { Guid Id { get; } }

public class IdService : ITransientId, IScopedId, ISingletonId
{
    public Guid Id { get; } = Guid.NewGuid();   // dùng NewGuid, KHÔNG dùng CreateVersion7
}