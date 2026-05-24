using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace NTGame
{
    public class LobbyWindow : BaseWindow
    {
        public interface IListener
        {
            void StartStage();
            void ResetStage();
            void ClearGameData();
        }

        public TextMeshProUGUI GameStartTxt;
        public TextMeshProUGUI ResetStageTxt;

        IListener _listener;
        public void Open(int curStage, IListener listener)
        {
            OpenInternal(() =>
            {
                GameStartTxt.text = $"{curStage} 이어하기";
                ResetStageTxt.text = $"{curStage} 다시하기";
                _listener = listener;
            });
        }

        protected override void OnClose()
        {
            _listener = null;
        }

        public void OnClickStartGame()
        {
            _listener.StartStage();
        }

        public void OnClickResetStage()
        {
            _listener.ResetStage();
        }

        public void OnClickClearGameData()
        {
            _listener.ClearGameData();
        }
    }
}
