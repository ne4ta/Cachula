namespace Cachula.Interfaces;

/// <summary>
/// Defines cache stampede protection: ensures only one loader runs per key, others await the same task.
/// </summary>
public interface IStampedeProtector
{
    /// <summary>
    /// Runs the loader for the specified key, ensuring that only one loader executes concurrently for the same key.
    /// Other concurrent calls for the same key will await the same task and receive the same result.
    /// </summary>
    /// <typeparam name="T">Type of the loaded value.</typeparam>
    /// <param name="key">Cache key to protect.</param>
    /// <param name="loader">A factory function to load the value if it is not already being loaded.</param>
    /// <returns>A task representing the asynchronous operation, containing the loaded value.</returns>
    Task<T> RunAsync<T>(string key, Func<Task<T>> loader);

    /// <summary>
    /// Runs the loader for a collection of keys, ensuring that only one loader executes concurrently for each key.
    /// For keys that are already being loaded by other threads, returns the existing tasks for those keys.
    /// Only keys not currently being loaded will be passed to the loader function.
    /// </summary>
    /// <typeparam name="T">Type of the loaded values.</typeparam>
    /// <param name="keys">Collection of cache keys to protect.</param>
    /// <param name="loader">A factory function that takes a collection of keys and loads their values in batch.</param>
    /// <returns>
    /// A dictionary mapping each requested key to a task representing the asynchronous operation for that key.
    /// For keys already being loaded, the existing task is returned; for new keys, a new task is created.
    /// </returns>
    IDictionary<string, Task<T>> RunManyAsync<T>(
        IEnumerable<string> keys, Func<IEnumerable<string>, Task<IDictionary<string, T>>> loader);
}
