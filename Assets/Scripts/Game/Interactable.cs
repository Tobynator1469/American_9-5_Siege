using Unity.Netcode;
using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    protected bool isInteractable;


    protected virtual void OnInteract(ulong id)
    {
        Debug.Log(id + " Interacted with");
    }

    [ServerRpc]
    public void Interact_ServerRpc(ulong id)
    {
        OnInteract(id);
    }
}
