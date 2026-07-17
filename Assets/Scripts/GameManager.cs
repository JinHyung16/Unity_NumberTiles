using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Jinhyeong_GameData;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

namespace NTGame
{
    public class GameManager
        : SceneSingleton<GameManager>
        , ITileObserver
        , LobbyWindow.IListener
        , TileWindow.IListener
        , GameResultWindow.IListener
    {
        [SerializeField] private Camera _uiCamera;
        [SerializeField] private LobbyWindow _lobbyWindow;
        [SerializeField] private Transform _windowRoot;

        public Camera UICamera => _uiCamera;

        TileWindow _tileWindow;
        GameResultWindow _resultWindow;
        GameRuleWindow _gameRuleWindow;

        readonly List<AsyncOperationHandle<GameObject>> _windowHandles = new List<AsyncOperationHandle<GameObject>>(4);

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

            ReleaseWindows();
        }

        async UniTaskVoid BootstrapAsync(CancellationToken cancellationToken)
        {
            GameMetaSaver.EnsureCreated();

            await DataManager.Instance.InitializeAsync(cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            await LoadWindowsAsync(cancellationToken);

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

            _lobbyWindow.Open(_curStageKey, HasStageProgress(_curStageKey), this);
            _tileWindow.Close();
            _resultWindow.Close();
            _gameRuleWindow.Close();
        }

        async UniTask LoadWindowsAsync(CancellationToken cancellationToken)
        {
            //_lobbyWindow = await InstantiateWindowAsync<LobbyWindow>(AddressableKeys.Windows.Lobby, cancellationToken);
            _tileWindow = await InstantiateWindowAsync<TileWindow>(AddressableKeys.Windows.Tile, cancellationToken);
            _resultWindow = await InstantiateWindowAsync<GameResultWindow>(AddressableKeys.Windows.GameResult, cancellationToken);
            _gameRuleWindow = await InstantiateWindowAsync<GameRuleWindow>(AddressableKeys.Windows.GameRule, cancellationToken);
        }

        async UniTask<T> InstantiateWindowAsync<T>(string address, CancellationToken cancellationToken) where T : BaseWindow
        {
            AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(address, _windowRoot);
            _windowHandles.Add(handle);

            GameObject instance = await handle.ToUniTask(cancellationToken: cancellationToken);

            var window = instance.GetComponent<T>();
            Debug.Assert(window != null, $"[GameManager] 어드레서블 \"{address}\" 에 {typeof(T).Name} 컴포넌트가 없습니다");

            var canvas = instance.GetComponent<Canvas>();
            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                canvas.worldCamera = _uiCamera;
            }

            return window;
        }

        void ReleaseWindows()
        {
            //_lobbyWindow = null;
            _tileWindow = null;
            _resultWindow = null;

            for (int i = 0; i < _windowHandles.Count; i++)
            {
                AsyncOperationHandle<GameObject> handle = _windowHandles[i];
                if (handle.IsValid())
                {
                    Addressables.ReleaseInstance(handle);
                }
            }
            _windowHandles.Clear();
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

        bool HasStageProgress(int stageKey)
        {
            if (stageKey <= 0)
            {
                return false;
            }
            return GameProgressSaver.TryLoad(stageKey, out _);
        }

        void StartGame(int stageKey)
        {
            _curStageKey = Mathf.Max(1, stageKey);
            GameMetaSaver.UpdateLastStage(_curStageKey);

            var tileManager = TileManager.Instance;
            tileManager.ClearObservers();

            _lobbyWindow.Close();
            _resultWindow.Close();

            PoolManager.Instance.InitPool();

            _tileWindow.Open(this);

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
            {
                GameMetaSaver.UpdateClearedStage(_curStageKey);
                SoundManager.Instance.PlaySfx(SoundType.RoundClear);
            }
            else if (gameResultType == GameResultType.FailStage)
            {
                SoundManager.Instance.PlaySfx(SoundType.RoundFail);
            }

            var tileManager = TileManager.Instance;
            tileManager.RemoveObserver(this);

            if (_tileWindow != null)
                _tileWindow.Close();

            if (_resultWindow != null)
                _resultWindow.Open(_curStageKey, gameResultType, this);
        }

        void GoToLobby()
        {
            if (_resultWindow != null)
                _resultWindow.Close();

            if (_tileWindow != null)
                _tileWindow.Close();

            if (_lobbyWindow != null)
            {
                _lobbyWindow.Open(_curStageKey, HasStageProgress(_curStageKey), this);
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

            _tileWindow.Close();
            _resultWindow.Close();

            var activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.buildIndex);
        }

        void LobbyWindow.IListener.ShowGameRule()
        {
            _gameRuleWindow.Open();
        }

        void TileWindow.IListener.ExitGame()
        {
            TileManager.Instance.ClearObservers();
            _isPlaying = false;
            _ignoreResultCheck = true;

            _tileWindow.Close();
            _lobbyWindow.Open(_curStageKey, HasStageProgress(_curStageKey), this);
        }

        void TileWindow.IListener.UseItem(ItemType itemType)
        {
            TileManager.Instance.UseItem(itemType);
        }

        void TileWindow.IListener.CancelTargetItem(ItemType itemType)
        {
            TileManager.Instance.CancelPendingTargetItem(itemType);
        }


        void GameResultWindow.IListener.GoToLobby()
        {
            GoToLobby();
        }

        void GameResultWindow.IListener.GoToStage(GameResultType gameResultType)
        {
            if (_resultWindow != null)
                _resultWindow.Close();

            if (gameResultType == GameResultType.ClearStage)
            {
                int nextStageKey = _curStageKey + 1;
                if (HasStage(nextStageKey))
                {
                    StartGame(nextStageKey);
                    return;
                }

                GoToLobby();
                return;
            }

            StartGame(_curStageKey);
        }
    }
}
