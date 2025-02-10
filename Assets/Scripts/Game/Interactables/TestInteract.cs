using UnityEngine;

public class TestInteract : Interactable
{
    protected override void OnInteract(ulong id, AMServerManger serverManger, Vector3 relativeDirection)
    {
        base.OnInteract(id, serverManger, relativeDirection);
    }
}
