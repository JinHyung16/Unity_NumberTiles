using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NTGame
{
    public class StageItemGroupComponent
        : MonoBehaviour
        , ITileObserver
    {
        public interface IListener
        {
            void OnClickUseItem(ItemType itemType);
            void OnCancelTargetItem(ItemType itemType);
        }

        public TextMeshProUGUI AddTilesCountTxt;
        public TextMeshProUGUI BreakOneTileCountTxt;
        public TextMeshProUGUI LineSwapCountTxt;
        public TextMeshProUGUI DiagonalClearCountTxt;

        public Toggle BreakOneTileToggle;
        public Toggle LineSwapToggle;
        public Toggle DiagonalClearToggle;

        IListener _listener;
        bool _syncing;

        public void Open(IListener listener)
        {
            _listener = listener;
            RefreshCounts();
            SyncTogglesToPending(ItemType.None);
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
            LineSwapCountTxt.text = tileManager.GetItemCount(ItemType.LineSwap).ToString();
            DiagonalClearCountTxt.text = tileManager.GetItemCount(ItemType.DiagonalClear).ToString();
        }

        void SyncTogglesToPending(ItemType pending)
        {
            _syncing = true;

            BreakOneTileToggle.isOn = (pending == ItemType.BreakOneTile);
            LineSwapToggle.isOn = (pending == ItemType.LineSwap);
            DiagonalClearToggle.isOn = (pending == ItemType.DiagonalClear);

            _syncing = false;
        }

        void ITileObserver.OnNotify(TileNotify notify)
        {
            if (notify.Type == TileNotifyType.BoardInit)
            {
                RefreshCounts();
                SyncTogglesToPending(ItemType.None);
                return;
            }

            if (notify.Type == TileNotifyType.ItemCountChanged)
            {
                RefreshCounts();
                return;
            }

            if (notify.Type == TileNotifyType.PendingTargetChanged)
            {
                SyncTogglesToPending(notify.ItemType);
                return;
            }
        }

        void OnTargetToggleChanged(ItemType itemType, bool isOn)
        {
            if (_syncing)
            {
                return;
            }

            if (_listener == null)
            {
                return;
            }

            if (isOn)
            {
                _listener.OnClickUseItem(itemType);
                return;
            }

            _listener.OnCancelTargetItem(itemType);
        }

        #region Button Event Functions
        public void OnClickAddTileItem()
        {
            _listener?.OnClickUseItem(ItemType.AddTiles);
        }

        public void OnClickBreakOneTileItem(bool isOn)
        {
            OnTargetToggleChanged(ItemType.BreakOneTile, isOn);
        }

        public void OnClickLineSwapItem(bool isOn)
        {
            OnTargetToggleChanged(ItemType.LineSwap, isOn);
        }

        public void OnClickDiagonalClearItem(bool isOn)
        {
            OnTargetToggleChanged(ItemType.DiagonalClear, isOn);
        }
        #endregion
    }
}
