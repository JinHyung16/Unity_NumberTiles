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
        public TextMeshProUGUI AddTilesCountTxt;
        public TextMeshProUGUI BreakOneTileCountTxt;
        public TextMeshProUGUI LineSwapCountTxt;
        public TextMeshProUGUI DiagonalClearCountTxt;

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
            if (AddTilesCountTxt != null)
                AddTilesCountTxt.text = tileManager.GetItemCount(ItemType.AddTiles).ToString();
            if (BreakOneTileCountTxt != null)
                BreakOneTileCountTxt.text = tileManager.GetItemCount(ItemType.BreakOneTile).ToString();
            if (LineSwapCountTxt != null)
                LineSwapCountTxt.text = tileManager.GetItemCount(ItemType.LineSwap).ToString();
            if (DiagonalClearCountTxt != null)
                DiagonalClearCountTxt.text = tileManager.GetItemCount(ItemType.DiagonalClear).ToString();
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

        public void OnClickLineSwapItem()
        {
            _listener.OnClickUseItem(ItemType.LineSwap);
        }

        public void OnClickDiagonalClearItem()
        {
            _listener.OnClickUseItem(ItemType.DiagonalClear);
        }
        #endregion
    }
}

