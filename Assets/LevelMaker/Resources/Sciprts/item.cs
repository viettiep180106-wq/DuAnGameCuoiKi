using System.Collections.Generic;
using Mono.Cecil.Cil;
using UnityEngine;

public class item : MonoBehaviour
{
    public SpriteRenderer sprite;
    public void Snapping(Dictionary<Vector2, Vector2Int> gridPositions)
    {
        Vector2 currentPos = transform.position;
        Vector2 closestPos = currentPos;
        float minDistance = float.MaxValue;
        foreach (var gridPos in gridPositions.Keys)
        {
            float dist = Vector2.Distance(currentPos, gridPos);
            if (dist < minDistance)
            {
                minDistance = dist;
                closestPos = gridPos;
            }
        }
        
        transform.position = new Vector3(closestPos.x, closestPos.y, 0);
    }
}
