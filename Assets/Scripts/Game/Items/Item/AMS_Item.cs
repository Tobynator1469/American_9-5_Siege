using Assets.Scripts;
using Unity.Netcode;
using UnityEngine;

public abstract class AMS_Item : Interactable
{
    [SerializeField]
    private MeshRenderer m_Renderer = null;

    [SerializeField]
    private Collider[] colliders = null;

    private Rigidbody rigidBody = null;
    private ServerObject serverObjComp = null;

    public Transform holdingPosition = null; //The position where the Item is locked at, can be Hand or an Shelf
    private ulong ownerPlayerID = 0;

    private bool hasBeenPickedUp = false;

    protected override void OnSpawned()
    {
        rigidBody = GetComponent<Rigidbody>();
        serverObjComp = GetComponent<ServerObject>();

        if (IsHost)
        {
            serverObjComp.ObjectUseNonServerPos_ServerRpc(true);
        }
    }

    protected override void OnInteract(ulong id, AMServerManger serverManger, Vector3 relativeDirection)
    {
        var player = serverManger.FindConnectedPlayer(id);

        if (!hasBeenPickedUp && CanPickup(player))
        {
            if (player)
            {
                if (player.PickupItem(this))
                {
                    OnItemState(true, id);
                    SetItemActive_ServerRpc(false);

                    SetIsKinemetic(true);
                    SetCollidersValue(false);
                }
            }
        }
        else
        {
            if (id != ownerPlayerID)
            {
                DebugClass.Error("Non Owner tried interacting with Item");
                return;
            }

            InteractWithItem(player);
        }
    }

    protected virtual bool CanPickup(AMSPlayer player)
    {
        return (player != null);
    }

    [ServerRpc]
    public void UpdateItemPosition_ServerRpc(Vector3 pos, Quaternion quaternion)
    {
        rigidBody.position = pos;
        rigidBody.rotation = quaternion;

        serverObjComp.ServerUpdateObjectServerRpc();
    }

    [ServerRpc]
    public void SetItemActive_ServerRpc(bool active)
    {
        if (m_Renderer)
        {
            m_Renderer.enabled = active;
        }
    }

    private void SetIsKinemetic(bool active)
    {
        if (rigidBody)
        {
            rigidBody.isKinematic = active;
        }
    }

    private void SetCollidersValue(bool active)
    {
        if (colliders != null)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = active;
            }
        }
    }

    private void InteractWithItem(AMSPlayer player)
    {
        InteractItem(player);
    }

    protected virtual void InteractItem(AMSPlayer player)
    {

    }

    [ServerRpc]
    public void DropItem_ServerRpc()
    {
        ResetItem();
    }

    private void OnItemState(bool pickedUp, ulong ID)
    {
        //isInteractable = !pickedUp;
        hasBeenPickedUp = pickedUp;

        ownerPlayerID = ID;
    }

    private void ResetItem()
    {
        transform.SetParent(null, true);

        SetIsKinemetic(false);
        SetCollidersValue(true);

        holdingPosition = null;

        ownerPlayerID = 0;

        hasBeenPickedUp = false;
    }
}
