using Unity.Netcode;
using UnityEngine;

public class AMS_GameState : NetworkBehaviour
{
    private AMServerManger amsServerManger = null;
    private bool isInitialized = false;

    public override void OnNetworkDespawn()
    {

    }

    public override void OnNetworkSpawn()
    {

    }

    private void Update()
    {
        if (!IsServer)
            return;


    }

    public void OnBindServerManager(AMServerManger serverManger)
    {
        
    }
}
