using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Stillwater.Core;

namespace Stillwater.Tests
{
    [TestFixture]
    public class EventBusTests
    {
        // Test event types
        private struct TestEvent
        {
            public string Message;
            public int Value;
        }

        private struct AnotherEvent
        {
            public float Data;
        }

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
        public void Subscribe_And_Publish_InvokesHandler()
        {
            TestEvent? received = null;
            Action<TestEvent> handler = e => received = e;

            EventBus.Subscribe(handler);
            EventBus.Publish(new TestEvent { Message = "Hello", Value = 42 });

            Assert.IsNotNull(received);
            Assert.AreEqual("Hello", received.Value.Message);
            Assert.AreEqual(42, received.Value.Value);
        }

        [Test]
        public void Unsubscribe_RemovesHandler()
        {
            int callCount = 0;
            Action<TestEvent> handler = e => callCount++;

            EventBus.Subscribe(handler);
            EventBus.Publish(new TestEvent());
            Assert.AreEqual(1, callCount);

            EventBus.Unsubscribe(handler);
            EventBus.Publish(new TestEvent());
            Assert.AreEqual(1, callCount); // Should not increase
        }

        [Test]
        public void Publish_WithNoSubscribers_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                EventBus.Publish(new TestEvent { Message = "No one listening" });
            });
        }

        [Test]
        public void MultipleSubscribers_AllReceiveEvent()
        {
            int handler1Calls = 0;
            int handler2Calls = 0;
            int handler3Calls = 0;

            EventBus.Subscribe<TestEvent>(e => handler1Calls++);
            EventBus.Subscribe<TestEvent>(e => handler2Calls++);
            EventBus.Subscribe<TestEvent>(e => handler3Calls++);

            EventBus.Publish(new TestEvent());

            Assert.AreEqual(1, handler1Calls);
            Assert.AreEqual(1, handler2Calls);
            Assert.AreEqual(1, handler3Calls);
        }

        [Test]
        public void DifferentEventTypes_AreIndependent()
        {
            int testEventCalls = 0;
            int anotherEventCalls = 0;

            EventBus.Subscribe<TestEvent>(e => testEventCalls++);
            EventBus.Subscribe<AnotherEvent>(e => anotherEventCalls++);

            EventBus.Publish(new TestEvent());

            Assert.AreEqual(1, testEventCalls);
            Assert.AreEqual(0, anotherEventCalls);

            EventBus.Publish(new AnotherEvent());

            Assert.AreEqual(1, testEventCalls);
            Assert.AreEqual(1, anotherEventCalls);
        }

        [Test]
        public void Subscribe_SameHandlerTwice_OnlyCalledOnce()
        {
            int callCount = 0;
            Action<TestEvent> handler = e => callCount++;

            EventBus.Subscribe(handler);
            EventBus.Subscribe(handler); // Duplicate

            EventBus.Publish(new TestEvent());

            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void Subscribe_Null_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                EventBus.Subscribe<TestEvent>(null);
            });
        }

        [Test]
        public void Unsubscribe_Null_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                EventBus.Unsubscribe<TestEvent>(null);
            });
        }

        [Test]
        public void Unsubscribe_NotSubscribed_DoesNotThrow()
        {
            Action<TestEvent> handler = e => { };

            Assert.DoesNotThrow(() =>
            {
                EventBus.Unsubscribe(handler);
            });
        }

        [Test]
        public void Clear_RemovesAllSubscribers()
        {
            EventBus.Subscribe<TestEvent>(e => { });
            EventBus.Subscribe<AnotherEvent>(e => { });

            Assert.AreEqual(1, EventBus.GetSubscriberCount<TestEvent>());
            Assert.AreEqual(1, EventBus.GetSubscriberCount<AnotherEvent>());

            EventBus.Clear();

            Assert.AreEqual(0, EventBus.GetSubscriberCount<TestEvent>());
            Assert.AreEqual(0, EventBus.GetSubscriberCount<AnotherEvent>());
        }

        [Test]
        public void ClearGeneric_RemovesOnlySpecificType()
        {
            EventBus.Subscribe<TestEvent>(e => { });
            EventBus.Subscribe<AnotherEvent>(e => { });

            EventBus.Clear<TestEvent>();

            Assert.AreEqual(0, EventBus.GetSubscriberCount<TestEvent>());
            Assert.AreEqual(1, EventBus.GetSubscriberCount<AnotherEvent>());
        }

        [Test]
        public void GetSubscriberCount_ReturnsCorrectCount()
        {
            Assert.AreEqual(0, EventBus.GetSubscriberCount<TestEvent>());

            EventBus.Subscribe<TestEvent>(e => { });
            Assert.AreEqual(1, EventBus.GetSubscriberCount<TestEvent>());

            EventBus.Subscribe<TestEvent>(e => { });
            Assert.AreEqual(2, EventBus.GetSubscriberCount<TestEvent>());
        }

        [Test]
        public void Handler_ThrowingException_DoesNotAffectOtherHandlers()
        {
            int handler1Calls = 0;
            int handler2Calls = 0;

            EventBus.Subscribe<TestEvent>(e => handler1Calls++);
            EventBus.Subscribe<TestEvent>(e => throw new Exception("Test exception"));
            EventBus.Subscribe<TestEvent>(e => handler2Calls++);

            // Expect the logged exception from Debug.LogException
            LogAssert.Expect(LogType.Exception, "Exception: Test exception");

            // Should not throw, and other handlers should still be called
            Assert.DoesNotThrow(() => EventBus.Publish(new TestEvent()));
            Assert.AreEqual(1, handler1Calls);
            Assert.AreEqual(1, handler2Calls);
        }

        [Test]
        public void Publish_PassesCorrectEventData()
        {
            TestEvent received = default;

            EventBus.Subscribe<TestEvent>(e => received = e);
            EventBus.Publish(new TestEvent { Message = "Test", Value = 123 });

            Assert.AreEqual("Test", received.Message);
            Assert.AreEqual(123, received.Value);
        }

        [Test]
        public void GameEvents_CanBeUsed()
        {
            // Test with actual game event types
            FishCaughtEvent? caughtEvent = null;
            MoodUpdatedEvent? moodEvent = null;

            EventBus.Subscribe<FishCaughtEvent>(e => caughtEvent = e);
            EventBus.Subscribe<MoodUpdatedEvent>(e => moodEvent = e);

            EventBus.Publish(new FishCaughtEvent
            {
                FishId = "bass_01",
                ZoneId = "lake_01",
                Size = 2.5f,
                IsRare = true
            });

            EventBus.Publish(new MoodUpdatedEvent
            {
                Stillness = 0.8f,
                Curiosity = 0.3f,
                Loss = 0.1f,
                Disruption = 0.2f
            });

            Assert.IsNotNull(caughtEvent);
            Assert.AreEqual("bass_01", caughtEvent.Value.FishId);
            Assert.IsTrue(caughtEvent.Value.IsRare);

            Assert.IsNotNull(moodEvent);
            Assert.AreEqual(0.8f, moodEvent.Value.Stillness, 0.001f);
        }
    }
}
