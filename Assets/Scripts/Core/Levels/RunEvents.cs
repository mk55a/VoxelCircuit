using System;

namespace CylinderShooter.Core
{
    /// <summary>Which wave an event is about.</summary>
    public readonly struct WaveInfo
    {
        public WaveInfo(int index, string name)
        {
            Index = index;
            Name = name;
        }

        public int Index { get; }
        public string Name { get; }
    }

    /// <summary>Which stage an event is about.</summary>
    public readonly struct StageInfo
    {
        public StageInfo(int waveIndex, int stageIndex, string name)
        {
            WaveIndex = waveIndex;
            StageIndex = stageIndex;
            Name = name;
        }

        public int WaveIndex { get; }
        public int StageIndex { get; }
        public string Name { get; }
    }

    /// <summary>
    /// Moments of a run that UI, power-ups, music or recordings react to. Listeners subscribe; the run doesn't know
    /// who they are (same idea as <see cref="IDestructible"/>).
    /// </summary>
    public interface IRunEvents
    {
        event Action RunStarted;
        event Action<WaveInfo> WaveStarted;
        event Action<StageInfo> StageStarted;
        event Action<StageInfo, EndReason> StageEnded;
        event Action<WaveInfo, EndReason> WaveEnded;
        event Action<EndReason> RunEnded;
    }
}
