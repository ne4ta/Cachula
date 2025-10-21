using Cachula.Core;
using Cachula.Interfaces;
using Cachula.Layers;

namespace Cachula;

/// <summary>
/// Builder for creating a Cachula cache instance with specified layers and stampede protection.
/// </summary>
public sealed class CachulaBuilder
{
    private readonly List<ICacheLayer> _layers = [];
    private IStampedeProtector _stampedeProtector = new InMemorySingleFlightProtector();

    /// <summary>
    /// Adds a cache layer to the Cachula cache.
    /// </summary>
    /// <param name="layer">The cache layer to add.</param>
    /// <returns>The current <see cref="CachulaBuilder"/> instance.</returns>
    public CachulaBuilder WithLayer(ICacheLayer layer)
    {
        _layers.Add(layer);
        return this;
    }

    /// <summary>
    /// Sets the stampede protector for the Cachula cache.
    /// </summary>
    /// <param name="protector">The stampede protector to use.</param>
    /// <returns>The current <see cref="CachulaBuilder"/> instance.</returns>
    public CachulaBuilder WithStampedeProtector(IStampedeProtector protector)
    {
        _stampedeProtector = protector;
        return this;
    }

    /// <summary>
    /// Builds the Cachula cache instance.
    /// </summary>
    /// <returns>The constructed <see cref="ICachulaCache"/> instance.</returns>
    public ICachulaCache Build()
    {
        if (_layers.Count == 0)
        {
            _layers.Add(new NullCacheLayer());
        }

        if (_layers.All(l => l is not NullCacheLayer))
        {
            _layers.Insert(0, new NullCacheLayer());
        }

        var engine = new CachulaEngine(_layers, _stampedeProtector);
        return new CachulaCache(engine);
    }
}
