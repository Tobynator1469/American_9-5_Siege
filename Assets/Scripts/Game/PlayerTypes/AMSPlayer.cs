using Assets.Scripts;
using Unity.Netcode;
using UnityEngine;

public struct ItemSlot
{
    public AMS_Item item;
    public Transform positionToStore;
    public bool hasItem;

    public ItemSlot(Transform positionToStore)
    {
        this.item = null;
        this.positionToStore = positionToStore;

        hasItem = false;
    }
}
public struct AMSPlayerData : INetworkSerializable
{
    public PlayerUpdateData baseData;

    public char hasWon;
    public char isInSafeZone;
    public char isGamePendingStart;
    public char hasRoundMoneyUpdated;
    public char hasChangedTeam;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsReader)
        {
            serializer.SerializeValue(ref baseData);
            serializer.SerializeValue(ref hasWon);
            serializer.SerializeValue(ref isInSafeZone);
            serializer.SerializeValue(ref isGamePendingStart);
            serializer.SerializeValue(ref hasRoundMoneyUpdated);
            serializer.SerializeValue(ref hasChangedTeam);
        }
        else
        {
            serializer.SerializeValue(ref baseData);
            serializer.SerializeValue(ref hasWon);
            serializer.SerializeValue(ref isInSafeZone);
            serializer.SerializeValue(ref isGamePendingStart);
            serializer.SerializeValue(ref hasRoundMoneyUpdated);
            serializer.SerializeValue(ref hasChangedTeam);
        }
    }
}

public abstract class AMSPlayer : Player
{
    public delegate void OnChangedSafeZoneState(Player _this, bool isInSafeZone);
    public delegate void OnGameResultState(Player _this, bool hasWon);
    public delegate void OnGamePendingState(AMSPlayer _this, bool isGamePendingStart);
    public delegate void OnGameChangedRoundMoney(AMSPlayer _this, int ChangedValue);
    public delegate void OnChangedTeam(AMSPlayer _this, PlayerTeam team);

    protected OnChangedSafeZoneState onChangedSafeZoneState = null;
    protected OnGameResultState onGameResultState = null;
    protected OnGamePendingState onGamePendingState = null;
    protected OnGameChangedRoundMoney onGameChangedRoundMoney = null;
    protected OnChangedTeam onChangedTeam = null;
    public void BindOnChangedSafeZoneState(OnChangedSafeZoneState onChangedBombHoldState) { this.onChangedSafeZoneState += onChangedBombHoldState; }
    public void BindOnGameResultState(OnGameResultState onGameResultState) { this.onGameResultState += onGameResultState; }
    public void BindOnGamePendingState(OnGamePendingState onGamePendingState) { this.onGamePendingState += onGamePendingState; }
    public void BindOnGameChangedRoundMoney(OnGameChangedRoundMoney onGameAddedRoundMoney) { this.onGameChangedRoundMoney += onGameAddedRoundMoney; }
    public void BindOnChangedTeam(OnChangedTeam onChangedTeam) { this.onChangedTeam += onChangedTeam; }
    public void UnbindOnChangedSafeZoneState(OnChangedSafeZoneState onChangedBombHoldState) { this.onChangedSafeZoneState -= onChangedBombHoldState; }
    public void UnbindOnGameResultState(OnGameResultState onGameResultState) { this.onGameResultState -= onGameResultState; }
    public void UnbindOnGamePendingState(OnGamePendingState onGamePendingState) { this.onGamePendingState -= onGamePendingState; }
    public void UnbindOnGameChangedRoundMoney(OnGameChangedRoundMoney onGameAddedRoundMoney) { this.onGameChangedRoundMoney -= onGameAddedRoundMoney; }
    public void UnbindOnChangedTeam(OnChangedTeam onChangedTeam) { this.onChangedTeam -= onChangedTeam; }

    public PlayerTeam currentTeam = PlayerTeam.None;

    [SerializeField]
    private Transform[] holdingHandspots = null;

    private ItemSlot[] holdingHand = new ItemSlot[6]; //the hands where stuff gets hold

    [SerializeField]
    private float raycastDistance = 4.0f;

    public int offshoreMoney = 0;
    public int playerHealth = 0;

    public int activeSlot = 0;

    public int lastUpdateRoundMoney = 0;
    public int currentUpdateRoundMoney = 0;

    public PBool isInSafeZone = new PBool(false);
    public PBool hasWon = new PBool(false);
    public PBool isGamePendingStart = new PBool(false);
    public PBool hasRoundMoneyUpdated = new PBool(false);
    public PBool hasChangedTeam = new PBool(false);
    public PBool hasBeenKnocked = new PBool(false);

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

