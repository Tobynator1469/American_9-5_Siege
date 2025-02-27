#define DebugSkipCutscene

using Assets.Scripts;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static AMServerManger;
using static Player;

public enum PlayerTeam
{
    None,
    Lobby,
    Thief,
    Defender,
    Spectator
}

public class AMServerManger : ServerManager
{
    [SerializeField]
    private GameObject ThiefPrefab = null;

    [SerializeField]
    private GameObject DefenderPrefab = null;

    [SerializeField]
    private GameObject SpectatorPrefab = null;

    [SerializeField]
    private GameObject SpectatorLocalPlayer = null;

    [SerializeField, Tooltip("Set the Specific Game State for the Scene!")]
    private GameObject GameStatePrefab = null;

    [SerializeField]
    private GameObject MapCutscene = null;

    [SerializeField]
    private GameObject SpectatorSpawnLocation = null;

    [SerializeField]
    private GameObject vanGameObj = null;

    private AMS_GameState gameState = null; //Current Game State

    private MultiplayerCutscene currentPlayingCutscene = null;

    Dictionary<ulong, AMSPlayer> connectedPlayers = new Dictionary<ulong, AMSPlayer>();

    Dictionary<PlayerTeam, List<ulong>> teams = new()
    { { PlayerTeam.Thief, new List<ulong>() }, { PlayerTeam.Defender, new List<ulong>() }, { PlayerTeam.Spectator, new List<ulong>() }, { PlayerTeam.Lobby, new List<ulong>() } };

    Dictionary<ulong, PlayerTeam> players = new Dictionary<ulong, PlayerTeam>();
    Dictionary<ulong, PlayerTeam> playerCopy = null;

    private float defaultRaycastDistance = 4.0f;

    private float timeTillStart = 4.0f;
    private float timeTillStartCur = 0.0f;

    private int InteractableLayer = 0;

    private bool startGame = false;
    private bool hasGameStarted = false;

    public delegate void OnAMServerManagerDestroyed(AMServerManger sv);
    public delegate void OnAMServerManagerGameStarted(AMServerManger sv, AMS_GameState gameState);

    private OnAMServerManagerDestroyed onAMServerManagerDestroyed = null;
    private OnAMServerManagerGameStarted onAMServerManagerGameStarted = null;

    public void BindOnAMServerManagerDestroyed(OnAMServerManagerDestroyed onAMServerManagerDestroyed) 
    {
        this.onAMServerManagerDestroyed += onAMServerManagerDestroyed;
    }

    public void BindOnAMServerManagerGameStarted(OnAMServerManagerGameStarted onAMServerManagerGameStarted)
    {
        this.onAMServerManagerGameStarted += onAMServerManagerGameStarted;
    }

    public void UnbindOnAMServerManagerDestroyed(OnAMServerManagerDestroyed onAMServerManagerDestroyed)
    {
        this.onAMServerManagerDestroyed -= onAMServerManagerDestroyed;
    }

    public void UnbindOnAMServerManagerGameStarted(OnAMServerManagerGameStarted onAMServerManagerGameStarted)
    {
        this.onAMServerManagerGameStarted -= onAMServerManagerGameStarted;
    }

    protected override void OnStartServer()
    {
        InteractableLayer = 1 << LayerMask.NameToLayer("Interactable");
    }

    protected override void OnDestroyServer()
    {
        if (onAMServerManagerDestroyed != null)
            onAMServerManagerDestroyed(this);
    }

    [ServerRpc]
    protected override void OnStartGameBtnServerRpc()
    {
        var playerList = GetPlayerDictionary();

        foreach (var connected in playerList)
        {
            var amsPlayer = (AMSPlayer)connected.Value; // Every player is an Amsplayer so shouldnt be an issue

            amsPlayer.SetPendingGameStart_ServerRpc(true);
        }

        if (!startGame && !hasGameStarted)
        {
            startGame = true;

            timeTillStartCur = 0.0f;
        }
    }

