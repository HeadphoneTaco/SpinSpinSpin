using System;

namespace _Project.Code.Settings {
    /// <summary>
    ///     An <see cref="ISetting{T}" /> backed by getter/setter delegates. Lets a manager expose
    ///     one of its existing fields as a setting in a single line, instead of writing a bespoke
    ///     adapter class per field:
    ///     <code>
    ///     public ISetting&lt;float&gt; MusicVolume =&gt;
    ///         _music ??= new DelegateSetting&lt;float&gt;(() =&gt; musicVolume, SetMusicVolume);
    ///     </code>
    /// </summary>
    public sealed class DelegateSetting<T> : ISetting<T> {
        private readonly Func<T> _get;
        private readonly Action<T> _set;

        public DelegateSetting(Func<T> get, Action<T> set) {
            _get = get ?? throw new ArgumentNullException(nameof(get));
            _set = set ?? throw new ArgumentNullException(nameof(set));
        }

        public T Value => _get();

        public void SetValue(T value) => _set(value);
    }
}
