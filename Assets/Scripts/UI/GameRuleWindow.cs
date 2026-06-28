using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NTGame
{
    public class GameRuleWindow : BaseWindow
    {
        [SerializeField] private GameRuleDataSO _ruleData;
        [SerializeField] private Image _displayIMG;
        [SerializeField] private TextMeshProUGUI _descriptionTXT;

        private int _curIndex = 0;

        public void Open()
        {
            OpenInternal(() =>
            {
                _curIndex = 0;
                RefreshDisplay();
            });
        }

        protected override void OnClose()
        {
            base.OnClose();
        }

        private void RefreshDisplay()
        {
            GameRuleDataSO.Page page = GetPage(_curIndex);

            if (_displayIMG != null)
                _displayIMG.sprite = page != null ? page.Image : null;

            if (_descriptionTXT != null)
                _descriptionTXT.text = page != null ? page.Description : string.Empty;
        }

        GameRuleDataSO.Page GetPage(int curIndex)
        {
            if (_ruleData == null)
                return null;

            return _ruleData.GetPage(curIndex);
        }

        public void OnClickNext()
        {
            if (_ruleData == null || _ruleData.Count == 0)
                return;

            _curIndex++;
            if (_curIndex >= _ruleData.Count)
                _curIndex = 0;

            RefreshDisplay();
        }

        public void OnClickBack()
        {
            if (_ruleData == null || _ruleData.Count == 0)
                return;

            _curIndex--;
            if (_curIndex < 0)
                _curIndex = _ruleData.Count - 1;

            RefreshDisplay();
        }
    }
}
