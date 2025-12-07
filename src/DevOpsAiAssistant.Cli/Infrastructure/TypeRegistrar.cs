namespace DevOpsAiAssistant.Cli.Infrastructure;

using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

public sealed class TypeRegistrar : ITypeRegistrar
{
    private readonly IServiceProvider _serviceProvider;

    public TypeRegistrar(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ITypeResolver Build() => new TypeResolver(_serviceProvider);

    public void Register(Type service, Type implementation)
    {
        // Not needed - we use the existing DI container
    }

    public void RegisterInstance(Type service, object implementation)
    {
        // Not needed - we use the existing DI container
    }

    public void RegisterLazy(Type service, Func<object> factory)
    {
        // Not needed - we use the existing DI container
    }
}

public sealed class TypeResolver : ITypeResolver
{
    private readonly IServiceProvider _serviceProvider;

    public TypeResolver(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public object? Resolve(Type? type)
    {
        if (type is null)
        {
            return null;
        }

        // Try to get from DI first, otherwise create with DI
        return _serviceProvider.GetService(type)
            ?? ActivatorUtilities.CreateInstance(_serviceProvider, type);
    }
}
