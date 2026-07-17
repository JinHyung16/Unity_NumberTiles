using System;
using System.Collections.Generic;
using UnityEngine;

namespace NTGame
{
    [CreateAssetMenu(fileName = "GameRuleData", menuName = "NTGame/Game Rule Data", order = 0)]
    public class GameRuleDataSO : ScriptableObject
    {
        [Serializable]
        public class Page
        {
            public Sprite Image;

            [TextArea(2, 5)]
            public string Description;
        }

        [SerializeField] private List<Page> _pages = new List<Page>();

        public int Count => _pages != null ? _pages.Count : 0;

        public Page GetPage(int index)
        {
            if (_pages == null || index < 0 || index >= _pages.Count)
                return null;

            return _pages[index];
        }
    }
}
