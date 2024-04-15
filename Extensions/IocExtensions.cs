using System.Reflection;
using Microsoft.AspNetCore.Components;
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
            sc.Add(new ServiceDescriptor(ob, factory: GetFactoryFunc(ob), serviceLifetime));
        }
    }

    public static Func<IServiceProvider, object> GetFactoryFunc(Type objectType)
    {
        return Factory;

        object Factory(IServiceProvider svcProvider)
        {
            var instance = ObservableState.CreateInstance(objectType, svcProvider);

            // get all [Inject] properties
            var propertiesWithInjectAttribute = instance.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetCustomAttribute<InjectAttribute>() != null).ToList();

            // if any, then set those [Inject] properties from the serviceProvider
            if (propertiesWithInjectAttribute.Any()) {
                foreach (var property in propertiesWithInjectAttribute) {
                    if (!property.CanWrite) continue;

                    var propertyType = property.PropertyType;
                    var serviceInstance = svcProvider.GetRequiredService(propertyType);
                    property.SetValue(instance, serviceInstance);
                }
            }

            return instance;
        }
    }
}