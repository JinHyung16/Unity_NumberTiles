using UnityEngine;
using TMPro;
using DG.Tweening;

namespace NTGame
{
    /// <summary>
    /// 화면 중앙에서 살짝 떠오르며 페이드인 → 홀드 → 페이드아웃되는 토스트 메시지.
    /// CanvasGroup을 사용하지 않고 자식들의 CanvasRenderer.SetAlpha로 그룹 페이드를 처리한다.
    /// (prefab에 별도 컴포넌트 추가 필요 없음)
    /// </summary>
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

        // 그룹 페이드를 위해 모든 자식 CanvasRenderer를 캐싱.
        // CanvasRenderer.SetAlpha는 각 렌더러의 final color에 곱해지므로
        // 자식 Image/Text의 원본 색상(BG 어두운 톤, outline 흰색 등)은 그대로 유지된다.
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

            // SetActive(true) 직후엔 자식 GameObject가 새로 활성화되며
            // CanvasRenderer가 추가/제거되었을 수도 있으니 재수집 (저렴함)
            _renderers = GetComponentsInChildren<CanvasRenderer>(true);

            PlayShowSequence();
        }

        void PlayShowSequence()
        {
            KillSeq();

            // 시작 상태: 알파 0, base보다 살짝 아래
            SetGroupAlpha(0f);
            PanelRect.anchoredPosition = _basePos + new Vector2(0f, StartOffsetY);

            float hold = Mathf.Max(0f, HoldDuration);

            // SetLink: GameObject 파괴 시 자동 Kill
            // SetUpdate(true): unscaledTime 사용 (게임 일시정지에서도 토스트 정상 진행)
            _seq = DOTween.Sequence()
                .SetLink(gameObject)
                .SetUpdate(true);

            // 1) 페이드인 + 중앙(base)으로 떠오름
            _seq.Append(
                DOTween.To(() => _currentAlpha, SetGroupAlpha, TargetAlpha, FadeInDuration)
                       .SetEase(Ease.OutCubic));
            _seq.Join(PanelRect.DOAnchorPosY(_basePos.y, FadeInDuration).SetEase(Ease.OutCubic));

            // 2) 홀드
            if (hold > 0f)
                _seq.AppendInterval(hold);

            // 3) 페이드아웃 + 위로 빠지며 사라짐
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
            // 다음 표시를 위해 트윈 정리하고 위치 복원
            KillSeq();

            if (_baseCached && PanelRect != null)
                PanelRect.anchoredPosition = _basePos;

            // 알파는 0으로 둬도 다음 Show()에서 어차피 0부터 시작
            SetGroupAlpha(0f);
        }

        void OnDestroy()
        {
            KillSeq();
        }
    }
}
