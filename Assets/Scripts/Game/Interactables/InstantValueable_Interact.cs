using UnityEngine;

public class InstantValueable_Interact : Interactable
{
    [SerializeField]
    private int MoneyValue = 1000;

    protected override void OnInteract(ulong id, AMServerManger serverManger, Vector3 relativeDirection)
    {
        var player = (AMSPlayer)serverManger.FindPlayer(id);

        if(player && player.currentTeam == PlayerTeam.Thief)
        {
            this.isInteractable = false;

            player.SetRoundMoney_ServerRpc(player.currentUpdateRoundMoney + MoneyValue);

            NetworkObject.Despawn();
        }
    }
}
