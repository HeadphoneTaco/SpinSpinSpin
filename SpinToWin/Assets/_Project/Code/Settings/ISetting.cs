namespace _Project.Code.Settings {
    /// <summary>
    ///     A single readable / writable preference (volume, a toggle, etc.). UI controls bind
    ///     to this abstraction rather than to a concrete manager, so the control doesn't care
    ///     <em>who</em> owns the value or how it's stored — the Dependency-Inversion "D" in SOLID.
    /// </summary>
    /// <typeparam name="T">The value type, e.g. <c>float</c> for a volume or <c>bool</c> for a toggle.</typeparam>
    public interface ISetting<T> {
        /// <summary>The current value.</summary>
        T Value { get; }

        /// <summary>Writes a new value (clamping/persistence is the implementer's job).</summary>
        void SetValue(T value);
    }
}
