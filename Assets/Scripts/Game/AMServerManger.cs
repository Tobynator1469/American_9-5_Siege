using Assets.Scripts;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum PlayerTeam
{
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
    private GameObject SpectatorSpawnLocation = null;

    Dictionary<ulong,  PlayerTeam> teams = new Dictionary<ulong, PlayerTeam>();
    Dictionary<ulong, AMSPlayer> connectedPlayers = new Dictionary<ulong, AMSPlayer>();

    private float defaultRaycastDistance = 4.0f;

    private float timeTillStart = 4.0f;
    private float timeTillStartCur = 0.0f;

    private int InteractableLayer = 0;

    private bool startGame = false;
    private bool hasGameStarted = false;

    protected override void OnStartServer()
    {
        InteractableLayer = 1 << LayerMask.NameToLayer("Interactable");
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

            RemovePlayer(playerInstance);
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

            OnInitializeAMS_PlayerDefaults(amsCast);

            if (!connectedPlayers.ContainsKey(id))
                connectedPlayers.Add(id, amsCast);
        }

        playerComp.BindOnDestroy(OnPlayerDestroyed);

        playerComp.id = id;

        playerComp.playerName = playerName;

        netObjComp.Spawn(true);

        CreateLocalPlayerRpc(netObjComp.NetworkObjectId, RpcTarget.Single(id, RpcTargetUse.Temp));

        ShareNewNameClientRpc(netObjComp.NetworkObjectId, playerName);
    }

    [ServerRpc]
    private void OnGameStarted_ServerRpc()
    {
        foreach (var item in teams)
        {
            SpawnPlayer_ServerRpc(item.Value, item.Key);
        }
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
                    interactable.Interact_ServerRpc(ID);
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
            DebugClass.Log("Unauthorized Team switch, from player with ID: " + pID);
            return;
        }

        if(teams.ContainsKey(pID))
        {
            teams[pID] = teamSelected;
        }
        else
        {
            teams.Add(pID, teamSelected);
        }
    }

    protected AMSPlayer FindConnectedPlayer(ulong ID)
    {
        if(connectedPlayers.TryGetValue(ID, out var player))
        { 
            return player;
        }

        return null;
    }
}
