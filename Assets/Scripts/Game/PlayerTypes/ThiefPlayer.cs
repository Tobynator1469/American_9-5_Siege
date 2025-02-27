using Unity.Netcode;
using UnityEngine;

struct ThiefPlayerData : INetworkSerializable
{
    public AMSPlayerData baseData;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsReader)
        {
            serializer.SerializeValue(ref baseData);
            //serializer.SerializeValue(ref hasWon);
            //serializer.SerializeValue(ref isInSafeZone);
        }
        else
        {
            serializer.SerializeValue(ref baseData);
            //serializer.SerializeValue(ref hasWon);
            //serializer.SerializeValue(ref isInSafeZone);
        }
    }
}

public class ThiefPlayer : AMSPlayer
{
    public int lockpicks = 0;

    [ServerRpc]
    protected override void DeframeBools_ServerRpc()
    {
        this.isInSafeZone.DeframeBool();
        this.hasWon.DeframeBool();
        this.isGamePendingStart.DeframeBool();
        this.hasRoundMoneyUpdated.DeframeBool();
        this.hasChangedTeam.DeframeBool();
        this.gameIntermission.DeframeBool();
    }

    [ServerRpc]
    protected override void OnNetworkRequestUpdateData_ServerRpc()
    {
        OnNetworkUpdateData_ClientRpc(CraftThiefPlayerUpdateData());
    }

    [ClientRpc]
    private void OnNetworkUpdateData_ClientRpc(ThiefPlayerData data_)
    {
        UnpackServerData_AMSPlayer(data_.baseData);

        if (onUpdatePlayer != null)
            onUpdatePlayer(this);
    }

    private ThiefPlayerData CraftThiefPlayerUpdateData()
    {
        ThiefPlayerData dataOut = new ThiefPlayerData();

        dataOut.baseData = CraftAMSPlayerData();

        return dataOut;
    }


    [Rpc(SendTo.Server)]
    public void PlayerTryLockPick_ServerRpc(RpcParams rpcParams = default)
    {
        if (!CheckAuthority(rpcParams) || !CanLockPick())
            return;


    }

    private bool CanLockPick()
    {
        return lockpicks > 0;
    }

    public bool IsObjectLockPickable(/*LockPickable obj*/)
    {
        return true;
    }
}
