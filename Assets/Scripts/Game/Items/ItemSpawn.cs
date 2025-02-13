using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum ItemTypes
{
    Weapon,
    LowValue,
    MediumValue,
    HighValue
}



public class ItemSpawn : NetworkBehaviour
{
    [SerializeField]
    private GameObject[] possibleSpawns;
    /*
    [SerializeField]
    private Dictionary<ItemTypes, List<GameObject>> items = new()
    { { ItemTypes.Weapons, new List<GameObject>() }, { ItemTypes.LowQuality, new List<GameObject>() },
      { ItemTypes.MediumQuality, new List<GameObject>() }, { ItemTypes.HighQuality, new List<GameObject>() }
    };*/

    public GameObject[] Weapons;
    public GameObject[] LowValueStuff;
    public GameObject[] MediumValueStuff;
    public GameObject[] HighValueStuff;

    private GameObject[] SpawnedStuff;

    [SerializeField]
    private int WeaponLimit = 1;
    void Start()
    {
        
    }

    public void DecideSpawned(GameObject listObj)
    {

        Debug.Log($"Der Spawn {listObj.name} hat den Tag {listObj.tag}.");


    }

    public void SpawnItems()
    {

        foreach (GameObject spawn in possibleSpawns)
        {
            if (spawn.tag != "HVSpawn")
            {
                DecideSpawned(spawn);
            }
            else
            {
                Debug.Log($" {spawn.name} is gud jit");
            }
        }

    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            SpawnItems();
        }
    }
}
