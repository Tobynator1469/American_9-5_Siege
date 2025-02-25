using Assets.Scripts;
using UnityEngine;

public class DamagePlayerInteract : Interactable
{
    private AMSPlayer playerToDamage = null;

    [SerializeField]
    private int baseDamage = 2;

    [SerializeField]
    private float delayTillCanBeDamaged = 1.0f;
    private float delayTillCanBeDamagedCur = 0.0f;

    [SerializeField]
    private float knockbackAmount = 10.0f;

    [SerializeField]
    private bool delayedDamage = false;
    private bool delayRunning = false;

    protected override void OnSpawned()
    {
        playerToDamage = GetComponent<AMSPlayer>();
    }

    private void Update()
    {
        if(delayedDamage && delayRunning)
        {
            if (delayTillCanBeDamagedCur >= delayTillCanBeDamaged)
            {
                delayRunning = false;
                delayTillCanBeDamagedCur = 0.0f;
            }
            else
                delayTillCanBeDamagedCur += Time.deltaTime;
        }
    }
    protected override void OnInteract(ulong id, AMServerManger serverManger, Vector3 relativeDirection)
    {
        if (!CanBeDamaged())
            return;

        base.OnInteract(id, serverManger, relativeDirection); //Comment out later!

        int extraDamage = 0;

        var player = serverManger.FindConnectedPlayer(id);

        if (player)
        {
            extraDamage = player.GetHoldingHandDamage();
        }

        playerToDamage.DamagePlayer_ServerRpc(baseDamage + extraDamage);

        OnDamagedPlayer_Server(player);
    }

    protected virtual void OnDamagedPlayer_Server(AMSPlayer source)
    {
        DebugClass.Log("Damaged Player!");

        playerToDamage.SetForceSlidedClientRpc(1.0f);

        Player.VelocityUpdate velocityUpdate = Player.VelocityUpdate.Forward | Player.VelocityUpdate.Right;

        var knockbackVelocityTarget = ((source.transform.forward * knockbackAmount) + (Vector3.up * (knockbackAmount / 2)));

        playerToDamage.OnNetworkUpdatePosition_ServerRpc(playerToDamage.transform.position, knockbackVelocityTarget, velocityUpdate);
    }

    private bool CanBeDamaged()
    {
        return !delayedDamage || !delayRunning;
    }

    public void SetDelayOnHit(float delay, bool delayHits)
    {
        delayedDamage = delayHits;
        delayRunning = false;

        delayTillCanBeDamaged = delay;
        delayTillCanBeDamagedCur = 0.0f;
    }
}
