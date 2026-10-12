using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace SantaPlay.AssetLoading
{
    public interface IImageAssetLoader
    {
        Task<Sprite> LoadSpriteAsync(string key, CancellationToken ct = default);
        Task PreloadSpritesAsync(System.Collections.Generic.IEnumerable<string> keys, CancellationToken ct = default);
        void Release(string key);
        void UnloadUnused();
    }
}
