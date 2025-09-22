using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TetherPool : MonoBehaviour
{
    [Header("Properties")]
    [SerializeField] private int poolSize = 5;
    [SerializeField] private GameObject tetherPullPrefab;
    private GameObject[] tetherPool; //keep this for resetting all tethers if needed
    private Queue<GameObject> availableTethers;

    public int AvailableTetherCount => availableTethers.Count;

    private void Awake()
    {
        tetherPool = new GameObject[poolSize];
        availableTethers = new Queue<GameObject>();

        for (int i = 0; i < poolSize; i++)
        {
            GameObject newTether = Instantiate(tetherPullPrefab, transform);
            newTether.SetActive(false);
            newTether.name = "Tether " + i.ToString();
            tetherPool[i] = newTether;
            availableTethers.Enqueue(newTether);
        }
    }

    public GameObject GetTether()
    {
        if (availableTethers.Count == 0)
        {
           print("no tethers left");
           return null;
        }

        GameObject tether = availableTethers.Dequeue();
        tether.SetActive(true);
        return tether;
    }

    public GameObject[] GetAllPlantedTether()
    {
        return tetherPool;
    }

    public void ReturnTether(GameObject tether)
    {
        tether.SetActive(false);
        availableTethers.Enqueue(tether);
    }

    public void ResetAllTethers()
    {
        foreach (var tether in tetherPool)
        {
            if (tether != null)
            {
                tether.SetActive(false);
                if (!availableTethers.Contains(tether))
                    availableTethers.Enqueue(tether);
            }
        }
    }
}
