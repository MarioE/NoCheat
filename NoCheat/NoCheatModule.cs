using System;
using JetBrains.Annotations;

namespace NoCheat
{
    /// <summary>
    ///     Specifies a NoCheat module responsible for a certain component of functionality.
    /// </summary>
    [UsedImplicitly]
    public abstract class NoCheatModule : IDisposable
    {
        protected NoCheatModule([NotNull] NoCheatPlugin plugin)
        {
            Plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
        }

        /// <summary>
        ///     Gets the NoCheat plugin.
        /// </summary>
        [NotNull]
        protected NoCheatPlugin Plugin { get; }

        public abstract void Dispose();
        public abstract void Initialize();
    }
}
