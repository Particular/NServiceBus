namespace NServiceBus.AcceptanceTesting.Support;

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

public class RunSettings : IEnumerable<KeyValuePair<string, object>>
{
    public TimeSpan? TestExecutionTimeout
    {
        get => TryGet("TestExecutionTimeout", out TimeSpan timeout) ? timeout : null;
        set
        {
            if (value.HasValue)
            {
                Set("TestExecutionTimeout", value.Value);
            }
            else
            {
                Remove("TestExecutionTimeout");
            }
        }
    }

    public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => stash.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Retrieves the specified type from the settings.
    /// </summary>
    /// <typeparam name="T">The type to retrieve.</typeparam>
    /// <returns>The type instance.</returns>
    public T Get<T>() => Get<T>(typeof(T).FullName!);

    /// <summary>
    /// Retrieves the specified type from the settings
    /// </summary>
    /// <typeparam name="T">The type to retrieve.</typeparam>
    /// <param name="key">The key to retrieve the type.</param>
    /// <returns>The type instance.</returns>
    public T Get<T>(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return TryGet(key, out T? result) ? result : ThrowKeyNotFoundException(key);

        [DoesNotReturn]
        static T ThrowKeyNotFoundException(string key) => throw new KeyNotFoundException("No item found in behavior settings with key: " + key);
    }

    /// <summary>
    /// Gets the requested extension, a new one will be created if needed.
    /// </summary>
    public T GetOrCreate<T>() where T : class, new()
    {
        if (TryGet(out T? value))
        {
            return value;
        }

        var newInstance = new T();
        Set(newInstance);
        return newInstance;
    }

    /// <summary>
    /// Tries to retrieves the specified type from the settings.
    /// </summary>
    /// <typeparam name="T">The type to retrieve.</typeparam>
    /// <param name="result">The type instance.</param>
    /// <returns><code>true</code> if found, otherwise <code>false</code>.</returns>
    public bool TryGet<T>([NotNullWhen(true)] out T? result) => TryGet(typeof(T).FullName!, out result);

    /// <summary>
    /// Stores the type instance in the settings.
    /// </summary>
    /// <typeparam name="T">The type to store.</typeparam>
    /// <param name="t">The instance type to store.</param>
    public void Set<T>(T t) => Set(typeof(T).FullName!, t);

    /// <summary>
    /// Removes the instance type from the settings.
    /// </summary>
    /// <typeparam name="T">The type to remove.</typeparam>
    public void Remove<T>() => Remove(typeof(T).FullName!);

    /// <summary>
    /// Removes the instance type from the settings.
    /// </summary>
    /// <param name="key">The key of the value being removed.</param>
    public void Remove(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _ = stash.TryRemove(key, out _);
    }

    /// <summary>
    /// Stores the passed instance in the settings.
    /// </summary>
    public void Set<T>(string key, T t)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(t);

        stash[key] = t;
    }

    /// <summary>
    /// Tries to retrieves the specified type from the settings.
    /// </summary>
    /// <typeparam name="T">The type to retrieve.</typeparam>
    /// <param name="key">The key of the value being looked up.</param>
    /// <param name="result">The type instance.</param>
    /// <returns><code>true</code> if found, otherwise <code>false</code>.</returns>
    public bool TryGet<T>(string key, [NotNullWhen(true)] out T? result)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (stash.TryGetValue(key, out var value))
        {
            result = (T)value;
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Merges the passed settings into this one.
    /// </summary>
    /// <param name="settings">The source settings.</param>
    public void Merge(RunSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        foreach (var kvp in settings.stash)
        {
            stash[kvp.Key] = kvp.Value;
        }
    }

    readonly ConcurrentDictionary<string, object> stash = new();
}