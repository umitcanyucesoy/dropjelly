using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Game;
using Tiles;
using UnityEngine;

namespace Grid
{
    public class GridManager : MonoBehaviour
    {
        [Header("~~~~~~~~~~ GRID ELEMENTS ~~~~~~~~~~")]
        [SerializeField] private GameObject gridPrefab;
        [SerializeField] private Transform gridParent;
        [SerializeField] public int rows;
        [SerializeField] public int columns;
        [SerializeField] public float gridSize;
        
        [Header("~~~~~~~~~~ GRID SETTINGS ~~~~~~~~~~")]
        private GameObject[,] _gridPositions;
        
        
        public static GridManager instance { get; private set; }
        private void Awake()
        {
            instance = this;
        }

        private void Start()
        {
            CreateGrid();
        }

        private void CreateGrid()
        {
            _gridPositions = new GameObject[rows, columns];
            for (var row = 0; row < rows; row++)
            {
                for (var col = 0; col < columns; col++)
                {
                    Vector3 position = new Vector3(col * gridSize, -row * gridSize, 0);
                    var newGrid = Instantiate(gridPrefab, position, Quaternion.identity,gridParent);
                    newGrid.name = $"Tile: {row}_{col}";
                    _gridPositions[row, col] = newGrid;
                }
            }
        }
    private IEnumerator ApplyGravity()
    {
        bool moved;
        do
        {
            moved = false;
            for (int row = rows - 2; row >= 0; row--)
            {
                for (int col = 0; col < columns; col++)
                {
                    GameObject currentCell = _gridPositions[row, col];
                    if (currentCell.transform.childCount > 0)
                    {
                        GameObject belowCell = _gridPositions[row + 1, col];
                        GridCell belowGridCell = belowCell.GetComponent<GridCell>();

                        if (belowCell.transform.childCount == 0 && (!belowGridCell || belowGridCell.isLocked == false))
                        {
                            if (belowGridCell)
                                belowGridCell.isLocked = true;

                            Transform movingTile = currentCell.transform.GetChild(0);
                            Vector3 targetPos = belowCell.transform.position;

                            movingTile.DOMove(targetPos, 0.3f)
                                .SetEase(Ease.OutBounce)
                                .SetLink(movingTile.gameObject, LinkBehaviour.KillOnDestroy)
                                .OnComplete(() =>
                                {
                                    if (movingTile != null)
                                    {
                                        movingTile.SetParent(belowCell.transform);
                                        movingTile.localPosition = Vector3.zero;
                                    }
                                    if (belowGridCell != null)
                                        belowGridCell.isLocked = false;
                                });
                            moved = true;
                        }
                    }
                }
            }
            if (moved)
                yield return new WaitForSeconds(0.35f);
        } while (moved);
    }

        
    public void UpdateGrid()
    {
        StartCoroutine(ApplyGravity());
    }
        
        public GameObject GetTileAt(int row, int col)
        {
            if (row >= 0 && row < rows && col >= 0 && col < columns)
            {
                if (_gridPositions[row, col].transform.childCount > 0)
                {
                    return _gridPositions[row, col].transform.GetChild(0).gameObject;
                }
            }
            return null;
        }

        public bool PlaceTile(int column, GameObject tile)
        {
            for (var row = rows - 1; row >= 0; row--)
            {
                if (_gridPositions[row, column].transform.childCount == 0)
                {
                    Vector3 targetPosition = _gridPositions[row, column].transform.position;

                    tile.transform.DOMove(targetPosition, 0.5f).SetEase(Ease.OutQuad).OnComplete(() =>
                    {
                        tile.transform.SetParent(_gridPositions[row, column].transform);
                        tile.transform.localPosition = Vector3.zero; 

                        StartCoroutine(DelayedMatchCheck());
                    });

                    return true;
                }
            }

            return false;
        }

        private IEnumerator DelayedMatchCheck()
        {
            yield return new WaitForSeconds(0.1f); 
            MatchManager.instance.CheckForMatches();
        }
    }    
    
    
}

