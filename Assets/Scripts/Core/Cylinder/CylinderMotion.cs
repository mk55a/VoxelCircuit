namespace CylinderShooter.Core
{
    public static class CylinderMotion
    {
        public static CylinderPosition MoveHorizontally(
            CylinderPosition position, float speed, float radius, float deltaTime)
        {
            return position.Moved(-CylinderSpace.ArcToAngle(speed * deltaTime, radius), 0f);
        }
    }
}
