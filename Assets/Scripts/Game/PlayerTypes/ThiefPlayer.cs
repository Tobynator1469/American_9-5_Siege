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
    [SerializeField]
    public GameObject[] itemHands = new GameObject[2];

    public int lockpicks = 0;

    [ServerRpc]
    protected override void DeframeBools_ServerRpc()
    {
        this.isInSafeZone.DeframeBool();
        this.hasWon.DeframeBool();
    }

    [ServerRpc]
    protected override void OnNetworkRequestUpdateData_ServerRpc()
    {
        OnNetworkUpdateData_ClientRpc(CraftThiefPlayerUpdateData());
    }

    [ClientRpc]
    private void OnNetworkUpdateData_ClientRpc(ThiefPlayerData data_)
    {
        PlayerUpdateData data = data_.baseData.baseData;
        AMSPlayerData psData = data_.baseData;

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

    private ThiefPlayerData CraftThiefPlayerUpdateData()
    {
        ThiefPlayerData dataOut = new ThiefPlayerData();

        dataOut.baseData = CraftAMSPlayerData();

        dataOut.baseData.isInSafeZone = (isInSafeZone.GetCharState());
        dataOut.baseData.hasWon = (hasWon.GetCharState());

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
