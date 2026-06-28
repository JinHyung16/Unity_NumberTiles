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
            void ShowGameRule();
        }

        public TextMeshProUGUI GameStartTxt;
        public TextMeshProUGUI ResetStageTxt;
        public GameObject ResetStageBtnObj;

        IListener _listener;
        public void Open(int curStage, bool hasProgress, IListener listener)
        {
            OpenInternal(() =>
            {
                GameStartTxt.text = $"{curStage} 이어하기";
                if (ResetStageTxt != null)
                {
                    ResetStageTxt.text = $"{curStage} 다시하기";
                }

                bool showResetButton = curStage > 1 || hasProgress;
                if (ResetStageBtnObj != null)
                {
                    ResetStageBtnObj.SetActive(showResetButton);
                }

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

        public void OnClickGameRule()
        {
            _listener.ShowGameRule();
        }
    }
}
