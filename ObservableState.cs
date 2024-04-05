using System;
using System.Collections.Generic;
using System.Reflection;
using Blax;
using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;

namespace Blax;

public class ObservableState
{
    private readonly List<Action> _subscribers = new();
    private static readonly ProxyGenerator _generator = new();
    private object? _state;

    public ObservableState()
    {
        ValidateObservableProperties(GetType());
    }

    /// <summary>
    /// Create a new observable object using the given generic type parameter <typeparamref name="T"/>.
    /// <para>Provide an optional <paramref name="serviceProvider"/>, to inject any constructor dependencies.</para>
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T CreateInstance<T>(IServiceProvider? serviceProvider = null) where T : ObservableState, new()
    {
        T instance;
        if (serviceProvider == null) {
            instance = new T();
        } else {
            instance = ActivatorUtilities.CreateInstance<T>(serviceProvider);
        }

        instance.InitializeProxy();
        return (T)instance._state!;
    }
    
    /// <summary>
    /// Create a new observable object using the given type parameter <paramref name="type"/>
    /// <para>Provide an optional <paramref name="serviceProvider"/>, to inject any constructor dependencies.</para>
    /// </summary>
    /// <param name="type"></param>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static object CreateInstance(Type type, IServiceProvider? serviceProvider = null)
    {
        ObservableState? instance;
        if (serviceProvider == null) {
            instance = Activator.CreateInstance(type) as ObservableState;    
        } else {
            instance = ActivatorUtilities.CreateInstance(serviceProvider, type) as ObservableState;
        }
        
        if (instance is null)
            throw new InvalidOperationException("Given type was not a derived type of " + nameof(ObservableState));
        
        instance.InitializeProxy();
        return instance._state!;
    }

    private void InitializeProxy()
    {
        if (_state == null)
        {
            var interceptor = new StateInterceptor();
            _state = _generator.CreateClassProxy(GetType(), interceptor);
        }
    }

    public object? State => _state;

    internal void Subscribe(Action callback)
    {
        if (_subscribers.Contains(callback)) return;

        _subscribers.Add(callback);

        var properties = GetType().GetProperties();

        foreach (var property in properties)
        {
            // Auto-subscribe ObservableList properties
            if (Attribute.IsDefined(property, typeof(ObservableAttribute)) && property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(ObservableList<>))
            {
                var list = property.GetValue(this) as IObservableList;
                if (list != null)
                {
                    SubscribeList(list);
                }
            }

            // Auto-subscribe ObservableDictionary properties
            else if (Attribute.IsDefined(property, typeof(ObservableAttribute)) && property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(ObservableDictionary<,>))
            {
                var dict = property.GetValue(this) as IObservableDictionary;
                if (dict != null)
                {
                    SubscribeDict(dict);
                }
            }
        }
    }

    internal void Unsubscribe(Action callback)
    {
        while (_subscribers.Remove(callback));

        var properties = GetType().GetProperties();

        foreach (var property in properties)
        {
            // Auto-subscribe ObservableList properties
            if (Attribute.IsDefined(property, typeof(ObservableAttribute)) && property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(ObservableList<>))
            {
                var list = property.GetValue(this) as IObservableList;
                if (list != null)
                {
                    UnsubscribeList(list);
                }
            }

            // Auto-subscribe ObservableDictionary properties
            else if (Attribute.IsDefined(property, typeof(ObservableAttribute)) && property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(ObservableDictionary<,>))
            {
                var dict = property.GetValue(this) as IObservableDictionary;
                if (dict != null)
                {
                    UnsubscribeDict(dict);
                }
            }
        }
    }

    internal void NotifySubscribers()
    {
        foreach (var subscriber in _subscribers)
        {
            subscriber.Invoke();
        }
    }

    private void SubscribeList(IObservableList list)
    {
        list.ListChanged += NotifySubscribers;
    }

    private void UnsubscribeList(IObservableList list)
    {
        list.ListChanged -= NotifySubscribers;
    }

    private void SubscribeDict(IObservableDictionary dict)
    {
        dict.DictionaryChanged += NotifySubscribers;
    }

    private void UnsubscribeDict(IObservableDictionary dict)
    {
        dict.DictionaryChanged -= NotifySubscribers;
    }

    private void ValidateObservableProperties(Type type)
    {
        var properties = type.GetProperties();
        foreach (var property in properties)
        {
            var isObservable = Attribute.IsDefined(property, typeof(ObservableAttribute));
            if (isObservable && !(property?.GetMethod?.IsVirtual ?? false))
            {
                var error = $"The property '{property?.Name}' in type '{type.FullName}' is marked as [Observable] but is not virtual. [Observable] properties must be virtual.";
                throw new InvalidOperationException(error);
            }
        }
    }
}