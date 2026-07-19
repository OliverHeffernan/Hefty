namespace engine;

using System;
using System.Collections.Generic;

public static class CollisionManager
{
    private static readonly HashSet<Collider> colliders = [];
	private static readonly HashSet<(Collider, Collider)> colisionPairs = [];
    private static float GRID_SIZE = 100f;

    public static void RegisterCollider(Collider collider)
    {
        colliders.Add(collider);
    }

    public static void UnregisterCollider(Collider collider)
    {
        colliders.Remove(collider);
    }

    public static void ClearColliders()
    {
        colliders.Clear();
    }

    public static void CheckCollisions()
    {
        Dictionary<(int, int), List<Collider>> grid = FormGrid();

        foreach (var (position, cell) in grid)
        {
            // Check collisions within this cell
            CheckCellAgainstCell(cell, cell, sameCell: true);

            // Only check 4 neighbors to avoid testing every pair twice
            (int x, int y)[] neighbors =
            [
                (position.Item1 + 1, position.Item2),
                (position.Item1, position.Item2 + 1),
                (position.Item1 + 1, position.Item2 + 1),
                (position.Item1 - 1, position.Item2 + 1),
            ];

            foreach (var neighborPosition in neighbors)
            {
                if (grid.TryGetValue(neighborPosition, out var neighborCell))
                {
                    CheckCellAgainstCell(cell, neighborCell, sameCell: false);
                }
            }
        }
    }

    private static void CheckCellAgainstCell(
        List<Collider> cellA,
        List<Collider> cellB,
        bool sameCell
    )
    {
        for (int i = 0; i < cellA.Count; i++)
        {
            int startJ = sameCell ? i + 1 : 0;

            for (int j = startJ; j < cellB.Count; j++)
            {
                var a = cellA[i];
                var b = cellB[j];

                if (a.Intersects(b) && !colisionPairs.Contains((a, b)) && !colisionPairs.Contains((b, a)))
                {
					colisionPairs.Add((a, b));
                    a.OnCollisionEnter?.Invoke(b);
                    b.OnCollisionEnter?.Invoke(a);
                }
				else if (!a.Intersects(b) && (colisionPairs.Contains((a, b)) || colisionPairs.Contains((b, a))))
				{
					colisionPairs.Remove((a, b));
					colisionPairs.Remove((b, a));
					a.OnCollisionExit?.Invoke(b);
					b.OnCollisionExit?.Invoke(a);
				}
            }
        }
    }

    private static Dictionary<(int, int), List<Collider>> FormGrid()
    {
        Dictionary<(int, int), List<Collider>> grid = [];

        foreach (var collider in colliders)
        {
            var gridX = (int)(collider.Transform.Position.X / GRID_SIZE);
            var gridY = (int)(collider.Transform.Position.Y / GRID_SIZE);
            var key = (gridX, gridY);

            if (!grid.TryGetValue(key, out var value))
            {
                value = [];
                grid[key] = value;
            }

            value.Add(collider);
        }

        return grid;
    }
}
