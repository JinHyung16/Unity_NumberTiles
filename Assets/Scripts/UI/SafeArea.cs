using UnityEngine;

namespace NTGame
{
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public class SafeArea : MonoBehaviour
    {
        [Tooltip("상단(노치/카메라/펀치홀) 영역을 피할지 여부")]
        public bool ApplyTop = true;

        [Tooltip("하단(홈 인디케이터/제스처 바) 영역을 피할지 여부")]
        public bool ApplyBottom = true;

        [Tooltip("좌측 영역을 피할지 여부")]
        public bool ApplyLeft = true;

        [Tooltip("우측 영역을 피할지 여부")]
        public bool ApplyRight = true;

        void Awake()
        {
            RectTransform rect = GetComponent<RectTransform>();

            int sw = Screen.width;
            int sh = Screen.height;
            if (sw <= 0 || sh <= 0) return;

            Rect safe = Screen.safeArea;

            float xMin = ApplyLeft ? safe.xMin : 0f;
            float xMax = ApplyRight ? safe.xMax : sw;
            float yMin = ApplyBottom ? safe.yMin : 0f;
            float yMax = ApplyTop ? safe.yMax : sh;

            rect.anchorMin = new Vector2(xMin / sw, yMin / sh);
            rect.anchorMax = new Vector2(xMax / sw, yMax / sh);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
