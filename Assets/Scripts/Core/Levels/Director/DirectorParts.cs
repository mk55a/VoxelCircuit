using System;
using System.Collections.Generic;
using UnityEngine;

namespace CylinderShooter.Core
{
    /// <summary>Random numbers for the director, behind an interface so tests can fix them and runs can be seeded later.</summary>
    public interface IRandomSource
    {
        /// <summary>A number from 0 (included) to 1 (excluded).</summary>
        float Value();
    }

    public sealed class SeededRandom : IRandomSource
    {
        private readonly System.Random _random;

        public SeededRandom(int seed) => _random = new System.Random(seed);

        public float Value() => (float)_random.NextDouble();
    }

    /// <summary>
    /// The director's credits: earned over time up to a cap, spent on groups. Separate from the director so the
    /// saving rules can be tested on their own.
    /// </summary>
    public sealed class CreditWallet
    {
        public float Credits { get; private set; }

        public void Reset(float credits) => Credits = Mathf.Max(0f, credits);

        /// <summary>Adds credits without going over the cap (0 = no cap). Credits already above a lowered cap stay.</summary>
        public void Earn(float amount, float cap)
        {
            if (amount <= 0f) return;
            float next = Credits + amount;
            if (cap > 0f) next = Mathf.Min(next, Mathf.Max(cap, Credits));
            Credits = next;
        }

        public bool CanAfford(float cost) => Credits + 1e-4f >= cost;

        public void Spend(float cost) => Credits = Mathf.Max(0f, Credits - Mathf.Max(0f, cost));
    }

    /// <summary>
    /// Remembers when each enemy or formation was last picked and lowers its chance for a while, so the same one isn't
    /// picked several times in a row. It never forbids anything: the chance recovers fully over time.
    /// </summary>
    public sealed class VarietyMemory
    {
        private readonly Dictionary<object, float> _lastPicked = new Dictionary<object, float>();

        public void Clear() => _lastPicked.Clear();

        public void Mark(object key, float now)
        {
            if (key != null) _lastPicked[key] = now;
        }

        /// <summary>1 = never picked or fully recovered; <paramref name="dip"/> = just picked.</summary>
        public float Factor(object key, float now, float dip, float recovery)
        {
            if (key == null || !_lastPicked.TryGetValue(key, out float time)) return 1f;
            dip = Mathf.Clamp01(dip);
            if (recovery <= 0f) return 1f;
            float t = Mathf.Clamp01((now - time) / recovery);
            return dip + (1f - dip) * t;
        }
    }

    public static class WeightedPick
    {
        /// <summary>Index picked in proportion to the weights, or -1 when every weight is 0. Roll is 0 to 1.</summary>
        public static int Choose(IReadOnlyList<float> weights, float roll)
        {
            float total = 0f;
            for (int i = 0; i < weights.Count; i++) total += Mathf.Max(0f, weights[i]);
            if (total <= 0f) return -1;
            float target = Mathf.Clamp01(roll) * total;
            int last = -1;
            for (int i = 0; i < weights.Count; i++)
            {
                float w = Mathf.Max(0f, weights[i]);
                if (w <= 0f) continue;
                last = i;
                if (target < w) return i;
                target -= w;
            }
            return last;
        }
    }

    /// <summary>
    /// Where the player has been spending time: the arena's height split into three lanes (bottom, middle, top), with
    /// older time fading out. The director crowds the lane the player favours, to make them move.
    /// </summary>
    public sealed class LaneTracker
    {
        public const int LaneCount = 3;
        private readonly float[] _time = new float[LaneCount];
        private readonly float _minHeight;
        private readonly float _maxHeight;

        public LaneTracker(float minHeight, float maxHeight)
        {
            if (!(maxHeight > minHeight)) throw new ArgumentException("maxHeight must be above minHeight.");
            _minHeight = minHeight;
            _maxHeight = maxHeight;
        }

        public void Clear() => Array.Clear(_time, 0, LaneCount);

        public int LaneOf(float height)
        {
            float t = (height - _minHeight) / (_maxHeight - _minHeight);
            return Mathf.Clamp((int)(t * LaneCount), 0, LaneCount - 1);
        }

        public float CenterOf(int lane) =>
            _minHeight + (Mathf.Clamp(lane, 0, LaneCount - 1) + 0.5f) * (_maxHeight - _minHeight) / LaneCount;

        /// <param name="memory">Seconds of history that count; older time fades exponentially.</param>
        public void Tick(float deltaTime, float playerHeight, float memory)
        {
            if (deltaTime <= 0f) return;
            float keep = memory > 0f ? Mathf.Exp(-deltaTime / memory) : 0f;
            for (int i = 0; i < LaneCount; i++) _time[i] *= keep;
            _time[LaneOf(playerHeight)] += deltaTime;
        }

        /// <summary>The lane with the most recent time. Ties go to the lower lane.</summary>
        public int Favourite
        {
            get
            {
                int best = 0;
                for (int i = 1; i < LaneCount; i++)
                    if (_time[i] > _time[best]) best = i;
                return best;
            }
        }

        /// <summary>0 to 1: how much of the remembered time is in this lane.</summary>
        public float Share(int lane)
        {
            float total = 0f;
            for (int i = 0; i < LaneCount; i++) total += _time[i];
            return total > 0f ? _time[Mathf.Clamp(lane, 0, LaneCount - 1)] / total : 0f;
        }
    }

    /// <summary>
    /// A hot streak: many kills in a short time without being hit. Kills fade over a window; a hit ends the streak.
    /// While hot, the director earns credits faster, so a player who is doing well keeps getting something to destroy.
    /// </summary>
    public sealed class StreakMeter
    {
        /// <summary>Recent kills, fading over time.</summary>
        public float Score { get; private set; }

        public void Clear() => Score = 0f;

        public void Kill() => Score += 1f;

        public void Hit() => Score = 0f;

        public void Tick(float deltaTime, float window)
        {
            if (deltaTime <= 0f) return;
            Score = window > 0f ? Score * Mathf.Exp(-deltaTime / window) : 0f;
        }

        public bool IsHot(float threshold) => threshold > 0f && Score >= threshold;
    }
}
