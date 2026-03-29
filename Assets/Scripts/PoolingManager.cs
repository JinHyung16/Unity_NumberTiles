using System.Collections.Generic;
using UnityEngine;

namespace NTGame
{
    public class PoolManager : SceneSingleton<PoolManager>
    {
        static readonly string TileUIPrefabResourcePath = "TileUIComponent";
        const int PrewarmCount = 100;

        Transform _poolRoot;
        TileUIComponent _prefab;

        readonly Stack<TileUIComponent> _poolStk = new Stack<TileUIComponent>(200);

        public void InitPool()
        {
            EnsurePrefabLoaded();
            EnsurePoolRoot();

            int need = Mathf.Max(0, PrewarmCount - _poolStk.Count);
            for (int i = 0; i < need; i++)
                _poolStk.Push(CreateTileUIComponent());
        }

        public TileUIComponent Get(Transform parent)
        {
            var item = _poolStk.Count > 0 ? _poolStk.Pop() : CreateTileUIComponent();
            item.gameObject.SetActive(true);
            item.transform.SetParent(parent, false);
            return item;
        }

        public void Release(TileUIComponent item)
        {
            if (item == null) 
                return;

            item.Clear();
            item.SetParent(_poolRoot);
            _poolStk.Push(item);
        }

        public void Clear()
        {
            if (_poolStk == null)
                return;

            foreach(var item in _poolStk)
            {
                item.Clear();
                Destroy(item);
            }
            _poolStk.Clear();
        }

        TileUIComponent CreateTileUIComponent()
        {
            var instance = Instantiate(_prefab, _poolRoot);
            instance.SetActive(false);
            return instance;
        }

        void EnsurePoolRoot()
        {
            if (_poolRoot != null)
                return;

            var go = new GameObject("PoolRoot");
            go.transform.SetParent(transform, false);
            _poolRoot = go.transform;
        }

        void EnsurePrefabLoaded()
        {
            if (_prefab != null)
                return;

            _prefab = Resources.Load<TileUIComponent>(TileUIPrefabResourcePath);
            Debug.Assert(_prefab != null, $"[PoolManager] Resources.Load 실패: \"{TileUIPrefabResourcePath}\" (Assets/Resources 아래에 TileUIComponent 프리팹이 있어야 합니다)");
        }
    }
}

