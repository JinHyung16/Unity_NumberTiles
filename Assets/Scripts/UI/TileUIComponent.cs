using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NTGame
{
    public class TileUIComponent : MonoBehaviour
    {
        public interface IListener
        {
            void OnClickTile(TileUIComponent comp);
        }

        public RectTransform RectTrans;
        public Button TileBtn;

        public TextMeshProUGUI LabelTxt;

        public Image BGImg;
        public Image OutlineImg;

        public TileTextColor TextColor;

        public Color TileBoardBGColor = new Color(0.12f, 0.14f, 0.22f, 1f);
        public Color ActiveTileColor = new Color(0.44f, 0.45f, 0.80f, 0.95f);
        public Color RemovedTileColor = new Color(0.65f, 0.65f, 0.65f, 0.95f);
        public Color RemovedTextColor = new Color(0.65f, 0.65f, 0.65f, 0.9f);
        public Color SelectedTileColor = new Color(1f, 0.92f, 0.3f, 1f);
        public Color LineSwapPrimaryColor = new Color(1f, 0.55f, 0.2f, 1f);
        public Color MismatchTileColor = new Color(0.86f, 0.36f, 0.38f, 1f);

        const float MismatchShakeAmplitude = 6f;
        const float MismatchShakeFrequency = 60f;

        Coroutine _mismatchRoutine;
        Vector2 _mismatchShakeBase;

        TileCoordStruct _tileStruct = default;
        IListener _listener;

        public int Row => _tileStruct.Row;
        public int Col => _tileStruct.Col;
        public int Number { get; private set; }

        public bool IsOpenCell { get; private set; }
        public bool IsActiveTile { get; private set; }

        public void Open(TileCoordStruct tileStruct, IListener listener)
        {
            _tileStruct = tileStruct;
            _listener = listener;
        }

        public void Clear()
        {
            StopMismatchFlash();
            SetActive(false);
            Number = 0;
            IsOpenCell = false;
            IsActiveTile = false;
            LabelTxt.text = string.Empty;
            LabelTxt.color = RemovedTextColor;

            SetBGAndOutlineColor(TileBoardBGColor);

            TileBtn.interactable = false;
            _tileStruct = default;
            _listener = null;
        }

        public void SetParent(Transform root)
        {
            transform.SetParent(root, false);
        }

        public void SetActive(bool enable)
        {
            gameObject.SetActive(enable);
        }

        public void SetOpen(bool isOpen)
        {
            StopMismatchFlash();
            IsOpenCell = isOpen;
            if (isOpen == false)
            {
                IsActiveTile = false;
                Number = 0;
                LabelTxt.text = string.Empty;
                TileBtn.interactable = false;
                SetBGAndOutlineColor(TileBoardBGColor);
                return;
            }

            TileBtn.interactable = true;
            SetBGAndOutlineColor(RemovedTileColor);
        }

        public void SetValue(int value)
        {
            if (IsOpenCell == false)
                return;

            StopMismatchFlash();

            if (value > 0)
            {
                IsActiveTile = true;
                Number = value;
                LabelTxt.text = value.ToString();
                SetTextColorRandomValue(value);
                SetBGAndOutlineColor(ActiveTileColor);
                TileBtn.interactable = true;
                return;
            }

            if (value == 0)
            {
                IsActiveTile = false;
                Number = 0;
                LabelTxt.text = string.Empty;
                LabelTxt.color = RemovedTextColor;
                SetBGAndOutlineColor(RemovedTileColor);
                TileBtn.interactable = false;
                return;
            }

            int abs = Mathf.Abs(value);
            IsActiveTile = false;
            Number = abs;
            LabelTxt.text = abs.ToString();
            LabelTxt.color = RemovedTextColor;
            SetBGAndOutlineColor(RemovedTileColor);
            TileBtn.interactable = false;
        }

        public void SetSelected(bool selected)
        {
            if (IsOpenCell == false)
                return;

            if (IsActiveTile == false)
                return;

            StopMismatchFlash();
            BGImg.color = selected ? SelectedTileColor : ActiveTileColor;
        }

        public void ShowMismatch(float duration)
        {
            if (IsOpenCell == false || IsActiveTile == false)
                return;

            StopMismatchFlash();
            SetBGAndOutlineColor(MismatchTileColor);

            if (isActiveAndEnabled)
            {
                if (BGImg != null)
                    _mismatchShakeBase = BGImg.rectTransform.anchoredPosition;

                _mismatchRoutine = StartCoroutine(MismatchRoutine(duration));
            }
            else
            {
                RestoreActiveColor();
            }
        }

        IEnumerator MismatchRoutine(float duration)
        {
            RectTransform shakeTrans = BGImg != null ? BGImg.rectTransform : null;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                if (shakeTrans != null)
                {
                    float damper = 1f - Mathf.Clamp01(elapsed / duration);
                    float offsetX = Mathf.Sin(elapsed * MismatchShakeFrequency) * MismatchShakeAmplitude * damper;
                    shakeTrans.anchoredPosition = _mismatchShakeBase + new Vector2(offsetX, 0f);
                }

                yield return null;
            }

            if (shakeTrans != null)
                shakeTrans.anchoredPosition = _mismatchShakeBase;

            _mismatchRoutine = null;
            RestoreActiveColor();
        }

        void RestoreActiveColor()
        {
            if (IsActiveTile)
                SetBGAndOutlineColor(ActiveTileColor);
        }

        void StopMismatchFlash()
        {
            if (_mismatchRoutine == null)
                return;

            StopCoroutine(_mismatchRoutine);
            _mismatchRoutine = null;

            if (BGImg != null)
                BGImg.rectTransform.anchoredPosition = _mismatchShakeBase;
        }

        public void SetLineSwapHighlight(LineSwapHighlightType highlightType)
        {
            if (IsOpenCell == false)
            {
                return;
            }

            StopMismatchFlash();

            if (highlightType == LineSwapHighlightType.Primary)
            {
                SetBGAndOutlineColor(LineSwapPrimaryColor);
                return;
            }

            SetBGAndOutlineColor(IsActiveTile ? ActiveTileColor : RemovedTileColor);
        }

        public Vector2 GetCenterScreenPoint()
        {
            var rect = RectTrans.rect;
            return RectTransformUtility.WorldToScreenPoint(null, RectTrans.TransformPoint(rect.center));
        }

        public Vector2 GetCenterScreenPoint(Camera uiCamera)
        {
            var rect = RectTrans.rect;
            return RectTransformUtility.WorldToScreenPoint(uiCamera, RectTrans.TransformPoint(rect.center));
        }

        void SetTextColorRandomValue(int number)
        {
            if(TextColor.TryGetTextColor(number, out var color))
            {
                LabelTxt.color = color;
            }
        }

        void SetBGAndOutlineColor(Color color)
        {
            BGImg.color = color;
            OutlineImg.color = color;
        }

        #region Button Event Functions

        public void OnClickButton()
        {
            _listener.OnClickTile(this);
        }

        #endregion
    }
}

