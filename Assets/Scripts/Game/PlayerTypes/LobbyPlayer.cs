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
    }

    [ServerRpc]
    protected override void OnNetworkRequestUpdateData_ServerRpc()
    {
        OnNetworkUpdateData_ClientRpc(CraftAMSPlayerData());
    }

    [ClientRpc]
    private void OnNetworkUpdateData_ClientRpc(AMSPlayerData data_)
    {
        PlayerUpdateData data = data_.baseData;
        AMSPlayerData psData = data_;

        if (this.playerRigidBody)
        {
            this.playerRigidBody.position = data.position;
            this.playerRigidBody.linearVelocity = data.velocity;
        }

        this.currentStamina = data.stamina;

        PBool livingState = new PBool(data.isAlive);
        PBool SafeZoneState = new PBool(psData.isInSafeZone);
        PBool wonState = new PBool(psData.hasWon);

        switch (livingState.GetState())
        {
            case PBool.EBoolState.FalseThisFrame:
                if (onChangedLivingState != null)
                    onChangedLivingState(this, false);

                isAlive = new PBool(PBool.EBoolState.False);
                break;

            case PBool.EBoolState.TrueThisFrame:
                if (onChangedLivingState != null)
                    onChangedLivingState(this, true);

                isAlive = new PBool(PBool.EBoolState.True);
                break;
        }

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

        if (onUpdatePlayer != null)
            onUpdatePlayer(this);
    }
}
