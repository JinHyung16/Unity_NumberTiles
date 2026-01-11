using TMPro;
using UnityEngine;

namespace NTGame
{
    public class StageItemGroupComponent 
        : MonoBehaviour
        , ITileObserver
    {
        public interface IListener
        {
            void OnClickUseItem(ItemType itemType);
        }

        [Header("UI")]
        public TextMeshProUGUI AddTilesCountTxt;
        public TextMeshProUGUI BreakOneTileCountTxt;

        IListener _listener;

        public void Open(IListener listener)
        {
            _listener = listener;
            RefreshCounts();
            TileManager.Instance.AddObserver(this);
        }

        public void Close()
        {
            _listener = null;
            TileManager.Instance.RemoveObserver(this);
        }

        void RefreshCounts()
        {
            var tileManager = TileManager.Instance;
            AddTilesCountTxt.text = tileManager.GetItemCount(ItemType.AddTiles).ToString();
            BreakOneTileCountTxt.text = tileManager.GetItemCount(ItemType.BreakOneTile).ToString();
        }

        void ITileObserver.OnNotify(TileNotify notify)
        {
            if (notify.Type == TileNotifyType.BoardInit)
            {
                RefreshCounts();
                return;
            }

            if (notify.Type == TileNotifyType.ItemCountChanged)
                RefreshCounts();
        }

        #region Button Event Functions
        public void OnClickAddTileItem()
        {
            _listener.OnClickUseItem(ItemType.AddTiles);
        }

        public void OnClickBreakOneTileItem()
        {
            _listener.OnClickUseItem(ItemType.BreakOneTile);
        }
        #endregion
    }
}

