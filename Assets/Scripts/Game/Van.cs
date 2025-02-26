using Unity.Netcode;
using UnityEngine;

public class Van : NetworkBehaviour
{
    [SerializeField]
    private GeneralInteract generalInteract = null;

    public override void OnNetworkSpawn()
    {
        if(generalInteract)
        {
            generalInteract.BindOnInteract(OnPlayerInteracted);
            generalInteract.BindOnDestroyed_Interactable(OnInteractableDestroyed);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (generalInteract)
        {
            generalInteract.UnbindOnInteract(OnPlayerInteracted);
            generalInteract.UnbindOnDestroyed_Interactable(OnInteractableDestroyed);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(IsHost)
        {
            UpdatePlayer(collision.gameObject.GetComponent<AMSPlayer>());
        }
    }

    private void OnPlayerInteracted(GeneralInteract _this, AMServerManger serverManger, ulong ID)
    {
        var player = serverManger.FindConnectedPlayer(ID);

        UpdatePlayer(player);
    }

    private void OnInteractableDestroyed(GeneralInteract _this)
    {
        if (generalInteract)
        {
            generalInteract.UnbindOnInteract(OnPlayerInteracted);
            generalInteract.UnbindOnDestroyed_Interactable(OnInteractableDestroyed);
        }
    }

    private void UpdatePlayer(AMSPlayer player)
    {
        if (player && player.currentTeam == PlayerTeam.Thief)
        {
            var item = player.GetActiveItem();

            if (item && item.GetType() == typeof(AMS_ValueableItem))
            {
                var valueableItem = (AMS_ValueableItem)item;

                player.DropItem_ServerRpc();

                player.SetRoundMoney_ServerRpc(player.currentUpdateRoundMoney + valueableItem.GetValueOfItem());

                NetworkObject.Despawn();
            }
        }
    }
}