    protected override void OnUpdateServer()
    {
        if(IsHost)
        {
            if (startGame && !hasGameStarted)
            {
                if (timeTillStartCur >= timeTillStart)
                {
                    startGame = false;
                    hasGameStarted = true;

                    OnGameStarted_ServerRpc();
                }
                else
                    timeTillStartCur += Time.deltaTime;
            }
        }
    }

    [ServerRpc]
    private void DestroyPlayer_ServerRpc(ulong id)
    {
        var playerInstance = FindPlayer(id);

        if(playerInstance)
        {
            playerInstance.NetworkObject.Despawn();
        }
    }
    private void OnInitializeAMS_PlayerDefaults(AMSPlayer player)
    {
        player.SetRaycastDist_ServerRpc(defaultRaycastDistance);
    }

    [ServerRpc]
    public void SpawnPlayer_ServerRpc(PlayerTeam team, ulong id)
    {
        var player_ = FindPlayer(id);

        if (!player_)
        {
            DebugClass.Error("Failed to Spawn Player!, Player was Invalid!");
            return;
        }

        string playerName = player_.playerName;

        DestroyPlayer_ServerRpc(id); //Destroy Instance before instantiating another one

        var transform = GetRandomSpawnLocation();

        GameObject instance = null;

        bool isSpectator = false;

        switch (team)
        {
            case PlayerTeam.Lobby:
                instance = Instantiate(GetDefaultPlayerPrefab(), transform.position, transform.rotation);
                break;
            case PlayerTeam.Thief:
                instance = Instantiate(ThiefPrefab, transform.position, transform.rotation);
                break;
            case PlayerTeam.Defender:
                instance = Instantiate(DefenderPrefab, transform.position, transform.rotation);
                break;
            case PlayerTeam.Spectator:
                instance = Instantiate(SpectatorPrefab, Vector3.zero, Quaternion.Euler(Vector3.zero));
                isSpectator = true;
                break;

            default:
                return;
        }

        var netObjComp = instance.GetComponent<NetworkObject>();
        var playerComp = netObjComp.GetComponent<AMSPlayer>();

        //Update Player Instance

        #region UpdateList

        if (!connectedPlayers.ContainsKey(id))
            connectedPlayers.Add(id, playerComp);
        else
            connectedPlayers[id] = playerComp;

        #endregion

        IntializeDefaults_PlayerComp(playerComp);

        playerComp.BindOnDestroy(OnPlayerDestroyed);

        playerComp.id = id;

        playerComp.playerName = playerName;

        netObjComp.Spawn(true);

        if(isSpectator)
            CreateAMS_SpectatorLocalPlayerRpc(netObjComp.NetworkObjectId, RpcTarget.Single(id, RpcTargetUse.Temp));
        else
            CreateLocalPlayerRpc(netObjComp.NetworkObjectId, RpcTarget.Single(id, RpcTargetUse.Temp));

        ShareNewNameClientRpc(netObjComp.NetworkObjectId, playerName);

        ShareNewPlayerTeam_ClientRpc(netObjComp.NetworkObjectId, team);

        DebugClass.Log("Switching Player to Team: " + team.ToString());
        SwitchPlayerToTeam_ClientRpc(id, team);
    }

    [Rpc(SendTo.SpecifiedInParams)]
    protected void CreateAMS_SpectatorLocalPlayerRpc(ulong EntityID, RpcParams toSender)
    {
        if (!CheckAuthorityServer(toSender))
            return;

        var localPlayer = GetNetworkObject(EntityID);

        var inst = Instantiate(SpectatorLocalPlayer, localPlayer.transform.position + (Vector3.up * defaultHeadHeight), localPlayer.transform.rotation);

        inst.transform.parent = localPlayer.transform;

        inst.transform.rotation = localPlayer.transform.rotation;

        var localPlayerComp = inst.GetComponent<LocalPlayer>();
        var playerComp = localPlayer.GetComponent<Player>();

        localPlayerComp.BindNetworkPlayer(playerComp);
        localPlayerComp.OnInitializedLocalPlayer();
    }

