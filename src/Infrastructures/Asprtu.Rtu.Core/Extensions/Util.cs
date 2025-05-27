using Asprtu.Rtu.Contracts;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Asprtu.Rtu.Extensions;

public static class Util
{
    internal static int TryOccupy<T>(T item)
    {
        try
        {
            int size = Marshal.SizeOf(item);

            if (size <= 64)
                return 1;

            int occupy = (int)Math.Ceiling(size / (double)1);
            return occupy;
        }
        catch (ArgumentException)
        {
            // Handle specific exception related to Marshal.SizeOf
            return 1;
        }
        catch (InvalidOperationException)
        {
            // Handle specific exception if any invalid operation occurs
            return 1;
        }
    }

    public static void GetProtocolList(Action<Type> action, IEnumerable<Assembly>? assemblies = null)
    {
        ArgumentNullException.ThrowIfNull(action);

        Type protocolType = typeof(IProtocol);

        var targetAssemblies = assemblies == null || !assemblies.Any()
            ? AppDomain.CurrentDomain.GetAssemblies().AsEnumerable()
            : assemblies;

        targetAssemblies
            .SelectMany(assembly => assembly.GetTypes())
            .GetProtocolList(action);
    }

    public static void GetProtocolList(this IEnumerable<Type> types, Action<Type> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(types);

        Type protocolType = typeof(IProtocol);

        IEnumerable<Type> protocolTypes = types.Where(type => protocolType.IsAssignableFrom(type)
                           && !type.IsInterface
                           && !type.IsAbstract);

        foreach (var type in protocolTypes)
        {
            action(type);
        }
    }
}