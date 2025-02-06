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

    Dictionary<ulong,  PlayerTeam> teams;

    private float timeTillStart = 0.0f;
    private float timeTillStartCur = 0.0f;

    private bool startGame = false;
    private bool hasGameStarted = false;

    [ServerRpc]
    protected override void OnStartGameBtnServerRpc()
    {
        if(!startGame && !hasGameStarted)
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

    [ServerRpc]
    private void SpawnPlayer_ServerRpc(PlayerTeam team, ulong id)
    {
        string playerName = FindPlayer(id).playerName;

        DestroyPlayer_ServerRpc(id); //Destroy Instance before instantiating another one

        var transform = GetRandomSpawnLocation();

        GameObject instance = null;

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
                break;

            default:
                return;
        }

        var netObjComp = instance.GetComponent<NetworkObject>();
        var playerComp = netObjComp.GetComponent<Player>();

        IntializeDefaults_PlayerComp(playerComp);

        playerComp.BindOnDestroy(OnPlayerDestroyed);

        playerComp.playerName = playerName;

        ShareNewNameClientRpc(netObjComp.NetworkObjectId, playerName);

        netObjComp.Spawn(true);

        CreateLocalPlayerRpc(netObjComp.NetworkObjectId, RpcTarget.Single(playerComp.id, RpcTargetUse.Temp));
    }

    [ServerRpc]
    private void OnGameStarted_ServerRpc()
    {
        foreach (var item in teams)
        {
            SpawnPlayer_ServerRpc(item.Value, item.Key);
        }
    }

    [Rpc(SendTo.SpecifiedInParams)]
    public void SwitchTeams_Rpc(PlayerTeam teamSelected, RpcParams rpcParams = default)
    {
        var pID = rpcParams.Receive.SenderClientId;

        if (!hasGameStarted)
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
}
