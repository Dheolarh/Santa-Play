using System.Collections.Generic;
using UnityEngine;

namespace SantaPlay.Piler
{
    public class PilerGiftPool : MonoBehaviour
    {
        [SerializeField] private PilerGift[] _prefabs;
        [SerializeField] private int _initialPoolPerType = 3;

        private readonly Dictionary<int, Queue<PilerGift>> _pools = new();
        private readonly List<PilerGift> _activeGifts = new();

        public IReadOnlyList<PilerGift> ActiveGifts => _activeGifts;

        public void Initialize()
        {
            if (_prefabs == null) return;

            for (var i = 0; i < _prefabs.Length; i++)
            {
                var prefab = _prefabs[i];
                if (prefab == null) continue;

                if (!_pools.ContainsKey(i))
                {
                    _pools[i] = new Queue<PilerGift>(_initialPoolPerType);
                }

                for (var j = 0; j < _initialPoolPerType; j++)
                {
                    var instance = Instantiate(prefab, transform);
                    instance.ResetForPool();
                    _pools[i].Enqueue(instance);
                }
            }
        }

        public PilerGift GetRandomGift()
        {
            if (_prefabs == null || _prefabs.Length == 0) return null;

            var index = Random.Range(0, _prefabs.Length);
            var prefab = _prefabs[index];
            if (prefab == null) return null;

            if (!_pools.TryGetValue(index, out var queue))
            {
                queue = new Queue<PilerGift>();
                _pools[index] = queue;
            }

            PilerGift instance;
            if (queue.Count > 0)
            {
                instance = queue.Dequeue();
            }
            else
            {
                instance = Instantiate(prefab, transform);
            }

            _activeGifts.Add(instance);
            return instance;
        }

        public void ReturnGift(PilerGift gift)
        {
            if (gift == null) return;

            _activeGifts.Remove(gift);
            gift.ResetForPool();
            gift.transform.SetParent(transform, false);

            for (var i = 0; i < _prefabs.Length; i++)
            {
                if (_prefabs[i] != null && _prefabs[i].AssetKey == gift.AssetKey)
                {
                    if (_pools.TryGetValue(i, out var queue))
                    {
                        queue.Enqueue(gift);
                        return;
                    }
                }
            }
        }

        public void ReturnAll()
        {
            for (var i = _activeGifts.Count - 1; i >= 0; i--)
            {
                var gift = _activeGifts[i];
                if (gift != null)
                {
                    ReturnGift(gift);
                }
            }
            _activeGifts.Clear();
        }

        public List<string> GetUniqueAssetKeys()
        {
            var keys = new List<string>();
            if (_prefabs == null) return keys;

            for (var i = 0; i < _prefabs.Length; i++)
            {
                var key = _prefabs[i]?.AssetKey;
                if (!string.IsNullOrEmpty(key) && !keys.Contains(key))
                {
                    keys.Add(key);
                }
            }
            return keys;
        }
    }
}
