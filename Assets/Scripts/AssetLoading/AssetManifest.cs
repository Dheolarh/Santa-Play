using System;
using System.Collections.Generic;
using UnityEngine;

namespace SantaPlay.AssetLoading
{
    [CreateAssetMenu(fileName = "AssetManifest", menuName = "SantaPlay/Asset Manifest")]
    public class AssetManifest : ScriptableObject
    {
        [Serializable]
        public struct AssetEntry
        {
            public string Key;
            public string RelativeUrl;
        }

        [SerializeField] private string _baseUrl = "https://cdn.santaplay.com/assets/";
        [SerializeField] private AssetEntry[] _entries = Array.Empty<AssetEntry>();

        private Dictionary<string, string> _lookup;

        public string BaseUrl => _baseUrl;

        public bool TryGetUrl(string key, out string fullUrl)
        {
            if (_lookup == null)
            {
                InitializeLookup();
            }

            if (_lookup.TryGetValue(key, out var relativePath))
            {
                fullUrl = CombineUrl(_baseUrl, relativePath);
                return true;
            }

            fullUrl = null;
            return false;
        }

        private void InitializeLookup()
        {
            _lookup = new Dictionary<string, string>(_entries.Length, StringComparer.Ordinal);
            for (var i = 0; i < _entries.Length; i++)
            {
                var entry = _entries[i];
                if (!string.IsNullOrEmpty(entry.Key))
                {
                    _lookup[entry.Key] = entry.RelativeUrl;
                }
            }
        }

        private static string CombineUrl(string baseUri, string relativeUri)
        {
            if (string.IsNullOrEmpty(baseUri)) return relativeUri;
            if (string.IsNullOrEmpty(relativeUri)) return baseUri;

            return baseUri.EndsWith("/") || relativeUri.StartsWith("/")
                ? $"{baseUri.TrimEnd('/')}/{relativeUri.TrimStart('/')}"
                : $"{baseUri}/{relativeUri}";
        }
    }
}
