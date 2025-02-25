using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor.Overlays;
using UnityEngine;

public class MultiplayerCutscene : NetworkBehaviour
{
    const float MaxNetworkDelay = 5.0f;

    [SerializeField]
    private Animator animator = null;

    [SerializeField]
    private Camera owningCutsceneCamera = null;

    private AMServerManger manager = null;

    private Dictionary<ulong, bool> subscribedPlayers = new Dictionary<ulong, bool>();

    public delegate void OnEndCutscene(MultiplayerCutscene _this);
    private OnEndCutscene onEndCutscene;

    private float timerTillAutoSkip = 0.0f;
    private float timerTillAutoSkipCur = 0.0f;

    private bool hasCutsceneStarted = false;
    private bool hasServerManager = false;

    public void BindOnEndCutscene(OnEndCutscene handler)
    {
        onEndCutscene += handler;
    }

    public void UnbindOnEndCutscene(OnEndCutscene handler)
    {
        onEndCutscene -= handler;
    }

    public override void OnNetworkSpawn()
    {
        owningCutsceneCamera.gameObject.SetActive(false);
    }

    private void Update()
    {
        if(hasCutsceneStarted)
        {
            if (timerTillAutoSkipCur >= timerTillAutoSkip)
            {
                ForceEndCutscene_ServerRpc();
            }
            else
                timerTillAutoSkipCur += Time.deltaTime;
        }
    }

    [ServerRpc]
    public void AddPlayerToCutscene_ServerRpc(ulong playerID)
    {
        subscribedPlayers.Add(playerID, false);
    }

    [ServerRpc]
    public void StartCutscene_ServerRpc(string AnimationToStart)
    {
        if(hasCutsceneStarted || !hasServerManager) 
            return;

        if(HasAnimatorCutscene(AnimationToStart, out AnimationClip clip))
        {
            hasCutsceneStarted = true;

            this.timerTillAutoSkip = clip.length + MaxNetworkDelay; //adjust for Network delay

            StartCutscene_ClientRpc(AnimationToStart);
        }
    }

    public void BindServerManager(AMServerManger serverManger)
    {
        this.manager = serverManger;

        if(serverManger)
        {
            this.hasServerManager = true;

            serverManger.BindOnAMServerManagerDestroyed(OnAMServerManagerDestroyed);
        }
        else
            this.hasServerManager = false;
    }

    [ServerRpc]
    public void ForceEndCutscene_ServerRpc()
    {
        if(hasCutsceneStarted)
        {
            timerTillAutoSkipCur = 0.0f;

            hasCutsceneStarted = false;

            this.animator.StopPlayback();

            owningCutsceneCamera.gameObject.SetActive(false);

            if (onEndCutscene != null)
                onEndCutscene(this);
        }
    }

    [ClientRpc]
    private void StartCutscene_ClientRpc(string AnimationToStart)
    {
        if ((Camera.current))
        {
            Camera.current.gameObject.SetActive(false);
        }

        owningCutsceneCamera.gameObject.SetActive(true);

        animator.SetTrigger(AnimationToStart);
    }

    [Rpc(SendTo.Server)]
    private void OnFinishedPlayerWatch_Rpc(RpcParams rpcParams = default)
    {
        subscribedPlayers[rpcParams.Receive.SenderClientId] = true;

        if(HasEveryoneWatched())
        {
            timerTillAutoSkipCur = 0.0f;

            hasCutsceneStarted = false;

            owningCutsceneCamera.gameObject.SetActive(false);

            if (onEndCutscene != null)
                onEndCutscene(this);
        }
    }

    private void OnFinishedWatchingAnim()
    {
        OnFinishedPlayerWatch_Rpc();
    }

    private void OnAMServerManagerDestroyed(AMServerManger sv)
    { 
        if(!HasEveryoneWatched())
        {
            this.manager = null;

            this.hasServerManager = false;

            this.animator.StopPlayback();
        }
    }

    private bool HasEveryoneWatched()
    {
        foreach (var player in subscribedPlayers)
        {
            if(!player.Value)
                return false;
        }

        return true;
    }

    private bool HasAnimatorCutscene(string name, out AnimationClip clipOut)
    {
        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;

        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i].name == name)
            {
                clipOut = clips[i];

                return true;
            }
        }

        clipOut = null;

        return false;
    }
}