    [ServerRpc]
    private void OnGameStarted_ServerRpc()
    {
        playerCopy = new Dictionary<ulong, PlayerTeam>(players);

        var list = new List<Player>(GetPlayerList());

        for (int i = 0; i < list.Count; i++)
        {
            SpawnPlayer_ServerRpc(PlayerTeam.Spectator, list[i].id);
        }

#if DebugSkipCutscene
        OnEndCutscene(null);
#else
        SpawnCutscene();
#endif
    }

    private void SpawnCutscene()
    {
        GameObject instance = Instantiate(MapCutscene, Vector3.zero, Quaternion.Euler(Vector3.zero));

        currentPlayingCutscene = instance.GetComponent<MultiplayerCutscene>();

        if(currentPlayingCutscene)
        {
            currentPlayingCutscene.NetworkObject.Spawn();

            currentPlayingCutscene.BindOnEndCutscene(OnEndCutscene);

            currentPlayingCutscene.BindServerManager(this);

            currentPlayingCutscene.StartCutscene_ServerRpc("GameStart");
        }
    }

    private void OnEndCutscene(MultiplayerCutscene cutscene)
    {
#if !DebugSkipCutscene
        cutscene.NetworkObject.Despawn();
#endif
        SetVanVisibility_ClientRpc(true);

        BalanceTeams();

        var playerEntries = new List<KeyValuePair<ulong, PlayerTeam>>(playerCopy);

        foreach (var item in playerEntries)
        {
            SpawnPlayer_ServerRpc(item.Value, item.Key);
            SwitchPlayerToTeam_ClientRpc(item.Key, item.Value);
        }

        SpawnGameState();
    }

    [ClientRpc]
    private void SetVanVisibility_ClientRpc(bool vanVisibility)
    {
        //if(vanGameObj)
           // vanGameObj.SetActive(vanVisibility);
    }

    private void SpawnGameState()
    {
        GameObject instance = Instantiate(GameStatePrefab, Vector3.zero, Quaternion.Euler(Vector3.zero));

        gameState = instance.GetComponent<AMS_GameState>();

        if (gameState)
        {
            gameState.NetworkObject.Spawn();

            gameState.BindOnGameStateDestroyed(OnGameStateDestroyed);
            gameState.BindOnGameEnded(OnGameStateEnded);

            gameState.OnBindServerManager(this);

            gameState.StartGame_ServerRpc();

            if(onAMServerManagerGameStarted != null)
                onAMServerManagerGameStarted(this, gameState);

            DebugClass.Log("Started Game State!");
        }
        else
        {
            Destroy(instance);

            DebugClass.Error("Prefab was not an GameState!");
        }
    }

    private void OnGameStateDestroyed(AMS_GameState gameState)
    {
        gameState.UnbindOnGameStateDestroyed(OnGameStateDestroyed);
        gameState.UnbindOnGameEnded(OnGameStateEnded);

        gameState = null;

        hasGameStarted = false;
    }

    private void OnGameStateEnded(AMS_GameState _this, Dictionary<bool, List<ulong>> playerState)
    {
        #region Return Game Results to Players

        var pList = playerState[true];

        if(pList != null)
        {
            for (int i = 0; i < pList.Count; i++)
            {
                var Player = FindConnectedPlayer(pList[i]);

                Player.PlayerGameResult_ServerRpc(true);
            }
        }

        pList = playerState[false];

        if (pList != null)
        {
            for (int i = 0; i < pList.Count; i++)
            {
                var Player = FindConnectedPlayer(pList[i]);

                Player.PlayerGameResult_ServerRpc(false);
            }
        }

        #endregion

        gameState.NetworkObject.Despawn();

        SetVanVisibility_ClientRpc(false);
    }


    [ServerRpc]
    public void OnPlayerTryInteract_ServerRpc(ulong ID, float YDirection)
    {
        var player = (AMSPlayer)FindPlayer(ID);

        if(player && player.CanInteract())
        {
            var transform_pos = player.transform.position;

            transform_pos.y += defaultHeadHeight;

            Vector3 direction = player.transform.forward;

            direction.y = YDirection;

           // Debug.DrawRay(transform_pos, direction, Color.red, 2000.0f);

            if (Physics.Raycast(transform_pos, direction, out RaycastHit hitInfo, player.GetRaycastDist(), InteractableLayer))
            {
                if(hitInfo.transform.TryGetComponent<Interactable>(out Interactable interactable))
                {
                    interactable.Interact_ServerRpc(ID, direction);
                }
            }
        }
    }

