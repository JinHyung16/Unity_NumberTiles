using System;
using System.Collections.Generic;
using UnityEngine;

namespace NTGame
{
    /// <summary>
    /// 게임 규칙 안내(GameRuleWindow)에서 한 페이지씩 보여줄 이미지 + 설명 텍스트 데이터.
    /// 페이지 순서는 Pages 리스트의 순서를 그대로 따른다.
    /// </summary>
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
