using Assets.Scripts;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum PlayerTeam
{
    None,
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

    [SerializeField, Tooltip("Set the Specific Game State for the Scene!")]
    private GameObject GameStatePrefab = null;

    [SerializeField]
    private GameObject SpectatorSpawnLocation = null;

    private AMS_GameState gameState = null; //Current Game State

    Dictionary<ulong, AMSPlayer> connectedPlayers = new Dictionary<ulong, AMSPlayer>();

    Dictionary<PlayerTeam, List<ulong>> teams = new()
    { { PlayerTeam.Thief, new List<ulong>() }, { PlayerTeam.Defender, new List<ulong>() }, { PlayerTeam.Spectator, new List<ulong>() } };

    Dictionary<ulong, PlayerTeam> players = new Dictionary<ulong, PlayerTeam>();

    private float defaultRaycastDistance = 4.0f;

    private float timeTillStart = 4.0f;
    private float timeTillStartCur = 0.0f;

    private int InteractableLayer = 0;

    private bool startGame = false;
    private bool hasGameStarted = false;

    public delegate void OnAMServerManagerDestroyed(AMServerManger sv);

    private OnAMServerManagerDestroyed onAMServerManagerDestroyed = null;

    public void BindOnAMServerManagerDestroyed(OnAMServerManagerDestroyed onAMServerManagerDestroyed) 
    {
        this.onAMServerManagerDestroyed += onAMServerManagerDestroyed;
    }

    public void UnbindOnAMServerManagerDestroyed(OnAMServerManagerDestroyed onAMServerManagerDestroyed)
    {
        this.onAMServerManagerDestroyed -= onAMServerManagerDestroyed;
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
        foreach (var connected in connectedPlayers)
        {
            connected.Value.SetPendingGameStart_ServerRpc(true);
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
    private void SpawnPlayer_ServerRpc(PlayerTeam team, ulong id)
    {
        string playerName = FindPlayer(id).playerName;

        DestroyPlayer_ServerRpc(id); //Destroy Instance before instantiating another one

        var transform = GetRandomSpawnLocation();

        GameObject instance = null;

        bool isSpectator = false;

        switch (team)
        {
            case PlayerTeam.Thief:
                instance = Instantiate(ThiefPrefab, transform.position, transform.rotation);
                break;
            case PlayerTeam.Defender:
                instance = Instantiate(DefenderPrefab, transform.position, transform.rotation);
                break;
            case PlayerTeam.Spectator:
                instance = Instantiate(SpectatorPrefab, transform.position, transform.rotation);
                isSpectator = true;
                break;

            default:
                return;
        }

        var netObjComp = instance.GetComponent<NetworkObject>();
        var playerComp = netObjComp.GetComponent<Player>();

        IntializeDefaults_PlayerComp(playerComp);

        if(!isSpectator)
        {
            var amsCast = (AMSPlayer)playerComp;

            //OnInitializeAMS_PlayerDefaults(amsCast);

            if (!connectedPlayers.ContainsKey(id))
                connectedPlayers.Add(id, amsCast);
        }

        playerComp.BindOnDestroy(OnPlayerDestroyed);

        playerComp.id = id;

        playerComp.playerName = playerName;

        netObjComp.Spawn(true);

        CreateLocalPlayerRpc(netObjComp.NetworkObjectId, RpcTarget.Single(id, RpcTargetUse.Temp));

        ShareNewNameClientRpc(netObjComp.NetworkObjectId, playerName);

        ShareNewPlayerTeam_ClientRpc(netObjComp.NetworkObjectId, team);

        SwitchPlayerToTeam_ClientRpc(id, team);
    }

    [ServerRpc]
    private void OnGameStarted_ServerRpc()
    {
        var playerEntries = new List<KeyValuePair<ulong, PlayerTeam>>(players);

        foreach (var item in playerEntries)
        {
            SpawnPlayer_ServerRpc(item.Value, item.Key);
        }
    }

    private void SpawnGameState()
    {
        GameObject instance = Instantiate(GameStatePrefab, Vector3.zero, Quaternion.Euler(Vector3.zero));

        var gameStateObj = instance.GetComponent<AMS_GameState>();

        if (gameStateObj)
        {
            gameStateObj.BindOnGameStateDestroyed(OnGameStateDestroyed);
        }
        else
        {
            Destroy(instance);

            DebugClass.Error("Prefab was not an GameState!");
        }
    }

    private void OnGameStateDestroyed(AMS_GameState gameState)
    {

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

    [ServerRpc]
    public void OnPlayerSwitchTeams_ServerRpc(ulong ID, PlayerTeam teamSelected)
    {
        var pID = ID;

        if (hasGameStarted)
        {
            //DebugClass.Log("Unauthorized Team switch, from player with ID: " + pID);
            return;
        }

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
                    player.hasRoundMoneyUpdated = new PBool(PBool.EBoolState.TrueThisFrame);
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

    protected AMSPlayer FindConnectedPlayer(ulong pID)
    {
        if(connectedPlayers.TryGetValue(pID, out var player))
        { 
            return player;
        }

        return null;
    }

    [ClientRpc]
    protected void SwitchPlayerToTeam_ClientRpc(ulong pID, PlayerTeam team)
    {
        var curTeam = FindPlayerTeam(pID);

        if (curTeam == team) 
            return;

        if (curTeam != PlayerTeam.None)
        {
            players.Remove(pID);
            teams[curTeam].Remove(pID);
        }

        if(team != PlayerTeam.None)
        {
            players.Add(pID, team);
            teams[team].Add(pID);
        }
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
}
