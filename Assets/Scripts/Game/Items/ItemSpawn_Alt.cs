#define DebugStuff_

using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

//Used for calculating the Spawn Chance
public enum EItemRarity
{
    LowValue, // = 50/100
    MidValue, // = 25/100
    HighValue // = 12.5/100
}

[System.Serializable]
public struct ItemSpawnType
{
    public GameObject objectToSpawn;
    public EItemRarity rarity;
}

public class ItemSpawn_Alt : NetworkBehaviour
{
    [SerializeField]
    private ItemSpawnType[] spawnables = null;

    [SerializeField]
    private Transform[] spawnPlaces = null;

    private List<NetworkObject> spawnedObjects = new List<NetworkObject>();

    private void Update()
    {
#if DebugStuff_
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (IsHost)
            {
                SpawnStuff_ServerRpc();
            }
        }
#endif
    }

    [ServerRpc]
    public void SpawnStuff_ServerRpc()
    {
        for (int i = 0; i < spawnPlaces.Length; i++)
        {
            for (int i1 = 0; i1 < spawnables.Length; i1++)
            {
                if (IsRarityMet(spawnables[i1].rarity))
                {
                    var obj = Instantiate(spawnables[i1].objectToSpawn, spawnPlaces[i].position, spawnPlaces[i].rotation);

                    var netObj = obj.GetComponent<NetworkObject>();

                    spawnedObjects.Add(netObj);

                    netObj.Spawn();

                    break;
                }
            }
        }
    }

    [ServerRpc]
    public void DespawnStuff_ServerRpc()
    {
        for(int i = 0;i < spawnedObjects.Count; i++)
        {
            spawnedObjects[i].Despawn();
        }

        spawnedObjects.Clear();
    }

    private bool IsRarityMet(EItemRarity rarity)
    {
        int value = UnityEngine.Random.Range(0, 9);

        EItemRarity harvestedRarity = EItemRarity.LowValue;

        switch (value)
        {
            case 0:
                harvestedRarity = EItemRarity.HighValue; 
                break;

            case 1:
                harvestedRarity = EItemRarity.HighValue;
                break;

            case 2:
                harvestedRarity = EItemRarity.MidValue;
                break;

            case 3:
                harvestedRarity = EItemRarity.MidValue;
                break;

            case 4:
                harvestedRarity = EItemRarity.MidValue;
                break;

            case 5:
                harvestedRarity = EItemRarity.LowValue;
                break;

            case 6:
                harvestedRarity = EItemRarity.LowValue;
                break;

            case 7:
                harvestedRarity = EItemRarity.LowValue;
                break;

            case 8:
                harvestedRarity = EItemRarity.LowValue;
                break;

            case 9:
                harvestedRarity = EItemRarity.LowValue;
                break;

            default:
                break;
        }

        if (harvestedRarity == rarity)
            return true;

        return false;
    }
}
