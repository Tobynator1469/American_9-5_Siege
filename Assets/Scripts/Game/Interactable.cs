using Assets.Scripts;
using Unity.Netcode;
using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    protected bool isInteractable;


    protected virtual void OnInteract(ulong id, AMServerManger serverManger)
    {
        Debug.Log(id + " Interacted with");
    }

    [ServerRpc]
    public void Interact_ServerRpc(ulong id)
    {
        var serverManager = GameObject.FindGameObjectWithTag("ServerManager");

        if (serverManager != null)
            OnInteract(id, serverManager.GetComponent<AMServerManger>());
        else
            DebugClass.Error("Failed To Find ServerManager through Tag!");
    }
}
