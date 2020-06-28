using System.Collections.Generic;

namespace Biglab.Collections
{
    public interface IReadyOnlyBiDictionary<TSourceKey, TTargetKey>
    {
        TTargetKey this[TSourceKey key] { get; }
        TSourceKey this[TTargetKey key] { get; }

        int Count { get; }

        IEnumerable<TSourceKey> SourceKeys { get; }
        IEnumerable<TTargetKey> TargetKeys { get; }

        bool Contains(TSourceKey key);
        bool Contains(TTargetKey key);
    }

    public interface IBiDictionary<TSourceKey, TTargetKey> : IReadyOnlyBiDictionary<TSourceKey, TTargetKey>
    {
        new TTargetKey this[TSourceKey key] { get; set; }
        new TSourceKey this[TTargetKey key] { get; set; }

        void Clear();

        bool Remove(TSourceKey key);
        bool Remove(TTargetKey key);
    }

    public class BiDictionary<TSourceKey, TTargetKey> : IBiDictionary<TSourceKey, TTargetKey>
    {
        private readonly Dictionary<TSourceKey, TTargetKey> _sourceToTarget;
        private readonly Dictionary<TTargetKey, TSourceKey> _targetToSource;

        public BiDictionary()
        {
            _sourceToTarget = new Dictionary<TSourceKey, TTargetKey>();
            _targetToSource = new Dictionary<TTargetKey, TSourceKey>();
        }

        public int Count => _sourceToTarget.Count;

        public IEnumerable<TSourceKey> SourceKeys => _sourceToTarget.Keys;

        public IEnumerable<TTargetKey> TargetKeys => _targetToSource.Keys;

        #region Indexer

        public TSourceKey this[TTargetKey key]
        {
            get { return _targetToSource[key]; }

            set
            {
                _targetToSource[key] = value;
                _sourceToTarget[value] = key;
            }
        }

        public TTargetKey this[TSourceKey key]
        {
            get { return _sourceToTarget[key]; }

            set
            {
                _sourceToTarget[key] = value;
                _targetToSource[value] = key;
            }
        }

        #endregion

        #region Remove

        public bool Remove(TTargetKey key)
        {
            if (Contains(key))
            {
                var src = _targetToSource[key];
                _targetToSource.Remove(key);
                _sourceToTarget.Remove(src);

                return true;
            }

            return false;
        }

        public bool Remove(TSourceKey key)
        {
            if (Contains(key))
            {
                var tar = _sourceToTarget[key];
                _sourceToTarget.Remove(key);
                _targetToSource.Remove(tar);

                return true;
            }

            return false;
        }

        #endregion

        #region Contains

        public bool Contains(TTargetKey key)
        {
            return _targetToSource.ContainsKey(key);
        }

        public bool Contains(TSourceKey key)
        {
            return _sourceToTarget.ContainsKey(key);
        }

        #endregion

        public void Clear()
        {
            _sourceToTarget.Clear();
            _targetToSource.Clear();
        }
    }
}