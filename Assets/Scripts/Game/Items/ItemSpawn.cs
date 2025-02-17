using Newtonsoft.Json.Linq;
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

        // Wähle zufällig eines der drei Arrays
        GameObject[][] arrays = { Weapons, LowValueStuff, MediumValueStuff };
        GameObject[] selectedArray = arrays[UnityEngine.Random.Range(0, arrays.Length)];

        if (selectedArray.Length == 0)
        {
            Debug.LogWarning("Das gewählte Array ist leer!");
            return;
        }

        GameObject selectedObject = selectedArray[UnityEngine.Random.Range(0, selectedArray.Length)];

        Instantiate(selectedObject, listObj.transform.position, Quaternion.identity);

    }

    /*
     void SpawnRandomObject()
    {
        // Wähle zufällig eines der drei Arrays
        GameObject[][] arrays = { array1, array2, array3 };
        GameObject[] selectedArray = arrays[Random.Range(0, arrays.Length)];
        
        if (selectedArray.Length == 0)
        {
            Debug.LogWarning("Das gewählte Array ist leer!");
            return;
        }
        
        // Wähle zufällig ein GameObject aus dem gewählten Array
        GameObject selectedObject = selectedArray[Random.Range(0, selectedArray.Length)];
        
        // Instanziiere das GameObject an der Position des SPAWNPOINTs
        Instantiate(selectedObject, SPAWNPOINT.position, Quaternion.identity);
    }
     */

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
