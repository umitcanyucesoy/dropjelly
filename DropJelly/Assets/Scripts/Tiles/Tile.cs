using System;
using System.Collections.Generic;
using Game;
using Grid;
using UnityEngine;

namespace Tiles
{
    public class Tile : MonoBehaviour
    {
        [Header("~~~~~~~ TILE ELEMENTS ~~~~~~")]
        private static readonly Color[] PredefinedColors = { Color.red, Color.blue, Color.yellow };
        public Color[] _tileColors;
        
        [Header("~~~~~~~ TILE SETTINGS ~~~~~~")]
        private Vector3 _offset;
        private MeshRenderer[] _meshRenderers;
        private bool _isDragging = false;

        private void Awake()
        {
            _meshRenderers = GetComponentsInChildren<MeshRenderer>();
        }

        private void Start()
        {
            SetRandomColors();
        }

        public void SetRandomColors()
        {
            _tileColors = new Color[_meshRenderers.Length];
            List<Color> availableColors = new List<Color>(PredefinedColors);

            for (int i = 0; i < _meshRenderers.Length; i++)
            {
                Color chosenColor;
                bool isValid;

                do
                {
                    isValid = true;
                    chosenColor = availableColors[UnityEngine.Random.Range(0, availableColors.Count)];

                    if (_meshRenderers.Length == 2) 
                    {
                        if (i > 0 && _tileColors[i - 1] == chosenColor)
                        {
                            isValid = false; 
                        }
                    }
                    else if (_meshRenderers.Length == 3) 
                    {
                        if (i > 0 && _tileColors[i - 1] == chosenColor)
                        {
                            isValid = false; 
                        }
                        if (availableColors.Count >= 3) 
                        {
                            availableColors.Remove(chosenColor);
                        }
                    }
                    else if (_meshRenderers.Length == 4) 
                    {
                        if (i > 0 && _tileColors[i - 1] == chosenColor)
                        {
                            isValid = false; 
                        }
                        if (i >= 2 && _tileColors[i - 2] == chosenColor)
                        {
                            isValid = false; 
                        }
                    }
                }
                while (!isValid);

                _tileColors[i] = chosenColor;
                _meshRenderers[i].material.color = _tileColors[i];
            }
        }

        private void OnMouseDown()
        {
            Vector3 mousePosition = GetMouseWorldPosition();
            _offset = new Vector3(transform.position.x - mousePosition.x, 0, 0);
            _isDragging = true;
        }

        private void OnMouseDrag()
        {
            if (_isDragging)
            {
                Vector3 mousePosition = GetMouseWorldPosition();
                mousePosition.y = transform.position.y;
                mousePosition.z = transform.position.z;
                transform.position = mousePosition + _offset;
            }         
        }

        private void OnMouseUp()
        {
            _isDragging = false;
            var closestColumn = GetClosestColumn();

            if (GridManager.instance.PlaceTile(closestColumn, this.gameObject))
            {
                Debug.Log("Tile placed at " + closestColumn);
                TileSpawner.instance.SpawnNewTile();
            }
            else
            {
                Debug.Log("Tile could not be placed at " + closestColumn);
            }
        }

       public bool HasMatchingPart(Tile otherTile, out GameObject matchedPart, out GameObject otherMatchedPart)
{
    matchedPart = null;
    otherMatchedPart = null;

    Vector3 offset = otherTile.transform.position - this.transform.position;
    
    bool horizontalAdj = (Mathf.Abs(offset.x) >= Mathf.Abs(offset.y));
    bool isRight  = (horizontalAdj && offset.x > 0f);
    bool isLeft   = (horizontalAdj && offset.x < 0f);
    bool isTop    = (!horizontalAdj && offset.y > 0f);
    bool isBottom = (!horizontalAdj && offset.y < 0f);

    MeshRenderer[] myRenderers = GetComponentsInChildren<MeshRenderer>();
    MeshRenderer[] otherRenderers = otherTile.GetComponentsInChildren<MeshRenderer>();

    List<MeshRenderer> myCandidates = new List<MeshRenderer>();
    List<MeshRenderer> otherCandidates = new List<MeshRenderer>();

    
    bool isFourPiece = (myRenderers.Length == 4);
    float tol = 0.1f;

    foreach (var rend in myRenderers)
    {
        if (!rend) continue;
        Vector3 lp = rend.transform.localPosition;
        Vector3 ls = rend.transform.localScale;

        float halfW = ls.x * 0.5f;
        float halfH = ls.y * 0.5f;
        float minX = lp.x - halfW;
        float maxX = lp.x + halfW;
        float minY = lp.y - halfH;
        float maxY = lp.y + halfH;

        if (isRight)
        {
            if (maxX > 0f && (!isFourPiece || Mathf.Abs(lp.y) < tol))
                myCandidates.Add(rend);
        }
        else if (isLeft)
        {
            if (minX < 0f && (!isFourPiece || Mathf.Abs(lp.y) < tol))
                myCandidates.Add(rend);
        }
        else if (isTop)
        {
            if (maxY > 0f && (!isFourPiece || Mathf.Abs(lp.x) < tol))
                myCandidates.Add(rend);
        }
        else if (isBottom)
        {
            if (minY < 0f && (!isFourPiece || Mathf.Abs(lp.x) < tol))
                myCandidates.Add(rend);
        }
    }

    foreach (var rend in otherRenderers)
    {
        if (!rend) continue;
        Vector3 lp = rend.transform.localPosition;
        Vector3 ls = rend.transform.localScale;
        float halfW = ls.x * 0.5f;
        float halfH = ls.y * 0.5f;
        float minX = lp.x - halfW;
        float maxX = lp.x + halfW;
        float minY = lp.y - halfH;
        float maxY = lp.y + halfH;

        if (isRight)
        {
            if (minX < 0f && (!isFourPiece || Mathf.Abs(lp.y) < tol))
                otherCandidates.Add(rend);
        }
        else if (isLeft)
        {
            if (maxX > 0f && (!isFourPiece || Mathf.Abs(lp.y) < tol))
                otherCandidates.Add(rend);
        }
        else if (isTop)
        {
            if (minY < 0f && (!isFourPiece || Mathf.Abs(lp.x) < tol))
                otherCandidates.Add(rend);
        }
        else if (isBottom)
        {
            if (maxY > 0f && (!isFourPiece || Mathf.Abs(lp.x) < tol))
                otherCandidates.Add(rend);
        }
    }

    foreach (var myRend in myCandidates)
    {
        if (!myRend || !myRend.material) continue;
        Color myColor = myRend.material.color;

        foreach (var otherRend in otherCandidates)
        {
            if (!otherRend || !otherRend.material) continue;
            if (myColor == otherRend.material.color)
            {
                matchedPart = myRend.gameObject;
                otherMatchedPart = otherRend.gameObject;
                return true;
            }
        }
    }

    return false; 
}



        private Vector3 GetMouseWorldPosition()
        {
            Vector3 mousePosition = Input.mousePosition;
            mousePosition.z = UnityEngine.Camera.main.WorldToScreenPoint(transform.position).z;
            return UnityEngine.Camera.main.ScreenToWorldPoint(mousePosition);
        }

        private int GetClosestColumn()
        {
            float closestDistance = float.MaxValue;
            int closestColumn = 0;

            for (var col = 0; col < GridManager.instance.columns; col++)
            {
                float distance = Mathf.Abs(transform.position.x - (col * GridManager.instance.gridSize));
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestColumn = col;
                }
            }
            
            return closestColumn;
        }
    }
}
