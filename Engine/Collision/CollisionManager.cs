using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Hefty.Engine.Collision;

/// <summary>
/// Central motion solver, broadphase, and collision event dispatcher.
/// </summary>
internal static class CollisionManager
{
    private readonly record struct PairKey
    {
        public Collider A { get; }
        public Collider B { get; }

        public PairKey(Collider first, Collider second)
        {
            if (first.Id < second.Id)
                (A, B) = (first, second);
            else
                (A, B) = (second, first);
        }
    }

    private const int GridSize = 100;
    private const float ContactTolerance = 0.001f;
    private const int MaxImpacts = 4;

    private static readonly HashSet<Collider> colliders = [];
    private static readonly HashSet<PhysicsBody> bodies = [];
    private static readonly Dictionary<Collider, PhysicsBody> bodyByCollider = [];
    private static readonly HashSet<PairKey> activePairs = [];
    private static readonly Dictionary<Collider, Aabb> previousBounds = [];
    private static readonly HashSet<Collider> pendingUnregister = [];
    private static bool pendingClear;
    private static int callbackDepth;
    private static bool isChecking;
    private static bool isStepping;

    public static void RegisterCollider(Collider collider)
    {
        ArgumentNullException.ThrowIfNull(collider);
        if (colliders.Add(collider))
            previousBounds[collider] = collider.GetFloatBounds();
    }

    internal static void AttachCollider(PhysicsBody body, Collider collider)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(collider);

        if (!bodies.Contains(body))
            RegisterBody(body);

        if (bodyByCollider.TryGetValue(collider, out PhysicsBody? existingBody)
            && !ReferenceEquals(existingBody, body))
        {
            throw new InvalidOperationException("A collider cannot belong to multiple physics bodies.");
        }

