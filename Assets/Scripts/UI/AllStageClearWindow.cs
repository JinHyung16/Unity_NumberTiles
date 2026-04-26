using TMPro;
using UnityEngine;

namespace NTGame
{
    public class AllStageClearWindow : BaseWindow
    {
        public interface IListener
        {
            void ClearGameData();
            void CancelAllClearWindow();
        }

        public TextMeshProUGUI MessageTxt;

        const string MessageText = "축하합니다.\n모든 스테이지를 클리어 했습니다.\n게임을 초기화 하고 다시 하시겠습니까?";

        IListener _listener;

        public void Open(IListener listener)
        {
            OpenInternal(() =>
            {
                _listener = listener;
                if (MessageTxt != null)
                {
                    MessageTxt.text = MessageText;
                }
            });
        }

        protected override void OnClose()
        {
            _listener = null;
        }

        public void OnClickResetGameData()
        {
            if (_listener == null)
            {
                return;
            }
            _listener.ClearGameData();
        }

        public void OnClickCancel()
        {
            if (_listener == null)
            {
                return;
            }
            _listener.CancelAllClearWindow();
        }
    }
}
