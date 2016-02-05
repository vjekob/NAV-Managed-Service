using System;
using System.Collections.Generic;

namespace ContosoDemoNAV.Process
{
    [Serializable]
    public class State
    {
        private readonly object _lockObjects = new object();
        private readonly object _lockFlags = new object();
        private readonly Dictionary<Type, object> _objects = new Dictionary<Type, object>();
        private readonly Dictionary<string, bool> _flags = new Dictionary<string, bool>();

        private T Get<T>(bool createIfNull) where T : class, new()
        {
            lock (_lockObjects)
            {
                if (!_objects.ContainsKey(typeof (T)))
                {
                    if (!createIfNull)
                        return null;
                    _objects.Add(typeof (T), new T());
                }
                return _objects[typeof (T)] as T;
            }
        }

        public T GetOrCreate<T>() where T : class, new()
        {
            return Get<T>(true);
        }

        public T Get<T>() where T : class, new()
        {
            return Get<T>(false);
        }

        public void Set<T>(T obj) where T: class, new()
        {
            lock (_lockObjects)
            {
                if (_objects.ContainsKey(typeof (T)))
                {
                    _objects.Remove(typeof (T));
                }
                _objects.Add(typeof (T), obj);
            }
        }

        public bool this[string key]
        {
            get
            {
                lock (_lockFlags)
                {
                    if (!_flags.ContainsKey(key))
                        _flags.Add(key, false);
                    return _flags[key];
                }
            }
            set
            {
                lock (_lockFlags)
                {
                    if (!_flags.ContainsKey(key))
                        _flags.Add(key, value);
                    else
                        _flags[key] = value;
                }
            }
        }

        public bool Completed { get; set; }
    }
}