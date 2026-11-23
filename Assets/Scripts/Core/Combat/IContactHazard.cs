namespace CylinderShooter.Core
{
    public interface IContactHazard
    {
        bool IsAlive { get; }
        float Radius { get; }
        CylinderPosition PreviousContactPosition { get; }
        CylinderPosition ContactPosition { get; }
        void DestroyOnContact();
    }
}
