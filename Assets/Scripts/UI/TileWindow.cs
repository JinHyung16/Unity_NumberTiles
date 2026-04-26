using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NTGame
{
    public class TileWindow 
        : BaseWindow
        , ITileObserver
        , TileUIComponent.IListener
        , StageClearConditionNumberComponent.IListener
        , StageItemGroupComponent.IListener
    {
        public interface IListener 
        { 
            void ExitGame();
            void UseItem(ItemType itemType);
        }


        public RectTransform CompRoot;
        public RectTransform TileLineRoot;
        public TileLineRender TileLineRender;
        
        public StageClearConditionNumberComponent ConditionComponent;
        public StageItemGroupComponent StageItemComp;
        public TileStatusAlarmPanel StatusAlarmPanel;

        IListener _listener;
        List<TileUIComponent> _tileList = new List<TileUIComponent>();

        public void Open(IListener listener)
        {
            OpenInternal(() =>
            {
                _listener = listener;
                StatusAlarmPanel.Close();
                ConditionComponent.Open(this);
                StageItemComp.Open(this);
                TileManager.Instance.AddObserver(this);
            });
        }

        protected override void OnClose()
        {
            TileManager.Instance.RemoveObserver(this);
            var poolMgr = PoolManager.Instance;
            for (int i = 0; i < _tileList.Count; i++)
            {
                poolMgr.Release(_tileList[i]);
            }
            _tileList.Clear();
            ConditionComponent.Close();
            StageItemComp.Close();
            StatusAlarmPanel.Close();
        }

        public Vector2 GetCellCenterScreen(int row, int col)
        {
            var tile = GetTile(row, col);
            if (tile != null)
                return tile.GetCenterScreenPoint(GameManager.Instance.UICamera);

            return Vector2.zero;
        }

        public void SetCellActive(int row, int col, bool isActive)
        {
            var tile = GetTile(row, col);
            if (tile != null)
                tile.SetOpen(isActive);
        }

        public void SetCellValue(int row, int col, int value)
        {
            var tile = GetTile(row, col);
            if (tile != null)
            {
                if (tile.IsOpenCell == false)
                    return;
                tile.SetValue(value);
            }
        }

        public void SetCellSelected(int row, int col, bool selected)
        {
            var tile = GetTile(row, col);
            if (tile != null)
                tile.SetSelected(selected);
        }

        public void ShowMatchedLine(int r1, int c1, int r2, int c2)
        {
            if (TileLineRender == null || TileLineRoot == null)
                return;

            var screenA = GetCellCenterScreen(r1, c1);
            var screenB = GetCellCenterScreen(r2, c2);

            var uiCamera = GameManager.Instance.UICamera;
            TileLineRender.ShowLine(screenA, screenB, TileLineRoot, uiCamera);
        }

        public void OnNotify(TileNotify notify)
        {
            if (notify.Type == TileNotifyType.BoardInit)
            {
                ClearAllTiles();
                EnsureTileCount(TileManager.Instance.CellCount);
                SyncAllCellsFromTileManager(notify.Row, notify.Col);
                return;
            }

            if (notify.Type == TileNotifyType.CellOpenChanged)
            {
                SetCellActive(notify.Row, notify.Col, notify.Flag);
                return;
            }

            if (notify.Type == TileNotifyType.CellValueChanged)
            {
                SetCellValue(notify.Row, notify.Col, notify.Value);
                return;
            }

            if (notify.Type == TileNotifyType.CellSelectedChanged)
            {
                SetCellSelected(notify.Row, notify.Col, notify.Flag);
                return;
            }

            if (notify.Type == TileNotifyType.MatchedPair)
            {
                ShowMatchedLine(notify.Row, notify.Col, notify.Row2, notify.Col2);
                return;
            }

            if (notify.Type == TileNotifyType.LineCleared)
            {
                StatusAlarmPanel.ShowLineClearAlarm();
                return;
            }

            if (notify.Type == TileNotifyType.DigitCleared)
            {
                StatusAlarmPanel.ShowTileNumberClearAlarm(notify.Value);
                return;
            }

            if (notify.Type == TileNotifyType.CellCountChanged)
            {
                EnsureTileCount(notify.Value);
                SyncAllCellsFromTileManager(notify.Row, notify.Col);
                return;
            }

            if (notify.Type == TileNotifyType.BoardChanged)
            {
                return;
            }
        }

        TileUIComponent GetTile(int row, int col)
        {
            if (row < 0 || col < 0)
                return null;

            int cols = TileManager.BoardCols;
            int idx = (row * cols) + col;
            if (idx < 0 || idx >= _tileList.Count)
                return null;

            return _tileList[idx];
        }

        void EnsureTileCount(int cellCount)
        {
            var poolMgr = PoolManager.Instance;
            int need = Mathf.Max(0, cellCount);
            int cols = TileManager.BoardCols;

            while (_tileList.Count < need)
            {
                int idx = _tileList.Count;
                int r = idx / cols;
                int c = idx % cols;
                var tileCoord = new TileCoordStruct { Row = r, Col = c };

                var tile = poolMgr.Get(CompRoot);
                tile.Open(tileCoord, this);

                _tileList.Add(tile);
            }
        }

        void SyncAllCellsFromTileManager(int rows, int cols)
        {
            var tileManager = TileManager.Instance;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    bool isOpen = tileManager.IsActiveCell(r, c);
                    SetCellActive(r, c, isOpen);

                    int value = tileManager.GetValue(r, c);
                    SetCellValue(r, c, value);
                }
            }
        }

        void ClearAllTiles()
        {
            for (int i = 0; i < _tileList.Count; i++)
            {
                PoolManager.Instance.Release(_tileList[i]);
            }
            _tileList.Clear();
        }

        void TileUIComponent.IListener.OnClickTile(TileUIComponent comp)
        {
            TileManager.Instance.OnTileClicked(comp.Row, comp.Col);
        }

        void StageClearConditionNumberComponent.IListener.OnClickExitGame()
        {
            GameProgressSaver.SaveCurrent();
            _listener.ExitGame();
        }

        void StageItemGroupComponent.IListener.OnClickUseItem(ItemType itemType)
        {
            _listener.UseItem(itemType);
        }
    }
}

