using UnityEngine;

public class GeneralInteract : Interactable
{
    public delegate void OnInteracted(GeneralInteract _this, ulong ID);
    public delegate void OnDestroyed_Interactable(GeneralInteract _this);

    private OnInteracted onInteracted = null;
    private OnDestroyed_Interactable onDestroyed_Interactable = null;

    public void BindOnInteract(OnInteracted onInteracted)
    {
        this.onInteracted += onInteracted;
    }

    public void BindOnDestroyed_Interactable(OnDestroyed_Interactable onDestroyed_)
    {
        this.onDestroyed_Interactable += onDestroyed_;
    }

    public void UnbindOnInteract(OnInteracted onInteracted)
    {
        this.onInteracted -= onInteracted;
    }

    public void UnbindOnDestroyed_Interactable(OnDestroyed_Interactable onDestroyed_Interactable)
    {
        this.onDestroyed_Interactable -= onDestroyed_Interactable;
    }
    protected override void OnInteract(ulong id, AMServerManger serverManger, Vector3 relativeDirection)
    {
        if (onInteracted != null)
            onInteracted(this, id);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if(onDestroyed_Interactable != null)
            onDestroyed_Interactable(this);
    }
}
