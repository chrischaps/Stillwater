using System;
using NUnit.Framework;
using Stillwater.Core;

namespace Stillwater.Tests
{
    [TestFixture]
    public class InputEventsTests
    {
        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Clear();
        }

        [Test]
        public void CastInputEvent_CanBePublishedAndReceived()
        {
            bool received = false;
            EventBus.Subscribe<CastInputEvent>(e => received = true);

            EventBus.Publish(new CastInputEvent());

            Assert.IsTrue(received);
        }

        [Test]
        public void ReelStartedEvent_CanBePublishedAndReceived()
        {
            bool received = false;
            EventBus.Subscribe<ReelStartedEvent>(e => received = true);

            EventBus.Publish(new ReelStartedEvent());

            Assert.IsTrue(received);
        }

        [Test]
        public void ReelEndedEvent_CanBePublishedAndReceived()
        {
            bool received = false;
            EventBus.Subscribe<ReelEndedEvent>(e => received = true);

            EventBus.Publish(new ReelEndedEvent());

            Assert.IsTrue(received);
        }

        [Test]
        public void SlackInputEvent_CanBePublishedAndReceived()
        {
            bool received = false;
            EventBus.Subscribe<SlackInputEvent>(e => received = true);

            EventBus.Publish(new SlackInputEvent());

            Assert.IsTrue(received);
        }

        [Test]
        public void InteractInputEvent_CanBePublishedAndReceived()
        {
            bool received = false;
            EventBus.Subscribe<InteractInputEvent>(e => received = true);

            EventBus.Publish(new InteractInputEvent());

            Assert.IsTrue(received);
        }

        [Test]
        public void CancelInputEvent_CanBePublishedAndReceived()
        {
            bool received = false;
            EventBus.Subscribe<CancelInputEvent>(e => received = true);

            EventBus.Publish(new CancelInputEvent());

            Assert.IsTrue(received);
        }

        [Test]
        public void MultipleInputEvents_AreIndependent()
        {
            int castCount = 0;
            int reelStartCount = 0;
            int interactCount = 0;

            EventBus.Subscribe<CastInputEvent>(e => castCount++);
            EventBus.Subscribe<ReelStartedEvent>(e => reelStartCount++);
            EventBus.Subscribe<InteractInputEvent>(e => interactCount++);

            EventBus.Publish(new CastInputEvent());
            EventBus.Publish(new CastInputEvent());
            EventBus.Publish(new ReelStartedEvent());

            Assert.AreEqual(2, castCount);
            Assert.AreEqual(1, reelStartCount);
            Assert.AreEqual(0, interactCount);
        }

        [Test]
        public void ReelStartAndEnd_CanTrackReelState()
        {
            bool isReeling = false;

            EventBus.Subscribe<ReelStartedEvent>(e => isReeling = true);
            EventBus.Subscribe<ReelEndedEvent>(e => isReeling = false);

            Assert.IsFalse(isReeling);

            EventBus.Publish(new ReelStartedEvent());
            Assert.IsTrue(isReeling);

            EventBus.Publish(new ReelEndedEvent());
            Assert.IsFalse(isReeling);
        }
    }
}
