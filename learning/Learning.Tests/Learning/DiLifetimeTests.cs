using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

public class DiLifeTimeTest
{
    private readonly ITestOutputHelper _output;
    public DiLifeTimeTest(ITestOutputHelper output)
    {
        _output = output;
    }
    [Fact]
    public void Test_ServiceLifetimes_PrintIds()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddTransient<ITransientId, IdService>();
        services.AddScoped<IScopedId, IdService>();
        services.AddSingleton<ISingletonId, IdService>();

        var provider = services.BuildServiceProvider();

        // Transient
        var transient1 = provider.GetRequiredService<ITransientId>();
        var transient2 = provider.GetRequiredService<ITransientId>();

        _output.WriteLine("========== TRANSIENT ==========");
        _output.WriteLine($"Transient 1: {transient1.Id}");
        _output.WriteLine($"Transient 2: {transient2.Id}");
        _output.WriteLine($"Same instance: {transient1.Id == transient2.Id}");

        // Scoped - Scope 1
        using var scope1 = provider.CreateScope();

        var scoped1 = scope1.ServiceProvider.GetRequiredService<IScopedId>();
        var scoped2 = scope1.ServiceProvider.GetRequiredService<IScopedId>();

        _output.WriteLine("\n========== SCOPED ==========");
        _output.WriteLine($"Scope 1 - Scoped 1: {scoped1.Id}");
        _output.WriteLine($"Scope 1 - Scoped 2: {scoped2.Id}");
        _output.WriteLine($"Same instance: {scoped1.Id == scoped2.Id}");

        // Scoped - Scope 2
        using var scope2 = provider.CreateScope();

        var scoped3 = scope2.ServiceProvider.GetRequiredService<IScopedId>();

        _output.WriteLine($"Scope 2 - Scoped 1: {scoped3.Id}");
        _output.WriteLine($"Different scope: {scoped1.Id != scoped3.Id}");

        // Singleton
        var singleton1 = provider.GetRequiredService<ISingletonId>();
        var singleton2 = scope1.ServiceProvider.GetRequiredService<ISingletonId>();
        var singleton3 = scope2.ServiceProvider.GetRequiredService<ISingletonId>();

        _output.WriteLine("\n========== SINGLETON ==========");
        _output.WriteLine($"Singleton 1: {singleton1.Id}");
        _output.WriteLine($"Singleton 2: {singleton2.Id}");
        _output.WriteLine($"Singleton 3: {singleton3.Id}");
        _output.WriteLine($"Same instance: {singleton1.Id == singleton2.Id && singleton2.Id == singleton3.Id}");
    }
    [Fact]
    public void Singleton_CapturesScopedDependency()
    {
        var services = new ServiceCollection();

        services.AddScoped<ScopedDep>();
        services.AddSingleton<SingletonUsingScoped>();

        using var provider = services.BuildServiceProvider();

        var singleton1 = provider.GetRequiredService<SingletonUsingScoped>();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var scoped1 = scope1.ServiceProvider.GetRequiredService<ScopedDep>();
        var scoped2 = scope2.ServiceProvider.GetRequiredService<ScopedDep>();

        _output.WriteLine($"Singleton Dep: {singleton1.Dep.Id}");
        _output.WriteLine($"Scope 1 Dep: {scoped1.Id}");
        _output.WriteLine($"Scope 2 Dep: {scoped2.Id}");
        _output.WriteLine($"Singleton Dep == Scope 1: {singleton1.Dep.Id == scoped1.Id}");
        _output.WriteLine($"Singleton Dep == Scope 2: {singleton1.Dep.Id == scoped2.Id}");
        Assert.NotEqual(scoped1.Id, scoped2.Id);
        var s1 = scope1.ServiceProvider.GetRequiredService<SingletonUsingScoped>();
        var s2 = scope2.ServiceProvider.GetRequiredService<SingletonUsingScoped>();

        Assert.Same(s1, s2);                      // hai scope nhận cùng một singleton
        Assert.Equal(s1.Dep.Id, s2.Dep.Id);       // ...và cùng một ScopedDep bị "giam"
        Assert.NotEqual(scoped1.Id, s1.Dep.Id);   // ScopedDep bị giam không phải của scope 1
        Assert.NotEqual(scoped2.Id, s2.Dep.Id);   // cũng không phải của scope 2
    }
    [Fact]
    public void ValidateScopes_True_FailsWhenSingletonCapturesScoped()
    {
        using var provider = CaptiveServices().BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true });

        var ex = Assert.Throws<InvalidOperationException>(
            () => provider.GetRequiredService<SingletonUsingScoped>());
        _output.WriteLine(ex.Message);
    }

    [Fact]
    public void ValidateScopes_True_FailsWhenResolvingScopedFromRoot()
    {
        var services = new ServiceCollection();
        services.AddScoped<ScopedDep>();
        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true });

        var ex = Assert.ThrowsAny<Exception>(() => provider.GetRequiredService<ScopedDep>());
        _output.WriteLine($"{ex.GetType().Name}: {ex.Message}");
    }

    private static ServiceCollection CaptiveServices()
    {
        var services = new ServiceCollection();
        services.AddScoped<ScopedDep>();
        services.AddSingleton<SingletonUsingScoped>();
        return services;
    }

    [Fact]
    public void ValidateOnBuild_Only_DoesNotDetectCaptiveDependency()
    {
        var ex = Record.Exception(() =>
            CaptiveServices().BuildServiceProvider(
                new ServiceProviderOptions { ValidateOnBuild = true }));

        Assert.Null(ex);   // đây là phát hiện của bạn: không có lỗi
    }

    [Fact]
    public void ValidateScopesAndOnBuild_Together_FailAtBuildTime()
    {
        var ex = Assert.ThrowsAny<Exception>(() =>
            CaptiveServices().BuildServiceProvider(
                new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true }));

        _output.WriteLine($"{ex.GetType().Name}: {ex.Message}");
        if (ex.InnerException is not null)
            _output.WriteLine($"Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
    }
}