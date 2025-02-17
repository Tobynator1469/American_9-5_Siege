using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum EGameState
{
    None,
    RoundStart,
    GameRunning,
    RoundEnd,
}
public struct TeamState
{
    public PlayerTeam team;
    public int roundMoney;

    public TeamState(PlayerTeam team)
    {
        this.team = team;
        roundMoney = 0;
    }
}

public class AMS_GameState : NetworkBehaviour
{
    private AMServerManger amsServerManger = null;

    private Dictionary<PlayerTeam, TeamState> teamDictionary = new()
    {
        { PlayerTeam.Thief, new TeamState(PlayerTeam.Thief)},
        { PlayerTeam.Defender, new TeamState(PlayerTeam.Defender)}
    };

    private Dictionary<PlayerTeam, List<ulong>> m_cachedTeams = null;

    private int roundsPlayed = 0;

    [SerializeField]
    private int roundsToPlay = 4;

    [SerializeField]
    private float timeTillRoundStart = 0.0f;
    private float timeTillRoundStartCur = 0.0f;

    [SerializeField]
    private float timeTillRoundEnd = 0.0f;
    private float timeTillRoundEndCur = 0.0f;

    [SerializeField]
    private float timeTillPostEnd_End = 0.0f;
    private float timeTillPostEnd_EndCur = 0.0f;

    private EGameState gameState = EGameState.None;

    private bool isInitialized = false;
    private bool hasAMS = false;
    private bool hasPlayersCached = false;

    public delegate void OnGameStateDestroyed(AMS_GameState _this);
    public delegate void OnGameRoundStarted(AMS_GameState _this);
    public delegate void OnGameRoundEnded(AMS_GameState _this);
    public delegate void OnGameEnded(AMS_GameState _this);

    public delegate void OnGameUpdateTimer(AMS_GameState _this, EGameState gameState,float time);

    private OnGameStateDestroyed onGameStateDestroyed = null;
    private OnGameRoundStarted onGameRoundStarted = null;
    private OnGameRoundEnded onGameRoundEnded = null;
    private OnGameEnded onGameEnded = null;

    private OnGameUpdateTimer onGameUpdateTimer = null;

    public void BindOnGameStateDestroyed(OnGameStateDestroyed onGameStateDestroyed)
    {
        this.onGameStateDestroyed += onGameStateDestroyed;
    }

    public void BindOnGameRoundStarted(OnGameRoundStarted onGameRoundStarted)
    {
        this.onGameRoundStarted += onGameRoundStarted;
    }

    public void BindOnGameRoundEnded(OnGameRoundEnded onGameRoundEnded)
    {
        this.onGameRoundEnded += onGameRoundEnded;
    }

    public void BindOnGameEnded(OnGameEnded onGameEnded)
    {
        this.onGameEnded += onGameEnded;
    }

    public void BindOnGameUpdateTimer(OnGameUpdateTimer onGameUpdateTimer)
    {
        this.onGameUpdateTimer += onGameUpdateTimer;
    }

    public void UnbindOnGameStateDestroyed(OnGameStateDestroyed onGameStateDestroyed)
    {
        this.onGameStateDestroyed -= onGameStateDestroyed;
    }

    public void UnbindOnGameRoundStarted(OnGameRoundStarted onGameRoundStarted)
    {
        this.onGameRoundStarted -= onGameRoundStarted;
    }

    public void UnbindOnGameRoundEnded(OnGameRoundEnded onGameRoundEnded)
    {
        this.onGameRoundEnded -= onGameRoundEnded;
    }

    public void UnbindOnGameEnded(OnGameEnded onGameEnded)
    {
        this.onGameEnded -= onGameEnded;
    }

    public void UnbindOnGameUpdateTimer(OnGameUpdateTimer onGameUpdateTimer)
    {
        this.onGameUpdateTimer -= onGameUpdateTimer;
    }

