using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace NTGame
{
    public class TileLineRender : MonoBehaviour
    {
        [Min(1f)]
        public float Thickness = 30f;

        [Header("Animation (seconds)")]
        [Min(0f)] public float DrawInSeconds = 0.08f;
        [Min(0f)] public float HoldSeconds = 0.18f;
        [Min(0f)] public float FadeOutSeconds = 0.15f;

        [Header("Fade-out thickness expand (x scale)")]
        [Min(1f)] public float EndThicknessScale = 1.5f;

        [SerializeField]
        private RectTransform _lineRect;

        [SerializeField]
        private Image _lineImage;

        Coroutine _co;

        void Awake()
        {
            if (_lineImage == null && _lineRect != null)
                _lineImage = _lineRect.GetComponent<Image>();
        }

        public void ShowLine(Vector2 screenA, Vector2 screenB, RectTransform root, Camera uiCamera)
        {
            if (_lineRect == null || root == null)
                return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screenA, uiCamera, out var aLocal);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screenB, uiCamera, out var bLocal);

            Vector2 dir = bLocal - aLocal;
            float len = dir.magnitude;
            if (len <= 0.001f)
                return;

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            // Pivot at left-center so the rect grows out from point A toward B.
            _lineRect.anchorMin = new Vector2(0.5f, 0.5f);
            _lineRect.anchorMax = new Vector2(0.5f, 0.5f);
            _lineRect.pivot = new Vector2(0f, 0.5f);
            _lineRect.anchoredPosition = aLocal;
            _lineRect.localRotation = Quaternion.Euler(0f, 0f, angle);
            _lineRect.sizeDelta = new Vector2(0f, Thickness);
            _lineRect.SetAsLastSibling();

            if (_co != null)
                StopCoroutine(_co);
            _co = StartCoroutine(CoPlay(len));
        }

        public void Clear()
        {
            if (_co != null)
            {
                StopCoroutine(_co);
                _co = null;
            }
            SetVisible(false);
        }

        IEnumerator CoPlay(float len)
        {
            SetVisible(true);
            SetAlpha(1f);

            // 1) Draw-in: sizeDelta.x  0 -> len (ease-out cubic)
            float t = 0f;
            while (t < DrawInSeconds)
            {
                t += Time.unscaledDeltaTime;
                float u = DrawInSeconds <= 0f ? 1f : Mathf.Clamp01(t / DrawInSeconds);
                float eased = 1f - Mathf.Pow(1f - u, 3f);
                _lineRect.sizeDelta = new Vector2(len * eased, Thickness);
                yield return null;
            }
            _lineRect.sizeDelta = new Vector2(len, Thickness);

            // 2) Hold
            if (HoldSeconds > 0f)
                yield return new WaitForSecondsRealtime(HoldSeconds);

            // 3) Fade-out: alpha 1->0 + thickness 1.0x -> EndThicknessScale (ease-in quad)
            t = 0f;
            float startThickness = Thickness;
            float endThickness = Thickness * EndThicknessScale;
            while (t < FadeOutSeconds)
            {
                t += Time.unscaledDeltaTime;
                float u = FadeOutSeconds <= 0f ? 1f : Mathf.Clamp01(t / FadeOutSeconds);
                float easedAlpha = u * u;
                SetAlpha(1f - easedAlpha);
                float th = Mathf.Lerp(startThickness, endThickness, u);
                _lineRect.sizeDelta = new Vector2(len, th);
                yield return null;
            }

            SetAlpha(0f);
            SetVisible(false);
            _co = null;
        }

        void SetVisible(bool visible)
        {
            if (_lineImage != null)
                _lineImage.enabled = visible;
        }

        void SetAlpha(float a)
        {
            if (_lineImage == null)
                return;
            var c = _lineImage.color;
            c.a = a;
            _lineImage.color = c;
        }
    }
}
