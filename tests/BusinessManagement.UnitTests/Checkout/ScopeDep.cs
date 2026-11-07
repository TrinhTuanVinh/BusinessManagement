namespace BusinessManagement.UnitTests.Checkout;

public class ScopedDep { public Guid Id { get; } = Guid.NewGuid(); }
public class SingletonUsingScoped
{
    public ScopedDep Dep { get; }
    public SingletonUsingScoped(ScopedDep dep) => Dep = dep;
}
