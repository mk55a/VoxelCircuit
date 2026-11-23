using CylinderShooter.Core;
using NUnit.Framework;

namespace CylinderShooter.Tests
{
    public class FireSideSelectorTests
    {
        [Test]
        public void NothingHeld_DoesNotFire()
        {
            Assert.AreEqual(FireSideSelector.None, new FireSideSelector().Update(false, false));
        }

        [Test]
        public void LeftHeld_FiresLeft()
        {
            Assert.AreEqual(FireSideSelector.Left, new FireSideSelector().Update(true, false));
        }

        [Test]
        public void RightHeld_FiresRight()
        {
            Assert.AreEqual(FireSideSelector.Right, new FireSideSelector().Update(false, true));
        }

        [Test]
        public void BothHeld_TheMostRecentPressWins()
        {
            var selector = new FireSideSelector();
            selector.Update(true, false);
            Assert.AreEqual(FireSideSelector.Right, selector.Update(true, true), "pressing right while holding left switches to right");
            Assert.AreEqual(FireSideSelector.Right, selector.Update(true, true), "and stays right while both are held");
        }

        [Test]
        public void ReleasingTheNewerButton_HandsBackToTheOlderOne()
        {
            var selector = new FireSideSelector();
            selector.Update(true, false);
            selector.Update(true, true);
            Assert.AreEqual(FireSideSelector.Left, selector.Update(true, false));
        }

        [Test]
        public void BothPressedOnTheSameFrame_StillFiresOneSide()
        {
            int side = new FireSideSelector().Update(true, true);
            Assert.AreNotEqual(FireSideSelector.None, side);
        }

        [Test]
        public void ReleasingBoth_StopsFiring()
        {
            var selector = new FireSideSelector();
            selector.Update(false, true);
            Assert.AreEqual(FireSideSelector.None, selector.Update(false, false));
        }

        [Test]
        public void Reset_ForgetsHeldButtons()
        {
            var selector = new FireSideSelector();
            selector.Update(true, false);
            selector.Reset();
            Assert.AreEqual(FireSideSelector.None, selector.Current);
            Assert.AreEqual(FireSideSelector.Left, selector.Update(true, false), "a held button counts as a fresh press after a reset");
        }
    }
}
