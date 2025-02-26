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

    public Transform holdingPosition = null; //The position where the Item is locked at, can be Hand or an Shelf
    private ulong ownerPlayerID = 0;

    private bool hasBeenPickedUp = false;

    protected override void OnSpawned()
    {
        rigidBody = GetComponent<Rigidbody>();
    }

    protected override void OnInteract(ulong id, AMServerManger serverManger, Vector3 relativeDirection)
    {
        if(!hasBeenPickedUp)
        {
            SetItemActive_ServerRpc(false);

            var player = serverManger.FindConnectedPlayer(id);

            if (player)
            {
                if (player.PickupItem(this))
                {
                    OnItemState(true, id);
                }
            }
        }
        else
        {
            if(id != ownerPlayerID)
            {
                DebugClass.Error("Non Owner tried interacting with Item");
                return;
            }

            var player = serverManger.FindConnectedPlayer(id);

            InteractWithItem(player);
        }
    }

    [ServerRpc]
    public void SetItemActive_ServerRpc(bool active)
    {
        if(m_Renderer)
        {
            m_Renderer.enabled = active;
        }

        isInteractable = active;

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

    [Rpc(SendTo.Server)]
    public void DropItem_ServerRpc(RpcParams rpc = default)
    {
        if(rpc.Receive.SenderClientId != ownerPlayerID)
        {
            DebugClass.Error("Non Owner Tried Droping up Item!");
            return;
        }

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

        rigidBody.isKinematic = false;

        holdingPosition = null;

        ownerPlayerID = 0;

        hasBeenPickedUp = false;
    }
}
