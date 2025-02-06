using UnityEngine;

public class AMS_LocalPlayer : LocalPlayer
{
    protected override void OnInitialized()
    {
        var amsPlayer = GetOwningPlayer<AMSPlayer>();

        amsPlayer.BindOnGamePendingState(OnGamePendingState);

        amsPlayer.OnSwitchTeams_Rpc(PlayerTeam.Thief);
    }

    protected override void OnDestroyed()
    {
        var amsPlayer = GetOwningPlayer<AMSPlayer>();

        amsPlayer.UnbindOnGamePendingState(OnGamePendingState);
    }

    protected override void OnUpdateClientControls()
    {
        base.OnUpdateClientControls();

        if(Input.GetKeyDown(KeyCode.Mouse0))
        {
            Interact();
        }
    }

    private void Interact()
    {
        var amsPlayer = GetOwningPlayer<AMSPlayer>();

        float yDir = this.GetPlayerCamera().transform.forward.y;

        amsPlayer.OnInteract_Rpc(yDir);
    }

    void OnGamePendingState(AMSPlayer _this, bool isGamePendingStart)
    {
        
    }
}