    public override void OnNetworkDespawn()
    {
        if(isInitialized)
        {
            onGameStateDestroyed(this);

            hasAMS = false;
            isInitialized = false;

            amsServerManger.UnbindOnAMServerManagerDestroyed(OnServerManagerDestroyed);
        }
    }

    public override void OnNetworkSpawn()
    {

    }

    private void OnServerManagerDestroyed(AMServerManger _this)
    {
        UninitializeState();

        amsServerManger = null;
        hasAMS = false;
    }

    private void UninitializeState()
    {
        isInitialized = false;
    }

    private void InitializeState()
    {
        isInitialized = true;
    }

    public void OnBindServerManager(AMServerManger serverManger)
    {
        serverManger.BindOnAMServerManagerDestroyed(OnServerManagerDestroyed);

        amsServerManger = serverManger;
        hasAMS = true;

        InitializeState();
    }

    private void Update()
    {
        if (!CanRunGameState())
            return;

        switch (gameState)
        {
            case EGameState.RoundStart:
                if(timeTillRoundStartCur >= timeTillRoundStart)
                {
                    gameState = EGameState.GameRunning;

                    OnRoundStart();
                }
                else
                {
                    UpdateTimer(timeTillRoundStartCur);

                    timeTillRoundStartCur += Time.deltaTime;
                }
                break;

            case EGameState.GameRunning:
                if(timeTillRoundEndCur >= timeTillRoundEnd)
                {
                    gameState = EGameState.RoundEnd;

                    OnRoundEnd();
                }
                else
                {
                    UpdateGame();

                    UpdateTimer(timeTillRoundEndCur);

                    timeTillRoundEndCur += Time.deltaTime;
                }
                break;

            case EGameState.RoundEnd:
                if(timeTillPostEnd_EndCur >= timeTillPostEnd_End)
                {
                    if(OnNextRound())
                        gameState = EGameState.RoundStart;
                    else
                    {
                        gameState = EGameState.None;

                        OnStateEnded();
                    }
                }
                else
                {
                    UpdateTimer(timeTillPostEnd_EndCur);

                    timeTillPostEnd_EndCur += Time.deltaTime;
                }
                break;

            default:
                break;
        }
    }

    private void OnRoundStart()
    {
        if (onGameRoundStarted != null)
            onGameRoundStarted(this);

        m_cachedTeams = amsServerManger.GetTeamPlayerList();

        SetPlayersMovementState_ServerRpc(false);
    }

    private void OnRoundEnd()
    {
        if (onGameRoundEnded != null)
            onGameRoundEnded(this);
    }

    private void UpdateGame()
    {

    }

    private bool OnNextRound()
    {
        if (roundsPlayed++ >= roundsToPlay)
            return false;

        ResetTimer();

        gameState = EGameState.RoundStart;

        return true;
    }

    private void OnStateEnded()
    {
        if(onGameEnded != null)
            onGameEnded(this);
    }

    private void UpdateTimer(float timer)
    {
        if (onGameUpdateTimer != null)
            onGameUpdateTimer(this, gameState, timer);
    }

    [ServerRpc]
    private void SetPlayersMovementState_ServerRpc(bool canMove)
    {
        var connectedPlayers = amsServerManger.GetConnectedPlayers();

        foreach(var player in connectedPlayers)
        {
            player.Value.SetCanMoveServerRpc(false);
        }
    }

    private void SwitchPlayerTeams()
    {

    }

    private void ResetTimer()
    {
        timeTillRoundEndCur = 0.0f;
        timeTillRoundStartCur = 0.0f;
        timeTillPostEnd_End = 0.0f;
    }

    private bool CanRunGameState()
    {
        return hasAMS && isInitialized;
    }

    public bool IsGameRunning()
    {
        return gameState == EGameState.GameRunning;
    }

    public EGameState GetCurrentGameState()
    {
        return gameState;
    }
}
