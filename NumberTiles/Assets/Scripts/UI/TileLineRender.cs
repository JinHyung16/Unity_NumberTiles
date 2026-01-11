using UnityEngine;
using UnityEngine.UI;

namespace NTGame
{
    public class TileLineRender : MonoBehaviour
    {
        [Header("Line Style")]
        [Min(1f)] 
        public float Thickness = 30f;

        [Min(0f)]
        public float AutoHideSeconds = 0.35f;

        [SerializeField]
        private RectTransform _lineRect;

        private float _hideTime;

        public void ShowLine(Vector2 screenA, Vector2 screenB, RectTransform root, Camera uiCamera)
        {
            if (_lineRect == null || root == null)
                return;

            ClearLineRect();

            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screenA, uiCamera, out var aLocal);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screenB, uiCamera, out var bLocal);

            Vector2 dir = (bLocal - aLocal);
            float len = dir.magnitude;
            if (len <= 0.001f)
                return;

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            Vector2 mid = (aLocal + bLocal) * 0.5f;

            SetEnableLineImage(true);

            _lineRect.anchoredPosition = mid;
            _lineRect.sizeDelta = new Vector2(len, Thickness);
            _lineRect.localRotation = Quaternion.Euler(0f, 0f, angle);
            _lineRect.SetAsLastSibling();

            if (AutoHideSeconds > 0f)
                _hideTime = Time.unscaledTime + AutoHideSeconds;
            else
                _hideTime = 0f;
        }

        public void Clear()
        {
            ClearLineRect();
        }

        void Update()
        {
            if (_hideTime <= 0)
                return;

            if (Time.unscaledTime >= _hideTime)
            {
                SetEnableLineImage(false);
                _hideTime = 0f;
            }
        }

        void SetEnableLineImage(bool enable)
        {
            _lineRect.gameObject.SetActive(enable);
        }

        void ClearLineRect()
        {
            _lineRect.anchorMin = new Vector2(0.5f, 0.5f);
            _lineRect.anchorMax = new Vector2(0.5f, 0.5f);
            _lineRect.pivot = new Vector2(0.5f, 0.5f);
        }
    }
}

