using Unity.Netcode;
using UnityEngine;

public enum DoorState
{
    none, openRight, openLeft, closeRight, closeLeft, 
}
public class DoorPickInteractable : Interactable
{
    private GameObject DoorFront;
    private GameObject DoorBack;

    protected Animator animator = null;
    protected DoorState doorState = DoorState.none;

    protected bool isUnlocked = false;
    
    protected override void OnSpawned()
    {
        animator = GetComponent<Animator>();
    }

    protected override void OnInteract(ulong id, AMServerManger serverManger)
    {
        
    }

    [ClientRpc]
    private void OpenDoor_ClientRpc()
    {



    }

   
}
