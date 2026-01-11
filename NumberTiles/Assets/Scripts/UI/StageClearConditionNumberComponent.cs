using TMPro;
using UnityEngine;

namespace NTGame
{
    public class StageClearConditionNumberComponent 
        : MonoBehaviour
        , ITileObserver
    {
        public interface IListener
        {
            void OnClickExitGame();
        }

        public Color ActiveColor = new Color(1f, 1f, 1f, 0.9f);
        public Color ClearedColor = new Color(1f, 1f, 1f, 0.15f);

        [Header("UI References (Inspector)")]
        public TextMeshProUGUI TitleTxt;
        [Tooltip("Index 0은 미사용. 1~9를 채워주세요.")]
        public TextMeshProUGUI[] NumberArray;

        IListener _listener;
        public void Open(IListener listener)
        {
            Debug.Assert(NumberArray != null, "NumberArray에 1~9까지 바인딩되어 있어야 합니다.");
            _listener = listener;
            Refresh();
            TileManager.Instance.AddObserver(this);
        }

        public void Close()
        {
            _listener = null;
            TileManager.Instance.RemoveObserver(this);
            var length = NumberArray.Length;
            for (int i = 0; i < length; i++)
            {
                var label = NumberArray[i];
                label.color = ActiveColor;
            }
        }

        void Refresh()
        {
            var tileManager = TileManager.Instance;
            var length = NumberArray.Length;
            for (int i = 0; i < length; i++)
            {
                bool cleared = tileManager.IsDigitCleared(i);
                var label = NumberArray[i];
                label.color = cleared ? ClearedColor : ActiveColor;
            }
        }

        void ITileObserver.OnNotify(TileNotify notify)
        {
            if (notify.Type == TileNotifyType.BoardChanged)
            {
                Refresh();
            }
        }

        #region Button Event Functions
        public void OnClickExitGame()
        {
            _listener.OnClickExitGame();
        }
        #endregion
    }
}

