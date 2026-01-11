using System;
using System.Collections.Generic;
using UnityEngine;

namespace NTGame
{
    [Serializable]
    public struct TextColorData
    {
        public int Key;
        public Color Color;
    }

    public class TileTextColor : MonoBehaviour
    {
        public List<TextColorData> ColorDataList = new List<TextColorData>();

        Dictionary<int, Color> _textColorDict = new Dictionary<int, Color>(16);

        public bool TryGetTextColor(int key, out Color color)
        {
            return _textColorDict.TryGetValue(key, out color);
        }

        void Awake()
        {
            RebuildCache();
        }

        void RebuildCache()
        {
            _textColorDict.Clear();

            for (int i = 0; i < ColorDataList.Count; i++)
            {
                var item = ColorDataList[i];
                _textColorDict[item.Key] = item.Color;
            }
        }
    }
}

