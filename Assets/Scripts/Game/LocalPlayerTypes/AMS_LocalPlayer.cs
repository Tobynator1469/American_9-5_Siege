using TMPro;
using UnityEngine;

public class AMS_LocalPlayer : LocalPlayer
{
    [SerializeField]
    private TextMeshProUGUI moneyText = null;

    private bool hasMoneyCounter = false;

    protected override void OnInitialized()
    {
        var amsPlayer = GetOwningPlayer<AMSPlayer>();

        amsPlayer.BindOnGamePendingState(OnGamePendingState);
        amsPlayer.BindOnGameChangedRoundMoney(OnRoundMoneyChanged);
        amsPlayer.BindOnChangedTeam(OnTeamChanged);

        amsPlayer.OnSwitchTeams_Rpc(PlayerTeam.Thief);

        GetServerManager<AMServerManger>().GetServerPlayerDataRpc();
    }

    protected override void OnDestroyed()
    {
        var amsPlayer = GetOwningPlayer<AMSPlayer>();

        amsPlayer.UnbindOnGamePendingState(OnGamePendingState);
        amsPlayer.UnbindOnGameChangedRoundMoney(OnRoundMoneyChanged);
        amsPlayer.UnbindOnChangedTeam(OnTeamChanged);
    }

    protected override void OnUpdateClientControls()
    {
        base.OnUpdateClientControls();

        if(Input.GetKeyDown(KeyCode.Mouse0))
        {
            Interact();
        }
    }

    private void UpdateMoneyValue(int currentMoney)
    {
        if(hasMoneyCounter)
        {
            moneyText.text = $"{currentMoney}$";
        }
    }

    private void Interact()
    {
        var amsPlayer = GetOwningPlayer<AMSPlayer>();

        float yDir = this.GetPlayerCamera().transform.forward.y;

        amsPlayer.OnInteract_Rpc(yDir);
    }

    void OnTeamChanged(AMSPlayer _this, PlayerTeam team)
    {
        if(team == PlayerTeam.Thief)
        {
            hasMoneyCounter = true;

            moneyText.transform.parent.gameObject.SetActive(true);

            UpdateMoneyValue(0);
        }
    }

    void OnRoundMoneyChanged(AMSPlayer _this, int RoundMoney)
    {
        UpdateMoneyValue(RoundMoney);
    }

    void OnGamePendingState(AMSPlayer _this, bool isGamePendingStart)
    {
        
    }
}
