using System;
using System.Collections.Generic;

namespace CylinderShooter.Core
{
    /// <summary>Generic reuse pool. Grows on demand; <paramref name="prewarm"/> items are created up front.</summary>
    public sealed class ObjectPool<T> where T : class
    {
        readonly Func<T> _create;
        readonly Action<T> _onGet;
        readonly Action<T> _onRelease;
        readonly Stack<T> _free = new Stack<T>();

        public int CountAll { get; private set; }
        public int CountInactive => _free.Count;
        public int CountActive => CountAll - CountInactive;

        public ObjectPool(Func<T> create, Action<T> onGet = null, Action<T> onRelease = null, int prewarm = 0)
        {
            _create = create ?? throw new ArgumentNullException(nameof(create));
            _onGet = onGet;
            _onRelease = onRelease;
            for (int i = 0; i < prewarm; i++) Release(CreateNew());
        }

        public T Get()
        {
            T item = _free.Count > 0 ? _free.Pop() : CreateNew();
            _onGet?.Invoke(item);
            return item;
        }

        public void Release(T item)
        {
            _onRelease?.Invoke(item);
            _free.Push(item);
        }

        T CreateNew()
        {
            CountAll++;
            return _create();
        }
    }
}
