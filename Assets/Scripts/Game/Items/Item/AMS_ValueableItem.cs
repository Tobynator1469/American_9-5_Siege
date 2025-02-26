using UnityEngine;

public class AMS_ValueableItem : AMS_Item
{
    [SerializeField]
    private int itemValue = 2500;
    
    public int GetValueOfItem()
    {
        return itemValue;
    }
}
