using Unity.Netcode;
using UnityEngine;

public class AMS_Item : NetworkBehaviour
{
    private Transform holdingPosition = null; //The position where the Item is locked at, can be Hand or an Shelf
    private ulong ownerPlayerID = 0;

    private bool hasBeenPickedUp = false;
    private bool canBePickedUp = true;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }
}