    public void PlayerKnockedOut(AMSPlayer player)
    {
        SpawnPlayer_ServerRpc(PlayerTeam.Spectator, player.id);
    }

    [ServerRpc]
    public void OnPlayerSwitchTeams_ServerRpc(ulong ID, PlayerTeam teamSelected)
    {
        var pID = ID;

        SwitchPlayerToTeam_ClientRpc(ID, teamSelected);
    }

    [ServerRpc]
    public void PlayerRoundMoneyHasUpdated_ServerRpc(ulong pID, int newMoney)
    {
        if(teams.TryGetValue(FindPlayerTeam(pID), out List<ulong> players))
        {
            for (int i = 0; i < players.Count; i++)
            {
                var player = FindConnectedPlayer(players[i]);

                if(player)
                {
                    gameState.PlayerChangeTeamMoney_ServerRpc(player.currentTeam, newMoney);

                    player.hasRoundMoneyUpdated = new PBool(PBool.EBoolState.TrueThisFrame);
                    player.currentUpdateRoundMoney = newMoney;

                    player.SetRoundMoney_Rpc(newMoney, RpcTarget.Single(player.id, RpcTargetUse.Temp));
                }
            }
        }
    }

    [ClientRpc]
    protected void ShareNewPlayerTeam_ClientRpc(ulong entityID, PlayerTeam team)
    {
        var obj = GetNetworkObject(entityID);

        if (obj)
        {
            var playerComp = obj.GetComponent<AMSPlayer>();

            if (playerComp)
            {
                playerComp.currentTeam = team;
                playerComp.hasChangedTeam = new PBool(PBool.EBoolState.TrueThisFrame);
            }
            else
                DebugClass.Log("Object didnt have PlayerComponent");
        }
        else
            DebugClass.Log("Object id was invalid");
    }


    [ClientRpc]
    protected void SwitchPlayerToTeam_ClientRpc(ulong pID, PlayerTeam team)
    {
        var curTeam = FindPlayerTeam(pID);

        if (curTeam == team)
        {
            DebugClass.Log("Same Team, Skipping Change, Team: " + team.ToString());
            return;
        }

        if (curTeam != PlayerTeam.None)
        {
            players.Remove(pID);
            teams[curTeam].Remove(pID);
        }

        if (team != PlayerTeam.None)
        {
            players.Add(pID, team);
            teams[team].Add(pID);
        }
    }

    protected void BalanceTeams()
    {
        GetPlayerList();
    }

    protected PlayerTeam FindPlayerTeam(ulong pID)
    {
        if (players.TryGetValue(pID, out PlayerTeam team))
            return team;

        return PlayerTeam.None;
    }

    protected override void OnPlayerJoined(Player player)
    {
        //connectedPlayers.Add()
    }

    protected override void OnPlayerLeft(Player player)
    {
        //SwitchPlayerToTeam_ClientRpc(player.id, PlayerTeam.None);
        if(!FindPlayer(player.id))
            connectedPlayers.Remove(player.id);
    }

    //Dont Change or Override Players from List, READ ONLY!
    //Server Call only!, else List is Empty
    public Dictionary<ulong, AMSPlayer> GetConnectedPlayers()
    {
        return connectedPlayers;
    }

    public Dictionary<PlayerTeam, List<ulong>> GetTeamPlayerList()
    {
        return teams;
    }

    public bool HasGameStarted()
    {
        return hasGameStarted;
    }

    public AMSPlayer FindConnectedPlayer(ulong pID)
    {
        if (connectedPlayers.TryGetValue(pID, out var player))
        {
            return player;
        }

        return null;
    }

    public bool HasConnctedPlayer(ulong pID)
    {
        return connectedPlayers.ContainsKey(pID);
    }
}