    [ServerRpc]
    public void SetRaycastDist_ServerRpc(float Dist)
    {
        raycastDistance = Dist;
    }

    [ServerRpc]
    public void SetRoundMoney_ServerRpc(int money)
    {
        currentUpdateRoundMoney = money;

        this.serverManager.GetComponent<AMServerManger>().PlayerRoundMoneyHasUpdated_ServerRpc(id, money);
    }

    [ServerRpc]
    public void DamagePlayer_ServerRpc(int damage)
    {
        if (hasBeenKnocked.GetBool())
            return;

        playerHealth -= damage;

        if (playerHealth < 0)
        {
            hasBeenKnocked = new PBool(PBool.EBoolState.TrueThisFrame); //Tell owning player hes knocked7

            this.serverManager.GetComponent<AMServerManger>().PlayerKnockedOut(this); //Spawn Effects and tell Clients they can Arrest
        }
    }

    public void PickupItem(int slot, AMS_Item itemOnGround)
    {
        if(CanPickupItem(slot))
        {
            holdingHand[slot].hasItem = true;

            itemOnGround.transform.SetParent(holdingHand[slot].positionToStore);
        }
    }

    [Rpc(SendTo.SpecifiedInParams)]
    public void SetRoundMoney_Rpc(int money, RpcParams rpcParams)
    {
        currentUpdateRoundMoney = money;
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

        if (GetIsValidSlot(activeSlot) && holdingHand[activeSlot].hasItem)
        {
            holdingHand[activeSlot].item.Interact_ServerRpc(id, Vector3.zero);
        }
        else
        {
            srvManager.OnPlayerTryInteract_ServerRpc(id, YDirection);
        }
    }

    [Rpc(SendTo.Server)]
    public void ActivateItem_Rpc(int slot, RpcParams rpcParams = default)
    {
        if (!CheckServerAuthority(rpcParams))
            return;

        if(GetIsValidSlot(slot))
        {
            activeSlot = slot;

            if (holdingHand[activeSlot].hasItem)
                holdingHand[activeSlot].item.SetItemActive_ServerRpc(true);
        }
    }

    [Rpc(SendTo.Server)]
    public void DropItem_Rpc(RpcParams rpcParams = default)
    {
        if (!CheckAuthority(rpcParams))
            return;

        DropItem_ServerRpc();
    }

    [Rpc(SendTo.Server)]
    public void OnSwitchTeams_Rpc(PlayerTeam team, RpcParams rpcParams = default)
    {
        if (!CheckAuthority(rpcParams) || !IsValidTeam(team))
            return;

        DebugClass.Log("Switching Teams!");

        var srvManager = serverManager.GetComponent<AMServerManger>();

        if (srvManager.HasGameStarted())
            return;

        srvManager.OnPlayerSwitchTeams_ServerRpc(id, team);
    }

    [ServerRpc]
    public void DropItem_ServerRpc()
    {
        if(GetIsValidSlot(activeSlot))
        {
            holdingHand[activeSlot].item.DropItem_ServerRpc();
            holdingHand[activeSlot].item = null;

            holdingHand[activeSlot].hasItem = false;
        }
    }

    protected override void IntializePlayer()
    {
        var length = holdingHandspots.Length;

        if (holdingHandspots.Length > holdingHand.Length)
            length = holdingHand.Length;

        for (int i = 0; i < length; i++)
        {
            holdingHand[i].positionToStore = holdingHandspots[i];
        }
    }

    [ClientRpc]
    private void OnPlayerAMS_Knocked_ClientRpc()
    {

    }

    protected AMSPlayerData CraftAMSPlayerData()
    {
        AMSPlayerData amsPlayerData = new AMSPlayerData();

        amsPlayerData.baseData = CraftPlayerUpdateData();

        amsPlayerData.isInSafeZone = (isInSafeZone.GetCharState());
        amsPlayerData.hasWon = (hasWon.GetCharState());
        amsPlayerData.isGamePendingStart = (isGamePendingStart.GetCharState());
        amsPlayerData.hasRoundMoneyUpdated = (hasRoundMoneyUpdated.GetCharState());
        amsPlayerData.hasChangedTeam = (hasChangedTeam.GetCharState());

        return amsPlayerData;
    }

