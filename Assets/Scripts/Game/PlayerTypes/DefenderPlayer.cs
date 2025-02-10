using Unity.Netcode;
using UnityEngine;

struct DefenderPlayerData : INetworkSerializable
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

public class DefenderPlayer : AMSPlayer
{
    [ServerRpc]
    protected override void DeframeBools_ServerRpc()
    {
        this.isInSafeZone.DeframeBool();
        this.hasWon.DeframeBool();
        this.isGamePendingStart.DeframeBool();
        this.hasRoundMoneyUpdated.DeframeBool();
        this.hasChangedTeam.DeframeBool();
    }

    [ServerRpc]
    protected override void OnNetworkRequestUpdateData_ServerRpc()
    {
        OnNetworkUpdateData_ClientRpc(CraftDefenderPlayerUpdateData());
    }

    [ClientRpc]
    private void OnNetworkUpdateData_ClientRpc(DefenderPlayerData data_)
    {
        UnpackServerData_AMSPlayer(data_.baseData);

        if (onUpdatePlayer != null)
            onUpdatePlayer(this);
    }

    private DefenderPlayerData CraftDefenderPlayerUpdateData()
    {
        DefenderPlayerData dataOut = new DefenderPlayerData();

        dataOut.baseData = CraftAMSPlayerData();

        return dataOut;
    }
}
