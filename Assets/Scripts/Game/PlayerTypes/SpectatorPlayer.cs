using Unity.Netcode;
using UnityEngine;

public class SpectatorPlayer : AMSPlayer
{
    [ServerRpc]
    protected override void OnNetworkRequestUpdateData_ServerRpc()
    {
        OnNetworkUpdateData_ClientRpc(CraftSpectatorPlayerUpdateData());
    }

    [ClientRpc]
    private void OnNetworkUpdateData_ClientRpc(AMSPlayerData data_)
    {
        UnpackServerData_AMSPlayer(data_);

        if (onUpdatePlayer != null)
            onUpdatePlayer(this);
    }

    private AMSPlayerData CraftSpectatorPlayerUpdateData()
    {
        AMSPlayerData dataOut = new AMSPlayerData();

        dataOut = CraftAMSPlayerData();

        return dataOut;
    }
}
