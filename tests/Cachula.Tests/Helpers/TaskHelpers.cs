namespace Cachula.Tests.Helpers;

public static class TaskHelpers
{
    /// <summary>
    /// Creates a completed task with a default value of type T.
    /// </summary>
    public static Task<T?> NullValue<T>() where T : struct
        => Task.FromResult<T?>(null);

    /// <summary>
    /// Creates a completed task with a null reference of type T.
    /// </summary>
    public static Task<T?> NullRef<T>() where T : class
        => Task.FromResult<T?>(null);
}
