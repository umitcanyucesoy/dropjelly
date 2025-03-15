using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Grid;
using Tiles;
using UnityEngine;

namespace Game
{
    public class MatchManager : MonoBehaviour
    {
        public static MatchManager instance;
        private void Awake() { instance = this; }
        
        public void CheckForMatches()
        {
            for (int row = 0; row < GridManager.instance.rows; row++)
            {
                for (int col = 0; col < GridManager.instance.columns; col++)
                {
                    GameObject tile = GridManager.instance.GetTileAt(row, col);
                    if (tile && tile.GetComponent<Tile>())
                    {
                        if (tile.transform.childCount == 0)
                        {
                            Destroy(tile);
                            continue;
                        }
                        CheckTileMatches(tile, row, col);
                    }
                }
            }
        }
        
        private void CheckTileMatches(GameObject tile, int row, int col)
        {
            Tile tileScript = tile.GetComponent<Tile>();
            List<GameObject> matchingParts = new List<GameObject>();

            GameObject rightTile = GridManager.instance.GetTileAt(row, col + 1);
            GameObject leftTile = GridManager.instance.GetTileAt(row, col - 1);
            GameObject topTile = GridManager.instance.GetTileAt(row - 1, col);
            GameObject bottomTile = GridManager.instance.GetTileAt(row + 1, col);

            if (rightTile && rightTile.GetComponent<Tile>().HasMatchingPart(tileScript, out GameObject rightMatchedPart, out GameObject thisMatchedPartRight))
            {
                matchingParts.Add(rightMatchedPart);
                matchingParts.Add(thisMatchedPartRight);
            }
            if (leftTile && leftTile.GetComponent<Tile>().HasMatchingPart(tileScript, out GameObject leftMatchedPart, out GameObject thisMatchedPartLeft))
            {
                matchingParts.Add(leftMatchedPart);
                matchingParts.Add(thisMatchedPartLeft);
            }
            if (topTile && topTile.GetComponent<Tile>().HasMatchingPart(tileScript, out GameObject topMatchedPart, out GameObject thisMatchedPartTop))
            {
                matchingParts.Add(topMatchedPart);
                matchingParts.Add(thisMatchedPartTop);
            }
            if (bottomTile && bottomTile.GetComponent<Tile>().HasMatchingPart(tileScript, out GameObject bottomMatchedPart, out GameObject thisMatchedPartBottom))
            {
                matchingParts.Add(bottomMatchedPart);
                matchingParts.Add(thisMatchedPartBottom);
            }

            if (matchingParts.Count > 0)
            {
                StartCoroutine(MatchAndTransform(matchingParts));
            }
        }

        private IEnumerator MatchAndTransform(List<GameObject> matchingParts)
        {
            yield return new WaitForSeconds(0.2f);

            Dictionary<GameObject, List<GameObject>> affectedTiles = new Dictionary<GameObject, List<GameObject>>();

            foreach (var part in matchingParts)
            {
                if (!part || !part.transform) 
                    continue;

                GameObject parentTile = part.transform.parent.gameObject;

                if (part.transform)
                {
                    Transform tr = part.transform;
                    var tween = tr.DOScale(Vector3.zero, 0.3f)
                        .SetEase(Ease.InBack)
                        .SetLink(tr.gameObject, LinkBehaviour.KillOnDestroy)
                        .OnUpdate(() =>
                        {
                            if (!tr) return;
                            Vector3 pos = tr.localPosition;
                            pos.x = Mathf.Clamp(pos.x, -0.25f, 0.25f);
                            pos.y = Mathf.Clamp(pos.y, -0.25f, 0.25f);
                            tr.localPosition = pos;
                        })
                        .OnComplete(() =>
                        {
                            if (part) Destroy(part);
                        });
                }

                if (!affectedTiles.ContainsKey(parentTile))
                    affectedTiles[parentTile] = new List<GameObject>();
                affectedTiles[parentTile].Add(part);
            }

            yield return new WaitForSeconds(0.3f);

            foreach (var tile in affectedTiles.Keys)
            {
                if (!tile) continue;
                if (tile.transform.childCount == 0)
                {
                    Destroy(tile);
                }
                else
                {
                    AdjustRemainingParts(tile);
                }
            }

            GridManager.instance.UpdateGrid();
            yield return new WaitForSeconds(0.4f);
            if (HasNewMatches())
                MatchManager.instance.CheckForMatches();
        }

