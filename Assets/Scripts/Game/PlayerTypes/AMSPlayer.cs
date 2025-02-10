using Assets.Scripts;
using System;
using System.Data;
using System.Diagnostics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.LowLevel;
public struct AMSPlayerData : INetworkSerializable
{
    public PlayerUpdateData baseData;

    public char hasWon;
    public char isInSafeZone;
    public char isGamePendingStart;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsReader)
        {
            serializer.SerializeValue(ref baseData);
            serializer.SerializeValue(ref hasWon);
            serializer.SerializeValue(ref isInSafeZone);
            serializer.SerializeValue(ref isGamePendingStart);
        }
        else
        {
            serializer.SerializeValue(ref baseData);
            serializer.SerializeValue(ref hasWon);
            serializer.SerializeValue(ref isInSafeZone);
            serializer.SerializeValue(ref isGamePendingStart);
        }
    }
}

public abstract class AMSPlayer : Player
{
    public delegate void OnChangedSafeZoneState(Player _this, bool isInSafeZone);
    public delegate void OnGameResultState(Player _this, bool hasWon);
    public delegate void OnGamePendingState(AMSPlayer _this, bool isGamePendingStart);

    protected OnChangedSafeZoneState onChangedSafeZoneState = null;
    protected OnGameResultState onGameResultState = null;
    protected OnGamePendingState onGamePendingState = null;
    public void BindOnChangedSafeZoneState(OnChangedSafeZoneState onChangedBombHoldState) { this.onChangedSafeZoneState += onChangedBombHoldState; }
    public void BindOnGameResultState(OnGameResultState onGameResultState) { this.onGameResultState += onGameResultState; }
    public void BindOnGamePendingState(OnGamePendingState onGamePendingState) { this.onGamePendingState += onGamePendingState; }
    public void UnbindOnChangedSafeZoneState(OnChangedSafeZoneState onChangedBombHoldState) { this.onChangedSafeZoneState -= onChangedBombHoldState; }
    public void UnbindOnGameResultState(OnGameResultState onGameResultState) { this.onGameResultState -= onGameResultState; }
    public void UnbindOnGamePendingState(OnGamePendingState onGamePendingState) { this.onGamePendingState -= onGamePendingState; }

    [SerializeField]
    private float raycastDistance = 4.0f;

    public int offshoreMoney = 0;
    public int roundMoney = 0;

    public PBool isInSafeZone = new PBool(false);
    public PBool hasWon = new PBool(false);
    public PBool isGamePendingStart = new PBool(false);

    private bool canInteract = true;

    [ServerRpc]
    public void PlayerChangeSafeZoneState_ServerRpc(bool isInSafeZone)
    {
        if (isInSafeZone)
            this.isInSafeZone = new PBool(PBool.EBoolState.TrueThisFrame);
        else
            this.isInSafeZone = new PBool(PBool.EBoolState.FalseThisFrame);
    }

    [ServerRpc]
    public void PlayerGameResult_ServerRpc(bool hasWon)
    {
        if (hasWon)
            this.hasWon = new PBool(PBool.EBoolState.TrueThisFrame);
        else
            this.hasWon = new PBool(PBool.EBoolState.FalseThisFrame);
    }

    [ServerRpc]
    public void SetPendingGameStart_ServerRpc(bool isGamePendingStart)
    {
        if (isGamePendingStart)
            this.isGamePendingStart = new PBool(PBool.EBoolState.TrueThisFrame);
        else
            this.isGamePendingStart = new PBool(PBool.EBoolState.FalseThisFrame);
    }

    [Rpc(SendTo.Server)]
    protected void RequestOffshoreMoney_ServerRpc(RpcParams rpcParams = default) // Nicht fertig
    {
        if (!CheckAuthority(rpcParams))
            return;

        int _offshoreMoney = 0;

        if(this.serverManager)
        {
            //Call Server Manager to get Offshore Value from Sql Server

            var srvManager = this.serverManager.GetComponent<AMServerManger>();
        }

        OnGatheredOffshoreMoney_Rpc(_offshoreMoney, RpcTarget.Single(this.id, RpcTargetUse.Temp));
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void OnGatheredOffshoreMoney_Rpc(int offshore, RpcParams rpcParams)
    {
        if (!CheckServerAuthority(rpcParams))
            return;

        offshoreMoney = offshore;
    }

    [Rpc(SendTo.Server)]
    public void OnInteract_Rpc(float YDirection, RpcParams rpcParams = default)
    {
        if (!CheckAuthority(rpcParams) && YDirection < -1.0f || YDirection > 1.0f)
            return;

        var srvManager = serverManager.GetComponent<AMServerManger>();

        srvManager.OnPlayerTryInteract_ServerRpc(id, YDirection);
    }

    [Rpc(SendTo.Server)]
    public void OnSwitchTeams_Rpc(PlayerTeam team, RpcParams rpcParams = default)
    {
        if (!CheckAuthority(rpcParams))
            return;

        DebugClass.Log("Switching Teams!");

        var srvManager = serverManager.GetComponent<AMServerManger>();

        srvManager.OnPlayerSwitchTeams_ServerRpc(id, team);
    }

    protected AMSPlayerData CraftAMSPlayerData()
    {
        AMSPlayerData amsPlayerData = new AMSPlayerData();

        amsPlayerData.baseData = CraftPlayerUpdateData();

        amsPlayerData.isInSafeZone = (isInSafeZone.GetCharState());
        amsPlayerData.hasWon = (hasWon.GetCharState());
        amsPlayerData.isGamePendingStart = (isGamePendingStart.GetCharState()); ;

        return amsPlayerData;
    }

    protected void UnpackServerData_AMSPlayer(AMSPlayerData psData)
    {
        UnpackServerData_PlayerData(psData.baseData);

        PBool SafeZoneState = new PBool(psData.isInSafeZone);
        PBool wonState = new PBool(psData.hasWon);
        PBool isGamePendingStart = new PBool(psData.isGamePendingStart);

        switch (SafeZoneState.GetState())
        {
            case PBool.EBoolState.FalseThisFrame:
                if (onChangedSafeZoneState != null)
                    onChangedSafeZoneState(this, false);

                isInSafeZone = new PBool(PBool.EBoolState.False);
                break;

            case PBool.EBoolState.TrueThisFrame:
                if (onChangedSafeZoneState != null)
                    onChangedSafeZoneState(this, true);

                isInSafeZone = new PBool(PBool.EBoolState.True);
                break;
        }

        switch (wonState.GetState())
        {
            case PBool.EBoolState.FalseThisFrame:
                if (onGameResultState != null)
                    onGameResultState(this, false);

                wonState = new PBool(PBool.EBoolState.False);
                break;

            case PBool.EBoolState.TrueThisFrame:
                if (onGameResultState != null)
                    onGameResultState(this, true);

                wonState = new PBool(PBool.EBoolState.True);
                break;
        }

        switch (isGamePendingStart.GetState())
        {
            case PBool.EBoolState.FalseThisFrame:
                if (onGamePendingState != null)
                    onGamePendingState(this, false);

                isGamePendingStart = new PBool(PBool.EBoolState.False);
                break;

            case PBool.EBoolState.TrueThisFrame:
                if (onGamePendingState != null)
                    onGamePendingState(this, true);

                isGamePendingStart = new PBool(PBool.EBoolState.True);
                break;
        }
    }

    [ServerRpc]
    public void SetRaycastDist_ServerRpc(float Dist)
    {
        raycastDistance = Dist;
    }

    public bool CanInteract()
    {
        return canInteract;
    }

    public float GetRaycastDist()
    {
        return raycastDistance;
    }
}
