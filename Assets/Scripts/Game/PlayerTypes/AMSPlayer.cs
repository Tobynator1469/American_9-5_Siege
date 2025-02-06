using Unity.Netcode;
using UnityEngine.LowLevel;
public struct AMSPlayerData : INetworkSerializable
{
    public PlayerUpdateData baseData;

    public char hasWon;
    public char isInSafeZone;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsReader)
        {
            serializer.SerializeValue(ref baseData);
            serializer.SerializeValue(ref hasWon);
            serializer.SerializeValue(ref isInSafeZone);
        }
        else
        {
            serializer.SerializeValue(ref baseData);
            serializer.SerializeValue(ref hasWon);
            serializer.SerializeValue(ref isInSafeZone);
        }
    }
}

public abstract class AMSPlayer : Player
{
    public delegate void OnChangedSafeZoneState(Player _this, bool isInSafeZone);
    public delegate void OnGameResultState(Player _this, bool hasWon);

    protected OnChangedSafeZoneState onChangedSafeZoneState = null;
    protected OnGameResultState onGameResultState = null;
    public void BindOnChangedSafeZoneState(OnChangedSafeZoneState onChangedBombHoldState) { this.onChangedSafeZoneState += onChangedBombHoldState; }
    public void BindOnGameResultState(OnGameResultState onGameResultState) { this.onGameResultState += onGameResultState; }
    public void UnbindOnChangedSafeZoneState(OnChangedSafeZoneState onChangedBombHoldState) { this.onChangedSafeZoneState -= onChangedBombHoldState; }
    public void UnbindOnGameResultState(OnGameResultState onGameResultState) { this.onGameResultState -= onGameResultState; }

    public int offshoreMoney = 0;
    public int roundMoney = 0;

    public PBool isInSafeZone = new PBool(false);
    public PBool hasWon = new PBool(false);

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

    protected AMSPlayerData CraftAMSPlayerData()
    {
        AMSPlayerData amsPlayerData = new AMSPlayerData();

        amsPlayerData.baseData = CraftPlayerUpdateData();

        return amsPlayerData;
    }
}