        private void AdjustRemainingParts(GameObject tile)
        {
            if (tile == null) return;

            MeshRenderer[] remainingParts = tile.GetComponentsInChildren<MeshRenderer>();
            int partCount = remainingParts.Length;
            Transform[] partTransforms = new Transform[partCount];
            for (int i = 0; i < partCount; i++)
            {
                if (remainingParts[i])
                    partTransforms[i] = remainingParts[i].transform;
            }

            if (partCount == 3)
            {
                Vector2[] canonPositions = {
                    new Vector2(-0.25f, 0.25f),   
                    new Vector2(0.25f, 0.25f),    
                    new Vector2(-0.25f, -0.25f),  
                    new Vector2(0.25f, -0.25f) 
                };

                List<Vector2> existingPos = new List<Vector2>();
                foreach (var pt in partTransforms)
                {
                    if (!pt) continue;
                    existingPos.Add(new Vector2(pt.localPosition.x, pt.localPosition.y));
                }

                Vector2 missingPos = Vector2.zero;
                bool foundMissing = false;
                foreach (var cp in canonPositions)
                {
                    bool found = false;
                    foreach (var epos in existingPos)
                    {
                        if (Vector2.Distance(cp, epos) < 0.01f)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        missingPos = cp;
                        foundMissing = true;
                        break;
                    }
                }

                if (foundMissing)
                {
                    Transform chunkToExtend = null;
                    if (missingPos.y > 0)
                    {
                        float minY = float.MaxValue;
                        foreach (var pt in partTransforms)
                        {
                            if (Mathf.Abs(pt.localPosition.x - missingPos.x) < 0.01f && pt.localPosition.y < minY)
                            {
                                minY = pt.localPosition.y;
                                chunkToExtend = pt;
                            }
                        }
                        if (chunkToExtend)
                        {
                            Vector3 pos = chunkToExtend.localPosition;
                            pos.y += 0.25f; 
                            chunkToExtend.DOLocalMove(new Vector3(missingPos.x, pos.y, 0f), 0.3f)
                                .SetEase(Ease.OutBounce)
                                .SetLink(chunkToExtend.gameObject, LinkBehaviour.KillOnDestroy)
                                .OnUpdate(() =>
                                {
                                    if (!chunkToExtend) return;
                                    Vector3 tmp = chunkToExtend.localPosition;
                                    tmp.x = Mathf.Clamp(tmp.x, -0.25f, 0.25f);
                                    tmp.y = Mathf.Clamp(tmp.y, -0.25f, 0.25f);
                                    chunkToExtend.localPosition = tmp;
                                })
                                .OnComplete(() => { ClampToParentBounds(chunkToExtend, -0.25f, 0.25f); });

                            chunkToExtend.DOScale(new Vector3(0.5f, 1f, 1f), 0.3f)
                                .SetEase(Ease.OutBounce)
                                .SetLink(chunkToExtend.gameObject, LinkBehaviour.KillOnDestroy)
                                .OnUpdate(() =>
                                {
                                    if (!chunkToExtend) return;
                                    Vector3 tmp = chunkToExtend.localPosition;
                                    tmp.x = Mathf.Clamp(tmp.x, -0.25f, 0.25f);
                                    tmp.y = Mathf.Clamp(tmp.y, -0.25f, 0.25f);
                                    chunkToExtend.localPosition = tmp;
                                })
                                .OnComplete(() => { ClampToParentBounds(chunkToExtend, -0.25f, 0.25f); });
                        }
                    }
                    else if (missingPos.y < 0)
                    {
                        float maxY = float.MinValue;
                        foreach (var pt in partTransforms)
                        {
                            if (Mathf.Abs(pt.localPosition.x - missingPos.x) < 0.01f && pt.localPosition.y > maxY)
                            {
                                maxY = pt.localPosition.y;
                                chunkToExtend = pt;
                            }
                        }
                        if (chunkToExtend != null)
                        {
                            Vector3 pos = chunkToExtend.localPosition;
                            pos.y -= 0.25f;
                            chunkToExtend.DOLocalMove(new Vector3(missingPos.x, pos.y, 0f), 0.3f)
                                .SetEase(Ease.OutBounce)
                                .SetLink(chunkToExtend.gameObject, LinkBehaviour.KillOnDestroy)
                                .OnUpdate(() =>
                                {
                                    if (!chunkToExtend) return;
                                    Vector3 tmp = chunkToExtend.localPosition;
                                    tmp.x = Mathf.Clamp(tmp.x, -0.25f, 0.25f);
                                    tmp.y = Mathf.Clamp(tmp.y, -0.25f, 0.25f);
                                    chunkToExtend.localPosition = tmp;
                                })
                                .OnComplete(() => { ClampToParentBounds(chunkToExtend, -0.25f, 0.25f); });

                            chunkToExtend.DOScale(new Vector3(0.5f, 1f, 1f), 0.3f)
                                .SetEase(Ease.OutBounce)
                                .SetLink(chunkToExtend.gameObject, LinkBehaviour.KillOnDestroy)
                                .OnUpdate(() =>
                                {
                                    if (!chunkToExtend) return;
                                    Vector3 tmp = chunkToExtend.localPosition;
                                    tmp.x = Mathf.Clamp(tmp.x, -0.25f, 0.25f);
                                    tmp.y = Mathf.Clamp(tmp.y, -0.25f, 0.25f);
                                    chunkToExtend.localPosition = tmp;
                                })
                                .OnComplete(() => { ClampToParentBounds(chunkToExtend, -0.25f, 0.25f); });
                        }
                    }
                }
            }
            else if (partCount == 2 || partCount == 3)
            {
                if (partCount == 2)
                {
                    Transform p0 = partTransforms[0];
                    Transform p1 = partTransforms[1];
                    if (p0 == null || p1 == null) return;

                    bool p0Quarter = IsQuarterBlock(p0.localScale);
                    bool p1Quarter = IsQuarterBlock(p1.localScale);
                    bool p0Half = IsHalfBlock(p0.localScale);
                    bool p1Half = IsHalfBlock(p1.localScale);

                    if (p0Quarter && p1Quarter)
                    {
                        if (p0.localPosition.y > 0 && p1.localPosition.y > 0)
                        {
                            p0.DOLocalMoveY(p0.localPosition.y - 0.25f, 0.3f)
                                .SetEase(Ease.OutBounce)
                                .SetLink(p0.gameObject, LinkBehaviour.KillOnDestroy)
                                .OnUpdate(() =>
                                {
                                    if (!p0) return;
                                    Vector3 tmp = p0.localPosition;
                                    tmp.x = Mathf.Clamp(tmp.x, -0.25f, 0.25f);
                                    tmp.y = Mathf.Clamp(tmp.y, -0.25f, 0.25f);
                                    p0.localPosition = tmp;
                                })
                                .OnComplete(() => { ClampToParentBounds(p0, -0.25f, 0.25f); });

                            p1.DOLocalMoveY(p1.localPosition.y - 0.25f, 0.3f)
                                .SetEase(Ease.OutBounce)
                                .SetLink(p1.gameObject, LinkBehaviour.KillOnDestroy)
                                .OnUpdate(() =>
                                {
                                    if (!p1) return;
                                    Vector3 tmp = p1.localPosition;
                                    tmp.x = Mathf.Clamp(tmp.x, -0.25f, 0.25f);
                                    tmp.y = Mathf.Clamp(tmp.y, -0.25f, 0.25f);
                                    p1.localPosition = tmp;
                                })
                                .OnComplete(() => { ClampToParentBounds(p1, -0.25f, 0.25f); });
                        }
                        else if (p0.localPosition.y < 0 && p1.localPosition.y < 0)
                        {
                            p0.DOLocalMoveY(p0.localPosition.y + 0.25f, 0.3f)
                                .SetEase(Ease.OutBounce)
                                .SetLink(p0.gameObject, LinkBehaviour.KillOnDestroy)
                                .OnUpdate(() =>
                                {
                                    if (p0 == null) return;
                                    Vector3 tmp = p0.localPosition;
                                    tmp.x = Mathf.Clamp(tmp.x, -0.25f, 0.25f);
                                    tmp.y = Mathf.Clamp(tmp.y, -0.25f, 0.25f);
                                    p0.localPosition = tmp;
                                })
                                .OnComplete(() => { ClampToParentBounds(p0, -0.25f, 0.25f); });

                            p1.DOLocalMoveY(p1.localPosition.y + 0.25f, 0.3f)
                                .SetEase(Ease.OutBounce)
                                .SetLink(p1.gameObject, LinkBehaviour.KillOnDestroy)
                                .OnUpdate(() =>
                                {
                                    if (!p1) return;
                                    Vector3 tmp = p1.localPosition;
                                    tmp.x = Mathf.Clamp(tmp.x, -0.25f, 0.25f);
                                    tmp.y = Mathf.Clamp(tmp.y, -0.25f, 0.25f);
                                    p1.localPosition = tmp;
                                })
                                .OnComplete(() => { ClampToParentBounds(p1, -0.25f, 0.25f); });
                        }

                        Vector3 newScaleP0 = new Vector3(p0.localScale.x, p0.localScale.y + 0.5f, p0.localScale.z);
                        p0.DOScale(newScaleP0, 0.3f)
                            .SetEase(Ease.OutBounce)
                            .SetLink(p0.gameObject, LinkBehaviour.KillOnDestroy)
                            .OnUpdate(() =>
                            {
                                if (!p0) return;
                                Vector3 tmp = p0.localPosition;
                                tmp.x = Mathf.Clamp(tmp.x, -0.25f, 0.25f);
                                tmp.y = Mathf.Clamp(tmp.y, -0.25f, 0.25f);
                                p0.localPosition = tmp;
                            })
                            .OnComplete(() => { ClampToParentBounds(p0, -0.25f, 0.25f); });

                        Vector3 newScaleP1 = new Vector3(p1.localScale.x, p1.localScale.y + 0.5f, p1.localScale.z);
                        p1.DOScale(newScaleP1, 0.3f)
                            .SetEase(Ease.OutBounce)
                            .SetLink(p1.gameObject, LinkBehaviour.KillOnDestroy)
                            .OnUpdate(() =>
                            {
                                if (!p1) return;
                                Vector3 tmp = p1.localPosition;
                                tmp.x = Mathf.Clamp(tmp.x, -0.25f, 0.25f);
                                tmp.y = Mathf.Clamp(tmp.y, -0.25f, 0.25f);
                                p1.localPosition = tmp;
                            })
                            .OnComplete(() => { ClampToParentBounds(p1, -0.25f, 0.25f); });
                    }
                    else if ((p0Quarter && p1Half) || (p0Half && p1Quarter))
                    {
                        Transform quarter = p0Quarter ? p0 : p1;
                        if (quarter.localPosition.y > 0)
                        {
                            quarter.DOLocalMoveY(quarter.localPosition.y - 0.25f, 0.3f)
                                .SetEase(Ease.OutBounce)
                                .SetLink(quarter.gameObject, LinkBehaviour.KillOnDestroy)
                                .OnUpdate(() =>
                                {
                                    if (!quarter) return;
                                    Vector3 tmp = quarter.localPosition;
                                    tmp.x = Mathf.Clamp(tmp.x, -0.25f, 0.25f);
                                    tmp.y = Mathf.Clamp(tmp.y, -0.25f, 0.25f);
                                    quarter.localPosition = tmp;
                                })
                                .OnComplete(() => { ClampToParentBounds(quarter, -0.25f, 0.25f); });
                        }
                        else
                        {
                            quarter.DOLocalMoveY(quarter.localPosition.y + 0.25f, 0.3f)
                                .SetEase(Ease.OutBounce)
                                .SetLink(quarter.gameObject, LinkBehaviour.KillOnDestroy)
                                .OnUpdate(() =>
                                {
                                    if (!quarter) return;
                                    Vector3 tmp = quarter.localPosition;
                                    tmp.x = Mathf.Clamp(tmp.x, -0.25f, 0.25f);
                                    tmp.y = Mathf.Clamp(tmp.y, -0.25f, 0.25f);
                                    quarter.localPosition = tmp;
                                })
                                .OnComplete(() => { ClampToParentBounds(quarter, -0.25f, 0.25f); });
                        }

                        quarter.DOScale(new Vector3(0.5f, 1f, 1f), 0.3f)
                            .SetEase(Ease.OutBounce)
                            .SetLink(quarter.gameObject, LinkBehaviour.KillOnDestroy)
                            .OnUpdate(() =>
                            {
                                if (!quarter) return;
                                Vector3 tmp = quarter.localPosition;
                                tmp.x = Mathf.Clamp(tmp.x, -0.25f, 0.25f);
                                tmp.y = Mathf.Clamp(tmp.y, -0.25f, 0.25f);
                                quarter.localPosition = tmp;
                            })
                            .OnComplete(() => { ClampToParentBounds(quarter, -0.25f, 0.25f); });
                    }
                }
            }
            else if (partCount == 1)
            {
                Transform singlePart = partTransforms[0];
                if (!singlePart) return;
                Vector3 pos = singlePart.localPosition;
                Vector3 newScale = Vector3.one;
                if (pos.x < 0) pos.x += 0.25f;
                else pos.x -= 0.25f;

               
                singlePart.DOLocalMove(pos, 0.3f)
                    .SetEase(Ease.OutBounce)
                    .SetLink(singlePart.gameObject, LinkBehaviour.KillOnDestroy)
                    .OnUpdate(() =>
                    {
                        if (!singlePart) return;
                        Vector3 tmp = singlePart.localPosition;
                        tmp.x = Mathf.Clamp(tmp.x, -0.25f, 0.25f);
                        tmp.y = Mathf.Clamp(tmp.y, -0.25f, 0.25f);
                        singlePart.localPosition = tmp;
                    })
                    .OnComplete(() => { ClampToParentBounds(singlePart, -0.25f, 0.25f); });

                
                singlePart.DOScale(newScale, 0.3f)
                    .SetEase(Ease.OutBounce)
                    .SetLink(singlePart.gameObject, LinkBehaviour.KillOnDestroy)
                    .OnUpdate(() =>
                    {
                        if (!singlePart) return;
                        Vector3 tmp = singlePart.localPosition;
                        tmp.x = Mathf.Clamp(tmp.x, -0.25f, 0.25f);
                        tmp.y = Mathf.Clamp(tmp.y, -0.25f, 0.25f);
                        singlePart.localPosition = tmp;
                    })
                    .OnComplete(() => { ClampToParentBounds(singlePart, -0.25f, 0.25f); });
            }

            StartCoroutine(FinalSnapAfterTweens(tile, 0.35f));
        }

