using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Hefty.Engine.Collision;

public static class CollisionManager
{
    private readonly record struct PairKey
    {
        public Collider A { get; }
        public Collider B { get; }

        public PairKey(Collider a, Collider b)
        {
            if (a.Id < b.Id) (A, B) = (a, b);
            else (A, B) = (b, a);
        }
    }

    private const int GridSize = 100;
    private static readonly HashSet<Collider> colliders = [];
    private static readonly HashSet<PairKey> activePairs = [];
    private static readonly Dictionary<Collider, Rectangle> previousBounds = [];
    private static readonly HashSet<Collider> pendingUnregister = [];
    private static bool pendingClear;
    private static int callbackDepth;
    private static bool isChecking;

    public static void RegisterCollider(Collider collider)
    {
        ArgumentNullException.ThrowIfNull(collider);
        if (colliders.Add(collider))
            previousBounds[collider] = collider.GetBounds();
    }

    public static void UnregisterCollider(Collider collider)
    {
        if (collider is null)
            return;
        if (callbackDepth > 0)
        {
            pendingUnregister.Add(collider);
            return;
        }
        UnregisterNow(collider);
    }

    public static void ClearColliders()
    {
        if (callbackDepth > 0)
        {
            pendingClear = true;
            return;
        }
        ClearNow();
    }

    public static void CheckCollisions()
    {
        if (isChecking)
            return;

        isChecking = true;
        try
        {
            CheckCollisionsCore();
        }
        finally
        {
            isChecking = false;
        }
    }

    private static void CheckCollisionsCore()
    {
        Collider[] snapshot = [.. colliders];
        Dictionary<(int, int), List<Collider>> grid = FormGrid(snapshot);
        HashSet<PairKey> candidates = [];

        foreach (List<Collider> cell in grid.Values)
        {
            for (int i = 0; i < cell.Count; i++)
                for (int j = i + 1; j < cell.Count; j++)
                    candidates.Add(new PairKey(cell[i], cell[j]));
        }

        HashSet<PairKey> detected = [];
        foreach (PairKey pair in candidates)
        {
            if (!colliders.Contains(pair.A) || !colliders.Contains(pair.B) || !LayersMatch(pair.A, pair.B))
                continue;

            Rectangle currentA = pair.A.GetBounds();
            Rectangle currentB = pair.B.GetBounds();
            bool overlaps = currentA.Intersects(currentB);
            bool hit = overlaps;
            if (!hit && !activePairs.Contains(pair))
                hit = SweptIntersects(previousBounds[pair.A], currentA, previousBounds[pair.B], currentB);
            if (hit)
                detected.Add(pair);
        }

        foreach (PairKey pair in detected)
        {
            if (!colliders.Contains(pair.A) || !colliders.Contains(pair.B))
                continue;
            if (activePairs.Add(pair))
                Notify(pair, static (self, other) => self.OnCollisionEnter?.Invoke(other));
            else
                Notify(pair, static (self, other) => self.OnCollisionStay?.Invoke(other));
        }

        PairKey[] oldPairs = [.. activePairs];
        foreach (PairKey pair in oldPairs)
        {
            if (!detected.Contains(pair) && activePairs.Remove(pair))
                Notify(pair, static (self, other) => self.OnCollisionExit?.Invoke(other));
        }

        foreach (Collider collider in snapshot)
            if (colliders.Contains(collider))
                previousBounds[collider] = collider.GetBounds();
    }

    private static Dictionary<(int, int), List<Collider>> FormGrid(Collider[] snapshot)
    {
        Dictionary<(int, int), List<Collider>> grid = [];
        foreach (Collider collider in snapshot)
        {
            Rectangle bounds = Rectangle.Union(previousBounds[collider], collider.GetBounds());
            int minX = FloorDiv(bounds.Left, GridSize);
            int maxX = FloorDiv(bounds.Right - 1, GridSize);
            int minY = FloorDiv(bounds.Top, GridSize);
            int maxY = FloorDiv(bounds.Bottom - 1, GridSize);
            for (int x = minX; x <= maxX; x++)
            for (int y = minY; y <= maxY; y++)
            {
                if (!grid.TryGetValue((x, y), out List<Collider> cell))
                    grid[(x, y)] = cell = [];
                cell.Add(collider);
            }
        }
        return grid;
    }

    private static int FloorDiv(int value, int divisor) => (int)MathF.Floor((float)value / divisor);

    private static bool LayersMatch(Collider a, Collider b) =>
        (a.CollisionMask & b.Layer) != 0 && (b.CollisionMask & a.Layer) != 0;

    private static bool SweptIntersects(Rectangle oldA, Rectangle newA, Rectangle oldB, Rectangle newB)
    {
        float relativeX = (newA.X - oldA.X) - (newB.X - oldB.X);
        float relativeY = (newA.Y - oldA.Y) - (newB.Y - oldB.Y);
        float entryX = AxisEntry(oldA.Left, oldA.Right, oldB.Left, oldB.Right, relativeX);
        float exitX = AxisExit(oldA.Left, oldA.Right, oldB.Left, oldB.Right, relativeX);
        float entryY = AxisEntry(oldA.Top, oldA.Bottom, oldB.Top, oldB.Bottom, relativeY);
        float exitY = AxisExit(oldA.Top, oldA.Bottom, oldB.Top, oldB.Bottom, relativeY);
        float entry = MathF.Max(entryX, entryY);
        float exit = MathF.Min(exitX, exitY);
        return entry <= exit && entry >= 0f && entry <= 1f;
    }

    private static float AxisEntry(float aMin, float aMax, float bMin, float bMax, float velocity)
    {
        if (velocity > 0) return (bMin - aMax) / velocity;
        if (velocity < 0) return (bMax - aMin) / velocity;
        return aMax > bMin && aMin < bMax ? float.NegativeInfinity : float.PositiveInfinity;
    }

    private static float AxisExit(float aMin, float aMax, float bMin, float bMax, float velocity)
    {
        if (velocity > 0) return (bMax - aMin) / velocity;
        if (velocity < 0) return (bMin - aMax) / velocity;
        return aMax > bMin && aMin < bMax ? float.PositiveInfinity : float.NegativeInfinity;
    }

    private static void Notify(PairKey pair, Action<Collider, Collider> callback)
    {
        callbackDepth++;
        try
        {
            callback(pair.A, pair.B);
            callback(pair.B, pair.A);
        }
        finally
        {
            callbackDepth--;
            if (callbackDepth == 0)
                FlushPendingMutations();
        }
    }

    private static void FlushPendingMutations()
    {
        if (pendingClear)
        {
            pendingClear = false;
            pendingUnregister.Clear();
            ClearNow();
            return;
        }
        Collider[] removals = [.. pendingUnregister];
        pendingUnregister.Clear();
        foreach (Collider collider in removals)
            UnregisterNow(collider);
    }

    private static void UnregisterNow(Collider collider)
    {
        if (!colliders.Remove(collider))
            return;
        previousBounds.Remove(collider);
        PairKey[] pairs = [.. activePairs];
        foreach (PairKey pair in pairs)
            if ((ReferenceEquals(pair.A, collider) || ReferenceEquals(pair.B, collider)) && activePairs.Remove(pair))
                Notify(pair, static (self, other) => self.OnCollisionExit?.Invoke(other));
    }

    private static void ClearNow()
    {
        PairKey[] pairs = [.. activePairs];
        activePairs.Clear();
        colliders.Clear();
        previousBounds.Clear();
        foreach (PairKey pair in pairs)
            Notify(pair, static (self, other) => self.OnCollisionExit?.Invoke(other));
    }
}
