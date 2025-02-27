using Unity.Netcode;
using UnityEngine;
using static ThiefPlayer;

public class LobbyPlayer : AMSPlayer
{
    [ServerRpc]
    protected override void DeframeBools_ServerRpc()
    {
        this.isInSafeZone.DeframeBool();
        this.hasWon.DeframeBool();
        this.gameIntermission.DeframeBool();
    }

    [ServerRpc]
    protected override void OnNetworkRequestUpdateData_ServerRpc()
    {
        OnNetworkUpdateData_ClientRpc(CraftAMSPlayerData());
    }

    [ClientRpc]
    private void OnNetworkUpdateData_ClientRpc(AMSPlayerData data_)
    {
        this.UnpackServerData_AMSPlayer(data_);

        if (onUpdatePlayer != null)
            onUpdatePlayer(this);
    }
}
