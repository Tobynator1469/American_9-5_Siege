using Unity.Netcode;
using UnityEngine;

public class AMServerManger : ServerManager
{
    [SerializeField]
    private GameObject ThiefPrefab = null;

    [SerializeField]
    private GameObject DefenderPrefab = null;

    private bool hasGameStarted = false;

    [ServerRpc]
    protected override void OnStartGameBtnServerRpc()
    {

    }
}
