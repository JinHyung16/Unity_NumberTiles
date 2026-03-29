using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace NTGame
{
    public class TileStatusAlarmPanel : MonoBehaviour
    {
        // Block BG가 있어 활성화 되어있는 동안에는 잠깐 클릭을 막음.
        // 추구 Close Event 부착해서 바로 취소될 수 있는 구조 열어두기
        const string RemoveLine = "라인 클리어!";
        const string ClearTileNumber = "{0} 클리어!";

        public TextMeshProUGUI DescTxt;
        public float ShowSeconds = 0.9f;

        Coroutine _coroutine;

        public void ShowLineClearAlarm()
        {
            SetEnable(true);
            ShowText(RemoveLine);
        }

        public void ShowTileNumberClearAlarm(int digit)
        {
            if (digit < 1 || digit > 9)
                return;

            SetEnable(true);
            string text = string.Format(ClearTileNumber, digit);
            ShowText(text);
        }

        public void Close()
        {
            if(_coroutine != null)
            {
                StopCoroutine(_coroutine);
                _coroutine = null;
            }
            SetEnable(false);
        }

        void SetEnable(bool enable)
        {
            gameObject.SetActive(enable);
        }

        void ShowText(string desc)
        {
            if (_coroutine != null)
                StopCoroutine(_coroutine);

            _coroutine = StartCoroutine(CoShow(desc));
        }

        IEnumerator CoShow(string desc)
        {
            DescTxt.text = desc;

            yield return new WaitForSeconds(ShowSeconds);
            _coroutine = null;
            SetEnable(false);
        }
    }
}
