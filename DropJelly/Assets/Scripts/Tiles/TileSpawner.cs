using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tiles
{
    public class TileSpawner : MonoBehaviour
    {
        [Header("~~~~~~ SPAWN ELEMENTS ~~~~~~")]
        //[SerializeField] private List<GameObject> tilePrefabs = new List<GameObject>();
        public GameObject tilePrefab1;
        public GameObject tilePrefab2;
        public GameObject tilePrefab3;
        public GameObject tilePrefab4;
        [SerializeField] private Transform spawnPoint;
        
        public static TileSpawner instance { get; private set; }
        private void Awake()
        {
            if (instance == null)
                instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            SpawnNewTile();
        }

        public void SpawnNewTile()
        {
            var randomType = UnityEngine.Random.Range(0, 4);
            GameObject tileType = null;

            switch (randomType)
            {
                case 0:
                    tileType = Instantiate(tilePrefab1, spawnPoint.position, Quaternion.identity); // Single 
                    break;
                case 1:
                    tileType = Instantiate(tilePrefab2, spawnPoint.position, Quaternion.identity); // SplitTile2
                    break;
                case 2:
                    tileType = Instantiate(tilePrefab3, spawnPoint.position, Quaternion.identity); // SplitTile3
                    break;
                case 3:
                    tileType = Instantiate(tilePrefab4, spawnPoint.position, Quaternion.identity); // SplitTile4
                    break;
            }
            
            
                tileType.GetComponent<Tile>().SetRandomColors();
        }
    }
}