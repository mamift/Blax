using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Blax.Extensions;

public static class IocExtensions
{
    /// <summary>
    /// Given the specified <paramref name="assembly"/>, add all types that derive from <see cref="ObservableState"/> to the current <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="sc"></param>
    /// <param name="assembly"></param>
    /// <param name="serviceLifetime"></param>
    public static void AddObservables(this IServiceCollection sc, Assembly assembly, 
        ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
    {
        var observables = assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(ObservableState))).ToList();

        foreach (var ob in observables) {
            sc.Add(new ServiceDescriptor(ob, factory: _ => ObservableState.CreateInstance(ob), serviceLifetime));
        }
    }
}