using Cachula;
using Cachula.Core;
using Cachula.Interfaces;
using Cachula.Layers;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Dependency injection extensions for Cachula.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds Cachula services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add Cachula services to.</param>
    /// <returns>The updated service collection with Cachula services registered.</returns>
    /// <remarks>
    /// Fun fact: the method name is a pun — "put on cachula" sounds like "put on a shirt", because "кашуля" (koszula) means "shirt" in Belarusian and Polish.
    /// </remarks>
    public static IServiceCollection PutOnCachula(this IServiceCollection services)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.AddSingleton<ICachulaCache, CachulaCache>();
        services.AddSingleton<ICachulaEngine, CachulaEngine>();
        services.AddSingleton<IStampedeProtector, InMemorySingleFlightProtector>();
        return services;
    }

    /// <summary>
    /// Adds a memory cache layer to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add the memory cache layer to.</param>
    /// <returns>The updated service collection with the memory cache layer registered.</returns>
    public static IServiceCollection WithMemoryCache(this IServiceCollection services)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.AddSingleton<ICacheLayer, CachulaMemoryCache>();
        return services;
    }

    /// <summary>
    /// Adds a distributed cache layer to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add the distributed cache layer to.</param>
    /// <param name="distributedCache">The distributed cache instance to register.</param>
    /// <returns>The updated service collection with the distributed cache layer registered.</returns>
    public static IServiceCollection WithDistributedCache(
        this IServiceCollection services, ICachulaDistributedCache distributedCache)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (distributedCache == null)
        {
            throw new ArgumentNullException(nameof(distributedCache));
        }

        if (services.All(sd => sd.ServiceType != typeof(ICacheLayer)))
        {
            services.AddSingleton<ICacheLayer, NullCacheLayer>();
        }

        services.AddSingleton<ICacheLayer>(distributedCache);
        return services;
    }
}