        bodyByCollider[collider] = body;
    }

    public static void RegisterBody(PhysicsBody body)
    {
        ArgumentNullException.ThrowIfNull(body);
        if (!bodies.Add(body))
            return;

        foreach (Collider collider in body.Colliders)
            AttachCollider(body, collider);
    }

    public static void UnregisterBody(PhysicsBody body)
    {
        if (body is null)
            return;

        bodies.Remove(body);
        foreach (Collider collider in body.Colliders)
            if (bodyByCollider.TryGetValue(collider, out PhysicsBody? owner)
                && ReferenceEquals(owner, body))
            {
                bodyByCollider.Remove(collider);
            }
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

        UnregisterColliderNow(collider);
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

    /// <summary>
    /// Consumes kinematic movement intent, resolves solid motion, and dispatches events.
    /// </summary>
    public static void Step(float elapsedSeconds)
    {
        if (elapsedSeconds < 0f || float.IsNaN(elapsedSeconds) || float.IsInfinity(elapsedSeconds))
            throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
        if (isStepping || isChecking)
            return;

        isStepping = true;
        try
        {
            Dictionary<(int X, int Y), List<Collider>> grid = FormGrid(includePreviousBounds: false);
            PhysicsBody[] snapshot = bodies.OrderBy(body => body.Id).ToArray();
            foreach (PhysicsBody body in snapshot)
            {
                if (bodies.Contains(body) && body.Type == BodyType.Kinematic)
                    Solve(body, body.ConsumeMovement(elapsedSeconds), grid);
            }

            CheckCollisionsCore();
        }
        finally
        {
            isStepping = false;
            if (callbackDepth == 0)
                FlushPendingMutations();
        }
    }

    /// <summary>
    /// Performs event-only detection for callers that do not use the physics step.
    /// </summary>
    public static void CheckCollisions()
    {
        if (isStepping || isChecking)
            return;

        isChecking = true;
        try
        {
            CheckCollisionsCore();
        }
        finally
        {
            isChecking = false;
            if (callbackDepth == 0)
                FlushPendingMutations();
        }
    }

    private static void Solve(
        PhysicsBody body,
        Vector2 movement,
        Dictionary<(int X, int Y), List<Collider>> grid)
    {
        Vector2 remaining = movement;
        for (int iteration = 0; iteration < MaxImpacts; iteration++)
        {
            bool hasMovement = remaining.LengthSquared() > 1e-12f;
            float earliestTime = 1f;
            float penetration = 0f;
            Vector2 collisionNormal = Vector2.Zero;
            Collider? hitCollider = null;
            bool startsOverlapping = false;

            foreach (Collider movingCollider in body.Colliders)
            {
                Aabb movingBounds = movingCollider.GetFloatBounds();
                Aabb sweepBounds = Aabb.Union(
                    movingBounds,
                    movingBounds.Translate(remaining)).Expand(ContactTolerance);

                foreach (Collider other in QueryGrid(grid, sweepBounds).OrderBy(collider => collider.Id))
                {
                    if (ReferenceEquals(movingCollider, other)
                        || IsOwnedBySameBody(body, other)
                        || other.IsTrigger
                        || movingCollider.IsTrigger
                        || !LayersMatch(movingCollider, other)
                        || !IsStaticCollider(other))
                    {
                        continue;
                    }

                    if (!Sweep(
                        movingBounds,
                        other.GetFloatBounds(),
                        remaining,
                        out float time,
                        out Vector2 normal,
                        out float overlapDepth,
                        out bool initiallyOverlapping))
                    {
                        continue;
                    }

                    bool isEarlier = time < earliestTime - 1e-7f;
                    bool isTie = MathF.Abs(time - earliestTime) <= 1e-7f
                        && (hitCollider is null || other.Id < hitCollider.Id);
                    if (isEarlier || isTie)
                    {
                        earliestTime = time;
                        penetration = overlapDepth;
                        collisionNormal = normal;
                        hitCollider = other;
                        startsOverlapping = initiallyOverlapping;
                    }
                }
            }

            if (hitCollider is null)
            {
                if (hasMovement)
                    body.Transform.Position += remaining;
                break;
            }

            if (!startsOverlapping && !hasMovement)
                break;

            if (startsOverlapping)
            {
                body.Transform.Position += collisionNormal * (penetration + ContactTolerance);
            }
            else
            {
                float movementLength = remaining.Length();
                float skinTime = movementLength > 0f
                    ? ContactTolerance / movementLength
                    : 0f;
                body.Transform.Position += remaining * MathF.Max(0f, earliestTime - skinTime);
            }

            remaining *= 1f - earliestTime;
            float intoSurface = Vector2.Dot(remaining, collisionNormal);
            if (intoSurface < 0f)
            {
                remaining -= collisionNormal * intoSurface;
                body.RemoveVelocityIntoSurface(collisionNormal);
            }
        }
    }

    private static bool IsStaticCollider(Collider collider) =>
        bodyByCollider.TryGetValue(collider, out PhysicsBody? body)
        && body.Type == BodyType.Static;

    private static bool IsOwnedBySameBody(PhysicsBody body, Collider collider) =>
        bodyByCollider.TryGetValue(collider, out PhysicsBody? owner)
        && ReferenceEquals(owner, body);

    private static bool IsOwnedBySameBody(Collider first, Collider second) =>
        bodyByCollider.TryGetValue(first, out PhysicsBody? firstOwner)
        && bodyByCollider.TryGetValue(second, out PhysicsBody? secondOwner)
        && ReferenceEquals(firstOwner, secondOwner);

    private static IEnumerable<Collider> QueryGrid(
        Dictionary<(int X, int Y), List<Collider>> grid,
        Aabb bounds)
    {
        HashSet<Collider> result = [];
        Aabb expanded = bounds.Expand(ContactTolerance);
        int minX = FloorCell(expanded.Left);
        int maxX = FloorCell(expanded.Right);
        int minY = FloorCell(expanded.Top);
        int maxY = FloorCell(expanded.Bottom);

        for (int x = minX; x <= maxX; x++)
            for (int y = minY; y <= maxY; y++)
                if (grid.TryGetValue((x, y), out List<Collider>? cell))
                    foreach (Collider collider in cell)
                        result.Add(collider);

        return result;
    }

    private static Dictionary<(int X, int Y), List<Collider>> FormGrid(bool includePreviousBounds)
    {
        Dictionary<(int X, int Y), List<Collider>> grid = [];
        foreach (Collider collider in colliders.OrderBy(collider => collider.Id))
        {
            Aabb bounds = collider.GetFloatBounds();
            if (includePreviousBounds && previousBounds.TryGetValue(collider, out Aabb previous))
                bounds = Aabb.Union(bounds, previous);
            bounds = bounds.Expand(ContactTolerance);

            int minX = FloorCell(bounds.Left);
            int maxX = FloorCell(bounds.Right);
            int minY = FloorCell(bounds.Top);
            int maxY = FloorCell(bounds.Bottom);
            for (int x = minX; x <= maxX; x++)
                for (int y = minY; y <= maxY; y++)
                {
                    if (!grid.TryGetValue((x, y), out List<Collider>? cell))
                        grid[(x, y)] = cell = [];
                    cell.Add(collider);
                }
        }

        return grid;
    }

    private static int FloorCell(float coordinate) => (int)MathF.Floor(coordinate / GridSize);

    private static void CheckCollisionsCore()
    {
        Collider[] snapshot = colliders.OrderBy(collider => collider.Id).ToArray();
        Dictionary<(int X, int Y), List<Collider>> grid = FormGrid(includePreviousBounds: true);
        HashSet<PairKey> candidates = [];

        foreach (List<Collider> cell in grid.Values)
        {
            for (int i = 0; i < cell.Count; i++)
                for (int j = i + 1; j < cell.Count; j++)
                    candidates.Add(new PairKey(cell[i], cell[j]));
        }

        HashSet<PairKey> detected = [];
        foreach (PairKey pair in candidates.OrderBy(pair => pair.A.Id).ThenBy(pair => pair.B.Id))
        {
            if (!colliders.Contains(pair.A)
                || !colliders.Contains(pair.B)
                || IsOwnedBySameBody(pair.A, pair.B)
                || !LayersMatch(pair.A, pair.B))
            {
                continue;
            }

            Aabb currentA = pair.A.GetFloatBounds();
            Aabb currentB = pair.B.GetFloatBounds();
            bool hit = currentA.Overlaps(currentB, ContactTolerance)
                || SweptIntersects(
                    previousBounds[pair.A],
                    currentA,
                    previousBounds[pair.B],
                    currentB);
            if (hit)
                detected.Add(pair);
        }

        foreach (PairKey pair in detected.OrderBy(pair => pair.A.Id).ThenBy(pair => pair.B.Id))
        {
            if (!colliders.Contains(pair.A) || !colliders.Contains(pair.B))
                continue;

            if (activePairs.Add(pair))
                Notify(pair, static (self, other) => self.RaiseEntered(other));
            else
                Notify(pair, static (self, other) => self.RaiseStayed(other));
        }

        foreach (PairKey pair in activePairs
            .Except(detected)
            .OrderBy(pair => pair.A.Id)
            .ThenBy(pair => pair.B.Id)
            .ToArray())
        {
            if (activePairs.Remove(pair))
                Notify(pair, static (self, other) => self.RaiseExited(other));
        }

        foreach (Collider collider in snapshot)
            if (colliders.Contains(collider))
                previousBounds[collider] = collider.GetFloatBounds();
    }

    private static bool SweptIntersects(Aabb oldA, Aabb newA, Aabb oldB, Aabb newB)
    {
        // A pair that was already overlapping is exiting if it is no longer touching;
        // do not turn its initial overlap into a new swept hit.
        if (oldA.StrictlyOverlaps(oldB))
            return false;

        Vector2 relativeMovement = new(
            (newA.Left - oldA.Left) - (newB.Left - oldB.Left),
            (newA.Top - oldA.Top) - (newB.Top - oldB.Top));

        return Sweep(
            oldA,
            oldB,
            relativeMovement,
            out _,
            out _,
            out _,
            out bool initiallyOverlapping)
            && !initiallyOverlapping;
    }

    private static bool Sweep(
        Aabb moving,
        Aabb target,
        Vector2 velocity,
        out float time,
        out Vector2 normal,
        out float penetration,
        out bool initiallyOverlapping)
    {
        time = 0f;
        normal = Vector2.Zero;
        penetration = 0f;
        initiallyOverlapping = false;

        if (moving.StrictlyOverlaps(target))
        {
            initiallyOverlapping = true;
            float penetrationX = MathF.Min(moving.Right - target.Left, target.Right - moving.Left);
            float penetrationY = MathF.Min(moving.Bottom - target.Top, target.Bottom - moving.Top);
            Vector2 centerDelta = moving.Center - target.Center;

            if (penetrationX <= penetrationY)
            {
                penetration = penetrationX;
                normal = centerDelta.X < 0f || (centerDelta.X == 0f && velocity.X >= 0f)
                    ? -Vector2.UnitX
                    : Vector2.UnitX;
            }
            else
            {
                penetration = penetrationY;
                normal = centerDelta.Y < 0f || (centerDelta.Y == 0f && velocity.Y >= 0f)
                    ? -Vector2.UnitY
                    : Vector2.UnitY;
            }

            return true;
        }

        float xEntry;
        float xExit;
        if (velocity.X > 0f)
        {
            xEntry = (target.Left - moving.Right) / velocity.X;
            xExit = (target.Right - moving.Left) / velocity.X;
        }
        else if (velocity.X < 0f)
        {
            xEntry = (target.Right - moving.Left) / velocity.X;
            xExit = (target.Left - moving.Right) / velocity.X;
        }
        else if (IntervalsTouch(moving.Left, moving.Right, target.Left, target.Right))
        {
            xEntry = float.NegativeInfinity;
            xExit = float.PositiveInfinity;
        }
        else
        {
            xEntry = float.PositiveInfinity;
            xExit = float.NegativeInfinity;
        }

        float yEntry;
        float yExit;
        if (velocity.Y > 0f)
        {
            yEntry = (target.Top - moving.Bottom) / velocity.Y;
            yExit = (target.Bottom - moving.Top) / velocity.Y;
        }
        else if (velocity.Y < 0f)
        {
            yEntry = (target.Bottom - moving.Top) / velocity.Y;
            yExit = (target.Top - moving.Bottom) / velocity.Y;
        }
        else if (IntervalsTouch(moving.Top, moving.Bottom, target.Top, target.Bottom))
        {
            yEntry = float.NegativeInfinity;
            yExit = float.PositiveInfinity;
        }
        else
        {
            yEntry = float.PositiveInfinity;
            yExit = float.NegativeInfinity;
        }

        float entry = MathF.Max(xEntry, yEntry);
        float exit = MathF.Min(xExit, yExit);
        if (entry > exit || entry < 0f || entry > 1f)
            return false;

        time = entry;
        if (xEntry > yEntry)
            normal = velocity.X > 0f ? -Vector2.UnitX : Vector2.UnitX;
        else if (yEntry > xEntry)
            normal = velocity.Y > 0f ? -Vector2.UnitY : Vector2.UnitY;
        else if (MathF.Abs(velocity.X) >= MathF.Abs(velocity.Y))
            normal = velocity.X > 0f ? -Vector2.UnitX : Vector2.UnitX;
        else
            normal = velocity.Y > 0f ? -Vector2.UnitY : Vector2.UnitY;

        return true;
    }

    private static bool IntervalsTouch(float firstMin, float firstMax, float secondMin, float secondMax) =>
        firstMax >= secondMin && firstMin <= secondMax;

    private static bool LayersMatch(Collider first, Collider second) =>
        (first.CollisionMask & second.Layer) != 0
        && (second.CollisionMask & first.Layer) != 0;

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
            UnregisterColliderNow(collider);
    }

    private static void UnregisterColliderNow(Collider collider)
    {
        if (!colliders.Remove(collider))
            return;

        previousBounds.Remove(collider);
        bodyByCollider.Remove(collider);
        foreach (PairKey pair in activePairs
            .Where(pair => ReferenceEquals(pair.A, collider) || ReferenceEquals(pair.B, collider))
            .ToArray())
        {
            if (activePairs.Remove(pair))
                Notify(pair, static (self, other) => self.RaiseExited(other));
        }
    }

    private static void ClearNow()
    {
        PairKey[] pairs = [.. activePairs];
        activePairs.Clear();
        colliders.Clear();
        bodies.Clear();
        bodyByCollider.Clear();
        previousBounds.Clear();
        pendingUnregister.Clear();

        foreach (PairKey pair in pairs)
            Notify(pair, static (self, other) => self.RaiseExited(other));
    }
}
