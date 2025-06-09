using Asprtu.Rtu.Contracts;
using Microsoft.Extensions.DependencyInjection;
using System.Linq.Expressions;
using System.Reflection;

namespace Asprtu.Rtu;

public class LibraryCapacities<T> : ILibraryCapacities<T>
    where T : class, IContracts
{
    private static readonly Func<IServiceProvider, T> _factory = CreateFactory();

    private static Func<IServiceProvider, T> CreateFactory()
    {
        var ctors = typeof(T).GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        var chosen = ctors
            .FirstOrDefault(ci => ci.GetCustomAttribute<ActivatorUtilitiesConstructorAttribute>() != null)
            ?? ctors.OrderByDescending(ci => ci.GetParameters().Length).First();

        var spParam = Expression.Parameter(typeof(IServiceProvider), "sp");
        var args = chosen.GetParameters()
            .Select(p => (Expression)Expression.Call(
                    typeof(ServiceProviderServiceExtensions),
                    nameof(ServiceProviderServiceExtensions.GetService),
                    [p.ParameterType],
                    spParam
                ) ?? Expression.Constant(null, p.ParameterType))
            .ToArray();

        var newExp = Expression.New(chosen, args);
        return Expression.Lambda<Func<IServiceProvider, T>>(newExp, spParam)
                         .Compile();
    }

    public string Name { get; } = typeof(T).Name;
    IContracts ILibraryCapacities.Contracts => Contracts;
    public T Contracts { get; }

    // 如果你还需要外部传入实例的重载，可保留这个
    public LibraryCapacities(T instance)
        => Contracts = instance ?? throw new ArgumentNullException(nameof(instance));

    public LibraryCapacities(IServiceProvider provider)
        => Contracts = _factory(provider);
}