        private IEnumerator FinalSnapAfterTweens(GameObject tile, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (tile == null) yield break;

            MeshRenderer[] parts = tile.GetComponentsInChildren<MeshRenderer>();
            int n = parts.Length;
            if (n == 0) yield break;

            if (n == 1)
            {
                Transform t = parts[0].transform;
                t.localPosition = Vector3.zero;
                t.localScale = Vector3.one;
            }
            else if (n == 2)
            {
                Transform p0 = parts[0].transform;
                Transform p1 = parts[1].transform;

                bool p0Half = IsHalfBlock(p0.localScale);
                bool p1Half = IsHalfBlock(p1.localScale);

                if (p0Half && p1Half)
                {
                    p0.localPosition = new Vector3(-0.25f, 0f, 0f);
                    p0.localScale = new Vector3(0.5f, 1f, 1f);

                    p1.localPosition = new Vector3(+0.25f, 0f, 0f);
                    p1.localScale = new Vector3(0.5f, 1f, 1f);
                }
            }
            else if (n == 3)
            {
            }

            foreach (var part in parts)
            {
                if (!part) continue;
                ClampToParentBounds(part.transform, -0.25f, 0.25f);
            }
        }

        private void ClampToParentBounds(Transform child, float minVal, float maxVal)
        {
            if (!child) return;
            Vector3 localPos = child.localPosition;
            localPos.x = Mathf.Clamp(localPos.x, minVal, maxVal);
            localPos.y = Mathf.Clamp(localPos.y, minVal, maxVal);
            child.localPosition = localPos;
        }

