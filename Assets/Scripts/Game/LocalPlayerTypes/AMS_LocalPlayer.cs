using Assets.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.Video;

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

        if (Input.GetKeyDown(KeyCode.F1))
            SwitchTeams(PlayerTeam.Thief);

        if (Input.GetKeyDown(KeyCode.F2))
            SwitchTeams(PlayerTeam.Defender);
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

    private void OnTeamChanged(AMSPlayer _this, PlayerTeam team)
    {
        if(team == PlayerTeam.Thief)
        {
            hasMoneyCounter = true;

            moneyText.transform.parent.gameObject.SetActive(true);

            UpdateMoneyValue(0);
        }
    }

    private void OnRoundMoneyChanged(AMSPlayer _this, int RoundMoney)
    {
        UpdateMoneyValue(RoundMoney);
    }

    private void OnGamePendingState(AMSPlayer _this, bool isGamePendingStart)
    {
        
    }

    private void SwitchTeams(PlayerTeam team)
    {
        var Player = GetOwningPlayer<AMSPlayer>();

        switch (team)
        {
            case PlayerTeam.Thief:
            case PlayerTeam.Defender:
                GetOwningPlayer<AMSPlayer>().OnSwitchTeams_Rpc(team);
                break;

            default:
                DebugClass.Log("Wrongfull Team Change!");
                break;
        }
    }
}
