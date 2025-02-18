using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


public class ItemSpawn : NetworkBehaviour
{
    [SerializeField]
    private List<GameObject> possibleSpawns = new List<GameObject>();

    public List<GameObject> Weapons = new List<GameObject>();
    public List<GameObject> LowValueStuff = new List<GameObject>();
    public List<GameObject> MediumValueStuff = new List<GameObject>();
    public List<GameObject> HighValueStuff = new List<GameObject>();

    private List<GameObject> selectedList;
    private List<GameObject> spawnedStuff = new List<GameObject>();

    [SerializeField]
    private int WPLimit = 1;

    [SerializeField]
    private int LVLimit = 1;

    [SerializeField]
    private int MVLimit = 1;

    void Start()
    {

    }

    public void DecideSpawned(GameObject listObj)
    {
        Debug.Log($"Der Spawn {listObj.name} hat den Tag {listObj.tag}.");

        List<List<GameObject>> lists = new List<List<GameObject>> { Weapons, LowValueStuff, MediumValueStuff };

        if (listObj.name.Contains("HV"))
        {
            selectedList = HighValueStuff;
        }
        else
        {
            selectedList = lists[UnityEngine.Random.Range(0, lists.Count)];
        }

        if (selectedList.Count == 0)
        {
            Debug.LogWarning("Die gewählte Liste ist leer!");
            return;
        }



        GameObject selectedObject = selectedList[UnityEngine.Random.Range(0, selectedList.Count)];
        GameObject spawnedObject = Instantiate(selectedObject, listObj.transform.position, Quaternion.identity);
        spawnedStuff.Add(spawnedObject);

    }

    public void SpawnItems()
    {
        
        DestroyAllSpawned();

        possibleSpawns = FindAllItemSpawns();

        if (possibleSpawns.Count > 0)
        {
            foreach (GameObject spawn in possibleSpawns)
            {
                

                DecideSpawned(spawn);
            }
        }
    }

    public void DestroyAllSpawned()
    {
        foreach (GameObject obj in spawnedStuff)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        spawnedStuff.Clear();
    }

    private List<GameObject> FindAllItemSpawns()
    {
        
        return new List<GameObject>(GameObject.FindGameObjectsWithTag("ItemSpawn"));

    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            SpawnItems();
        }
    }
}