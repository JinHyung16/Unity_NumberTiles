using UnityEngine;
using TMPro;
using DG.Tweening;

namespace NTGame
{
    public class ToastMessagePanel : MonoBehaviour
    {
        const string RemoveLine = "라인 클리어!";
        const string ClearTileNumber = "{0} 클리어!";

        [Header("Refs")]
        public TextMeshProUGUI DescTxt;
        [Tooltip("비워두면 self의 RectTransform 사용")]
        public RectTransform PanelRect;

        [Header("타이밍")]
        [Tooltip("페이드인 + 위로 떠오르는 시간")]
        public float FadeInDuration = 0.18f;
        [Tooltip("최대 알파 상태로 머무는 시간 (페이드인/아웃을 제외한 순수 노출 시간)")]
        public float HoldDuration = 0.55f;
        [Tooltip("페이드아웃 + 위로 사라지는 시간")]
        public float FadeOutDuration = 0.25f;

        [Header("모션")]
        [Tooltip("페이드인 시작 시 base 위치 기준 Y 오프셋(px). 음수 = 약간 아래에서 시작")]
        public float StartOffsetY = -20f;
        [Tooltip("페이드아웃 종료 시 base 위치 기준 Y 오프셋(px). 양수 = 위로 빠지며 사라짐")]
        public float EndOffsetY = 40f;
        [Tooltip("최대 알파 (1 = 자식 그래픽들의 원본 알파 그대로 표시)")]
        [Range(0f, 1f)]
        public float TargetAlpha = 1f;

        Sequence _seq;
        Vector2 _basePos;
        bool _baseCached;

        CanvasRenderer[] _renderers;
        float _currentAlpha;

        void Awake()
        {
            if (PanelRect == null)
                PanelRect = transform as RectTransform;

            _renderers = GetComponentsInChildren<CanvasRenderer>(true);
            CacheBasePosition();
        }

        void CacheBasePosition()
        {
            if (_baseCached || PanelRect == null) return;
            _basePos = PanelRect.anchoredPosition;
            _baseCached = true;
        }

        public void ShowLineClearAlarm()
        {
            Show(RemoveLine);
        }

        public void ShowTileNumberClearAlarm(int digit)
        {
            if (digit < 1 || digit > 9)
                return;

            Show(string.Format(ClearTileNumber, digit));
        }

        public void Close()
        {
            KillSeq();
            gameObject.SetActive(false);
        }

        void Show(string desc)
        {
            CacheBasePosition();

            if (DescTxt != null)
                DescTxt.text = desc;

            gameObject.SetActive(true);

            _renderers = GetComponentsInChildren<CanvasRenderer>(true);

            PlayShowSequence();
        }

        void PlayShowSequence()
        {
            KillSeq();

            SetGroupAlpha(0f);
            PanelRect.anchoredPosition = _basePos + new Vector2(0f, StartOffsetY);

            float hold = Mathf.Max(0f, HoldDuration);

            _seq = DOTween.Sequence()
                .SetLink(gameObject)
                .SetUpdate(true);

            _seq.Append(
                DOTween.To(() => _currentAlpha, SetGroupAlpha, TargetAlpha, FadeInDuration)
                       .SetEase(Ease.OutCubic));
            _seq.Join(PanelRect.DOAnchorPosY(_basePos.y, FadeInDuration).SetEase(Ease.OutCubic));

            if (hold > 0f)
                _seq.AppendInterval(hold);

            _seq.Append(
                DOTween.To(() => _currentAlpha, SetGroupAlpha, 0f, FadeOutDuration)
                       .SetEase(Ease.InCubic));
            _seq.Join(PanelRect.DOAnchorPosY(_basePos.y + EndOffsetY, FadeOutDuration).SetEase(Ease.InCubic));

            _seq.OnComplete(OnSequenceComplete);
        }

        void OnSequenceComplete()
        {
            _seq = null;
            gameObject.SetActive(false);
        }

        void SetGroupAlpha(float a)
        {
            _currentAlpha = a;

            if (_renderers == null) return;

            for (int i = 0; i < _renderers.Length; i++)
            {
                var cr = _renderers[i];
                if (cr != null)
                    cr.SetAlpha(a);
            }
        }

        void KillSeq()
        {
            if (_seq != null && _seq.IsActive())
                _seq.Kill();
            _seq = null;
        }

        void OnDisable()
        {
            KillSeq();

            if (_baseCached && PanelRect != null)
                PanelRect.anchoredPosition = _basePos;

            SetGroupAlpha(0f);
        }

        void OnDestroy()
        {
            KillSeq();
        }
    }
}