    protected void UnpackServerData_AMSPlayer(AMSPlayerData psData)
    {
        UnpackServerData_PlayerData(psData.baseData);

        PBool SafeZoneState = new PBool(psData.isInSafeZone);
        PBool wonState = new PBool(psData.hasWon);
        PBool isGamePendingStart = new PBool(psData.isGamePendingStart);
        PBool hasRoundMoneyUpdated = new PBool(psData.hasRoundMoneyUpdated);
        PBool hasChangedTeam = new PBool(psData.hasChangedTeam);

        switch (SafeZoneState.GetState())
        {
            case PBool.EBoolState.FalseThisFrame:
                if (onChangedSafeZoneState != null)
                    onChangedSafeZoneState(this, false);

                this.isInSafeZone = new PBool(PBool.EBoolState.False);
                break;

            case PBool.EBoolState.TrueThisFrame:
                if (onChangedSafeZoneState != null)
                    onChangedSafeZoneState(this, true);

                this.isInSafeZone = new PBool(PBool.EBoolState.True);
                break;
        }

        switch (wonState.GetState())
        {
            case PBool.EBoolState.FalseThisFrame:
                if (onGameResultState != null)
                    onGameResultState(this, false);

                this.hasWon = new PBool(PBool.EBoolState.False);
                break;

            case PBool.EBoolState.TrueThisFrame:
                if (onGameResultState != null)
                    onGameResultState(this, true);

                this.hasWon = new PBool(PBool.EBoolState.True);
                break;
        }

        switch (isGamePendingStart.GetState())
        {
            case PBool.EBoolState.FalseThisFrame:
                if (onGamePendingState != null)
                    onGamePendingState(this, false);

                this.isGamePendingStart = new PBool(PBool.EBoolState.False);
                break;

            case PBool.EBoolState.TrueThisFrame:
                if (onGamePendingState != null)
                    onGamePendingState(this, true);

                this.isGamePendingStart = new PBool(PBool.EBoolState.True);
                break;
        }

        switch (hasRoundMoneyUpdated.GetState())
        {
            case PBool.EBoolState.FalseThisFrame:
                if (onGameChangedRoundMoney != null)
                    onGameChangedRoundMoney(this, this.currentUpdateRoundMoney - this.lastUpdateRoundMoney);

                this.hasRoundMoneyUpdated = new PBool(PBool.EBoolState.False);
                break;

            case PBool.EBoolState.TrueThisFrame:
                if (onGameChangedRoundMoney != null)
                    onGameChangedRoundMoney(this, this.currentUpdateRoundMoney - this.lastUpdateRoundMoney);

                this.hasRoundMoneyUpdated = new PBool(PBool.EBoolState.True);
                break;
        }

        switch (hasChangedTeam.GetState())
        {
            case PBool.EBoolState.FalseThisFrame:
                if (onChangedTeam != null)
                    onChangedTeam(this, this.currentTeam);

                this.hasChangedTeam = new PBool(PBool.EBoolState.False);
                break;

            case PBool.EBoolState.TrueThisFrame:
                if (onChangedTeam != null)
                    onChangedTeam(this, this.currentTeam);

                this.hasChangedTeam = new PBool(PBool.EBoolState.True);
                break;
        }
    }
    private int GetFreeItemSlot()
    {
        for (int i = 0; i < holdingHand.Length; i++)
        {
            if (!holdingHand[i].hasItem)
                return i;
        }

        return -1;
    }

    protected bool CanPickupItem(int slot)
    {
        if (holdingHand.Length > 0 && slot < holdingHand.Length)
            return CanInteract();

        return false;
    }
    protected bool IsValidTeam(PlayerTeam team)
    {
        return (team != PlayerTeam.None);
    }

    public bool CanInteract()
    {
        return canInteract;
    }

    public float GetRaycastDist()
    {
        return raycastDistance;
    }

    public int GetHoldingHandDamage()
    {
        return 0;
    }

    public bool IsKnocked()
    {
        return hasBeenKnocked.GetBool();
    }

    public AMS_Item GetActiveItem()
    {
        if(GetIsValidSlot(activeSlot))
        {
            return holdingHand[activeSlot].item;
        }

        return null;
    }

    public bool PickupItem(AMS_Item item)
    {
        int slot = GetFreeItemSlot();

        if (slot >= 0)
        {
            var itemStoreTransform = holdingHand[slot].positionToStore;

            holdingHand[slot].hasItem = true;
            holdingHand[slot].item = item;

            item.holdingPosition = itemStoreTransform;

            item.transform.SetParent(this.transform);

            item.UpdateItemPosition_ServerRpc(itemStoreTransform.position, itemStoreTransform.rotation);

            //item.transform.SetPositionAndRotation(itemStoreTransform.position, itemStoreTransform.rotation);

            return true;
        }

        return false;
    }

    public bool GetIsValidSlot(int slot)
    {
        if(holdingHand.Length > 0 && slot < holdingHand.Length)
        {
            return true;
        }

        return false;
    }
}
