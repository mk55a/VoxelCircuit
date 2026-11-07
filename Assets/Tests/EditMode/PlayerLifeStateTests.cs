using System;
using CylinderShooter.Core;
using NUnit.Framework;

namespace CylinderShooter.Tests
{
    public class PlayerLifeStateTests
    {
        [Test]
        public void Destroy_RespawnsAfterDelayWithInvulnerability()
        {
            var state = new PlayerLifeState(3, 10f, 2f);
            Assert.IsTrue(state.TryDestroy());
            Assert.AreEqual(2, state.Lives);
            Assert.IsFalse(state.TryDestroy());
            state.Tick(9.9f);
            Assert.AreEqual(PlayerStatus.Respawning, state.Status);
            state.Tick(0.11f);
            Assert.AreEqual(PlayerStatus.Invulnerable, state.Status);
            Assert.IsFalse(state.TryDestroy());
            state.Tick(2f);
            Assert.AreEqual(PlayerStatus.Alive, state.Status);
        }

        [Test]
        public void ZeroLives_StillRespawnsAndCanBeAdded()
        {
            var state = new PlayerLifeState(1, 0f, 0f);
            state.TryDestroy();
            state.Tick(0f);
            Assert.AreEqual(0, state.Lives);
            Assert.IsTrue(state.TryDestroy());
            state.Tick(0f);
            Assert.AreEqual(0, state.Lives);
            state.AddLives(4);
            state.RemoveLives(2);
            Assert.AreEqual(2, state.Lives);
            state.RemoveLives(20);
            Assert.AreEqual(0, state.Lives);
            Assert.Throws<ArgumentOutOfRangeException>(() => state.AddLives(-1));
        }

        [Test]
        public void SweptContact_DetectsCrossingAndSeam()
        {
            var player = new CylinderPosition(0.04f, 0f);
            var end = new CylinderPosition(-0.04f, 0f);
            var enemy = new CylinderPosition(0f, 0f);
            Assert.IsTrue(CylinderContact.SweptSpheres(player, end, 0.1f, enemy, enemy, 0.1f, 20f));
            Assert.IsFalse(CylinderContact.SweptSpheres(player, end, 0.1f,
                new CylinderPosition(0f, 5f), new CylinderPosition(0f, 5f), 0.1f, 20f));
        }
    }
}
