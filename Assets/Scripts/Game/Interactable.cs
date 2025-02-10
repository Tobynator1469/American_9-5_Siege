using Assets.Scripts;
using Unity.Netcode;
using UnityEngine;

public abstract class Interactable : NetworkBehaviour
{
    private bool isSpawned = false;
    protected bool isInteractable = true;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        isSpawned = true;

        OnSpawned();
    }
    protected virtual void OnSpawned()
    {
        
    }

    protected virtual void OnInteract(ulong id, AMServerManger serverManger, Vector3 relativeDirection)
    {
        Debug.Log(id + " Interacted with");
    }

    [ServerRpc]
    public void Interact_ServerRpc(ulong id, Vector3 relativeDirection)
    {
        if (isInteractable == false || isSpawned == false)
            return;

        var serverManager = GameObject.FindGameObjectWithTag("ServerManager");

        if (serverManager != null)
            OnInteract(id, serverManager.GetComponent<AMServerManger>(), relativeDirection);
        else
            DebugClass.Error("Failed To Find ServerManager through Tag!");
    }
}
