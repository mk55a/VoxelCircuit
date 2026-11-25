namespace CylinderShooter.Core
{
    /// <summary>
    /// How a proximity fuse behaves (the Bomber's, and any later enemy that arms and explodes).
    /// Implemented by a tuning asset so the fuse logic stays pure and testable.
    /// </summary>
    public interface IFuseSettings
    {
        /// <summary>The fuse arms when the player comes this close, in metres along the cylinder surface.</summary>
        float TriggerRange { get; }

        /// <summary>Seconds from arming to the explosion: the player's window to escape or shoot it.</summary>
        float FuseDuration { get; }

        /// <summary>Lamp blinks per second when the fuse has just armed.</summary>
        float BlinkRateStart { get; }

        /// <summary>Lamp blinks per second just before the explosion. Faster than the start, so the player hears and sees it coming.</summary>
        float BlinkRateEnd { get; }
    }
}
