using UnityEngine;

namespace CylinderShooter.Core
{
    public static class ChaserSteering
    {
        private const float DirectionEpsilon = 0.000001f;

        public static float TurnTowards(float headingDegrees, CylinderPosition position,
            CylinderPosition target, float radius, float turnSpeed, float deltaTime)
        {
            float horizontal = -CylinderSpace.ShortestDelta(position.Angle, target.Angle) * radius;
            float vertical = target.Height - position.Height;
            if (horizontal * horizontal + vertical * vertical < DirectionEpsilon)
            {
                return headingDegrees;
            }

            float desiredHeading = Mathf.Atan2(vertical, horizontal) * Mathf.Rad2Deg;
            return Mathf.MoveTowardsAngle(headingDegrees, desiredHeading, turnSpeed * deltaTime);
        }

        public static float TurnWithinBounds(float headingDegrees, CylinderPosition position,
            CylinderPosition target, float radius, float speed, float turnSpeed, float deltaTime,
            float minHeight, float maxHeight)
        {
            float pursuitHeading = TurnTowards(headingDegrees, position, target, radius, turnSpeed, deltaTime);
            float verticalDirection = Mathf.Sin(pursuitHeading * Mathf.Deg2Rad);
            if (Mathf.Abs(verticalDirection) < DirectionEpsilon || speed <= 0f || deltaTime <= 0f)
            {
                return pursuitHeading;
            }

            float distanceToEdge = verticalDirection > 0f
                ? maxHeight - position.Height
                : position.Height - minHeight;
            float nextStep = speed * deltaTime * Mathf.Abs(verticalDirection);
            float turnRadians = turnSpeed * Mathf.Deg2Rad;
            float turningDistance = turnRadians > 0f
                ? speed / turnRadians * (1f - Mathf.Abs(Mathf.Cos(pursuitHeading * Mathf.Deg2Rad)))
                : float.PositiveInfinity;

            // Reserve one additional step for discrete frame integration.
            if (distanceToEdge > nextStep + turningDistance + speed * deltaTime)
            {
                return pursuitHeading;
            }

            float horizontalDirection = Mathf.Cos(headingDegrees * Mathf.Deg2Rad);
            if (Mathf.Abs(horizontalDirection) < DirectionEpsilon)
            {
                horizontalDirection = -CylinderSpace.ShortestDelta(position.Angle, target.Angle);
            }

            float tangentHeading = horizontalDirection >= 0f ? 0f : 180f;
            return Mathf.MoveTowardsAngle(headingDegrees, tangentHeading, turnSpeed * deltaTime);
        }

        public static CylinderPosition MoveWithinBounds(CylinderPosition position, float headingDegrees,
            float speed, float radius, float deltaTime, float minHeight, float maxHeight)
        {
            var next = Move(position, headingDegrees, speed, radius, deltaTime);
            return next.WithHeight(Mathf.Clamp(next.Height, minHeight, maxHeight));
        }

        public static CylinderPosition Move(CylinderPosition position, float headingDegrees,
            float speed, float radius, float deltaTime)
        {
            float heading = headingDegrees * Mathf.Deg2Rad;
            float distance = speed * deltaTime;
            return position.Moved(-Mathf.Cos(heading) * distance / radius,
                Mathf.Sin(heading) * distance);
        }
    }
}