        private bool IsQuarterBlock(Vector3 sc)
        {
            return (Mathf.Abs(sc.x - 0.5f) < 0.01f && Mathf.Abs(sc.y - 0.5f) < 0.01f);
        }
        private bool IsHalfBlock(Vector3 sc)
        {
            return (Mathf.Abs(sc.x - 0.5f) < 0.01f && Mathf.Abs(sc.y - 1.0f) < 0.01f);
        }

        private bool HasNewMatches()
        {
            for (int row = 0; row < GridManager.instance.rows; row++)
            {
                for (int col = 0; col < GridManager.instance.columns; col++)
                {
                    GameObject tile = GridManager.instance.GetTileAt(row, col);
                    if (tile && tile.GetComponent<Tile>())
                    {
                        if (CheckTileMatchesForVerification(tile, row, col))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        
        private bool CheckTileMatchesForVerification(GameObject tile, int row, int col)
        {
            Tile tileScript = tile.GetComponent<Tile>();
            GameObject rightTile = GridManager.instance.GetTileAt(row, col + 1);
            GameObject topTile = GridManager.instance.GetTileAt(row - 1, col);

            if (rightTile && rightTile.GetComponent<Tile>().HasMatchingPart(tileScript, out _, out _))
                return true;
            if (topTile && topTile.GetComponent<Tile>().HasMatchingPart(tileScript, out _, out _))
                return true;

            return false;
        }
    }
}