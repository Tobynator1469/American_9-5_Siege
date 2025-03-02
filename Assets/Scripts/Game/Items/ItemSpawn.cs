using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using System.Linq;



public class ItemSpawn : NetworkBehaviour
{
    private List<GameObject> possibleSpawns = new List<GameObject>();

    //public List<GameObject> Weapons = new List<GameObject>();
    public List<GameObject> LowValueStuff = new List<GameObject>();
    public List<GameObject> MediumValueStuff = new List<GameObject>();
    public List<GameObject> HighValueStuff = new List<GameObject>();

    private List<GameObject> selectedList;
    private List<GameObject> spawnedStuff = new List<GameObject>();

    //[SerializeField]
    //private int WPLimit = 1;


    [SerializeField]
    private int MVLimit = 1;
    
    private int CurrentMVLimit = 0;

    void Start()
    {

        possibleSpawns = FindAllItemSpawns();
    }

    public void DecideSpawned(GameObject listObj)
    {
        Debug.Log($"Der Spawn {listObj.name} hat den Tag {listObj.tag}.");




        if (listObj.name.Contains("HV"))
        {
            selectedList = HighValueStuff;
        }
        else if (CurrentMVLimit >= 0)
        {
            selectedList = MediumValueStuff;
            CurrentMVLimit--;
        }
        else
        {
            selectedList = LowValueStuff;
        }
        

        if (selectedList.Count == 0 || selectedList == null)
        {
            Debug.LogWarning("Die gewählte Liste ist leer!");
            return;
        }



        GameObject selectedObject = selectedList[UnityEngine.Random.Range(0, selectedList.Count)];
        GameObject spawnedObject = Instantiate(selectedObject, listObj.transform.position, Quaternion.identity);
        spawnedStuff.Add(spawnedObject);

        selectedList = null;

    }

    public void SpawnItems()
    {
        CurrentMVLimit = MVLimit;
        
        DestroyAllSpawned();

        System.Random rng = new System.Random();
        var shuffledSpawns = possibleSpawns.OrderBy(_ => rng.Next()).ToList();

        

        if (shuffledSpawns.Count > 0)
        {
            foreach (GameObject spawn in shuffledSpawns)
            {
                Debug.Log(spawn);
                
                DecideSpawned(spawn);
            }
        }
    }

    public void DestroyAllSpawned()
    {
        if (spawnedStuff.Count <= 0)
        {
            return;
        }

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