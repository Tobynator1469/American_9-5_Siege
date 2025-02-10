using Unity.Netcode;
using UnityEngine;

public enum DoorState
{
    None, 
    OpenRight, 
    OpenLeft, 
    CloseRight, 
    CloseLeft, 
}
public class DoorPickInteractable : Interactable
{
    protected Animator animator = null;
    protected DoorState doorState = DoorState.None;

    protected bool isUnlocked = false;
    
    protected override void OnSpawned()
    {
        animator = GetComponent<Animator>();
    }

    protected override void OnInteract(ulong id, AMServerManger serverManger, Vector3 relativeDirection)
    {
        isUnlocked = !isUnlocked;

        if(IsRaycastFromFront(relativeDirection))
        {
            if(isUnlocked)
                doorState = DoorState.OpenLeft;
            else
                doorState = DoorState.CloseLeft;
        }
        else
        {
            if (isUnlocked)
                doorState = DoorState.OpenRight;
            else
                doorState = DoorState.CloseRight;
        }

        OpenDoor_ClientRpc(doorState);
    }

    [ClientRpc]
    private void OpenDoor_ClientRpc(DoorState doorState)
    {

    }

    private bool IsRaycastFromFront(Vector3 relativeDirection)
    {
        var lookRotation = Quaternion.LookRotation(relativeDirection).eulerAngles;

        if (lookRotation.y < 180.0f)
            return true;

        return false;
    }
}
