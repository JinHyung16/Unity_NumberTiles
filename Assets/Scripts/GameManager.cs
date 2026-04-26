using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Jinhyeong_GameData;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NTGame
{
    public class GameManager
        : SceneSingleton<GameManager>
        , ITileObserver
        , LobbyWindow.IListener
        , TileWindow.IListener
        , GameResultWindow.IListener
        , AllStageClearWindow.IListener
    {
        public Camera UICamera;

        [Header("Base Window 모음")]
        public LobbyWindow LobbyWindow;
        public TileWindow TileWindow;
        public GameResultWindow ResultWindow;
        public AllStageClearWindow AllClearWindow;

        int _curStageKey = 1;
        bool _ignoreResultCheck;
        bool _isPlaying;

        CancellationTokenSource _bootstrapCts;

        protected override void Awake()
        {
            base.Awake();
        }

        void Start()
        {
            _bootstrapCts = new CancellationTokenSource();
            BootstrapAsync(_bootstrapCts.Token).Forget();
        }

        void OnDestroy()
        {
            if (_bootstrapCts != null)
            {
                _bootstrapCts.Cancel();
                _bootstrapCts.Dispose();
                _bootstrapCts = null;
            }
        }

        async UniTaskVoid BootstrapAsync(CancellationToken cancellationToken)
        {
            GameMetaSaver.EnsureCreated();

            await DataManager.Instance.InitializeAsync(cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            int startStageKey = 0;

            if (GameProgressSaver.TryFindMostRecentStageKey(out int progressStageKey))
                startStageKey = Mathf.Max(1, progressStageKey);
            else
                startStageKey = GameMetaSaver.GetNextStageAfterClearOrDefault(startStageKey);

            if (HasStage(startStageKey))
                _curStageKey = startStageKey;

            _curStageKey = Mathf.Max(1, _curStageKey);

            LobbyWindow.Open(_curStageKey, this);
            TileWindow.Close();
            ResultWindow.Close();

            if (AllClearWindow != null)
            {
                AllClearWindow.Close();
            }
        }

        StageContainer GetStageContainer()
        {
            return DataManager.Instance.GetContainer<StageContainer>();
        }

        bool HasStage(int stageKey)
        {
            StageContainer container = GetStageContainer();
            if (container == null)
            {
                return false;
            }
            return container.ContainsKey(stageKey);
        }

        int GetMaxStageKey()
        {
            StageContainer container = GetStageContainer();
            if (container == null)
            {
                return 0;
            }
            int max = 0;
            foreach (KeyValuePair<int, Stage> kv in container.All)
            {
                if (kv.Key > max)
                {
                    max = kv.Key;
                }
            }
            return max;
        }

        bool IsAllStageCleared()
        {
            int maxStageKey = GetMaxStageKey();
            if (maxStageKey <= 0)
            {
                return false;
            }
            return GameMetaSaver.IsAllStageCleared(maxStageKey);
        }

        void ShowAllStageClearWindow()
        {
            if (AllClearWindow == null)
            {
                Debug.LogWarning("[GameManager] AllClearWindow 연결이 필요합니다");
                return;
            }

            _isPlaying = false;
            _ignoreResultCheck = true;

            if (TileWindow != null)
            {
                TileWindow.Close();
            }
            if (ResultWindow != null)
            {
                ResultWindow.Close();
            }

            AllClearWindow.Open(this);
        }

        void StartGame(int stageKey)
        {
            _curStageKey = Mathf.Max(1, stageKey);
            GameMetaSaver.UpdateLastStage(_curStageKey);

            var tileManager = TileManager.Instance;
            tileManager.ClearObservers();

            LobbyWindow.Close();
            ResultWindow.Close();

            PoolManager.Instance.InitPool();

            TileWindow.Open(this);

            _isPlaying = true;
            _ignoreResultCheck = true;
            tileManager.AddObserver(this);

            if (GameProgressSaver.TryLoad(_curStageKey, out var progress) &&
                tileManager.TryApplyProgress(_curStageKey, progress))
            {
                _ignoreResultCheck = false;
                TryShowResultIfNeeded();
                return;
            }

            tileManager.StartStage(_curStageKey);
            _ignoreResultCheck = false;
            TryShowResultIfNeeded();
        }
        void TryShowResultIfNeeded()
        {
            if (_isPlaying == false)
                return;

            if (_ignoreResultCheck)
                return;

            var tileManager = TileManager.Instance;
            if (tileManager.IsClearStage())
            {
                ShowResult(GameResultType.ClearStage);
                return;
            }

            if (tileManager.IsFailStage())
            {
                ShowResult(GameResultType.FailStage);
                return;
            }
        }

        void ShowResult(GameResultType gameResultType)
        {
            _isPlaying = false;
            _ignoreResultCheck = true;

            GameProgressSaver.Delete(_curStageKey);
            if (gameResultType == GameResultType.ClearStage)
                GameMetaSaver.UpdateClearedStage(_curStageKey);

            var tileManager = TileManager.Instance;
            tileManager.RemoveObserver(this);

            if (TileWindow != null)
                TileWindow.Close();

            if (ResultWindow != null)
                ResultWindow.Open(_curStageKey, gameResultType, this);
        }

        void GoToLobby()
        {
            if (ResultWindow != null)
                ResultWindow.Close();

            if (TileWindow != null)
                TileWindow.Close();

            if (LobbyWindow != null)
            {
                LobbyWindow.gameObject.SetActive(true);
                LobbyWindow.Open(_curStageKey, this);
            }

            TileManager.Instance.ClearObservers();
        }

        void ITileObserver.OnNotify(TileNotify notify)
        {
            if (_isPlaying == false)
                return;

            if (_ignoreResultCheck)
                return;

            if (notify.Type == TileNotifyType.BoardChanged ||
                notify.Type == TileNotifyType.ItemCountChanged)
            {
                TryShowResultIfNeeded();
            }
        }

        void LobbyWindow.IListener.StartStage()
        {
            if (IsAllStageCleared())
            {
                ShowAllStageClearWindow();
                return;
            }
            StartGame(_curStageKey);
        }

        void LobbyWindow.IListener.ResetStage()
        {
            GameProgressSaver.Delete(_curStageKey);
            StartGame(_curStageKey);
        }

        void LobbyWindow.IListener.ClearGameData()
        {
            TileManager.Instance.ClearObservers();
            _isPlaying = false;
            _ignoreResultCheck = true;

            GameProgressSaver.DeleteAll();
            GameMetaSaver.Reset();

            TileWindow.Close();
            ResultWindow.Close();

            var activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.buildIndex);
        }

        void TileWindow.IListener.ExitGame()
        {
            TileManager.Instance.ClearObservers();
            _isPlaying = false;
            _ignoreResultCheck = true;

            TileWindow.Close();
            LobbyWindow.Open(_curStageKey, this);
        }

        void TileWindow.IListener.UseItem(ItemType itemType)
        {
            TileManager.Instance.UseItem(itemType);
        }
 

        void GameResultWindow.IListener.GoToLobby()
        {
            GoToLobby();
        }

        void GameResultWindow.IListener.GoToStage(GameResultType gameResultType)
        {
            if (ResultWindow != null)
                ResultWindow.Close();

            if (gameResultType == GameResultType.ClearStage)
            {
                int nextStageKey = _curStageKey + 1;
                if (HasStage(nextStageKey))
                {
                    StartGame(nextStageKey);
                    return;
                }

                if (IsAllStageCleared())
                {
                    ShowAllStageClearWindow();
                    return;
                }

                GoToLobby();
                return;
            }

            StartGame(_curStageKey);
        }

        void AllStageClearWindow.IListener.ClearGameData()
        {
            if (AllClearWindow != null)
            {
                AllClearWindow.Close();
            }
            ((LobbyWindow.IListener)this).ClearGameData();
        }

        void AllStageClearWindow.IListener.CancelAllClearWindow()
        {
            if (AllClearWindow != null)
            {
                AllClearWindow.Close();
            }
            GoToLobby();
        }
    }
}

