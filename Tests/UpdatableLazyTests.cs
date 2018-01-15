using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using truepele;

namespace Tests
{
    [TestFixture]
    public class UpdatableLazyTests
    {
        [Test]
        public async Task Should_ReturnValue()
        {
            const string val = "Val";

            var lazy = new UpdatableLazy<string>(() => val);
            Assert.AreEqual(val, lazy.Value);
        }


        [Test]
        public async Task Should_ReturnValueAsync()
        {
            const string val = "Val";

            var updater = new UpdatableLazy<string>(() => val);
            Assert.AreEqual(val, await updater.GetValueAsync());
        }

        [Test]
        public void Should_UpdateValue()
        {
            var val = "Val";

            var lazy = new UpdatableLazy<string>(() => val);
            Assert.AreEqual(val, lazy.Value);

            val = "Changed";
            lazy.UpdateOrWait();
            Assert.AreEqual(val, lazy.Value);
        }

        [Test]
        public async Task Should_UpdateValueAsync()
        {
            var val = "Val";

            var lazy = new UpdatableLazy<string>(() => val);
            Assert.AreEqual(val, lazy.Value);

            val = "Changed";
            await lazy.UpdateOrWaitAsync();
            Assert.AreEqual(val, lazy.Value);
        }

        [Test]
        public void Should_WaitForValue()
        {
            const string val = "Val";

            var lazy = new UpdatableLazy<string>(() =>
            {
                Task.Delay(100).Wait();
                return val;
            });

            Assert.AreEqual(val, lazy.Value);
        }


        [Test]
        public async Task Should_WaitForValueAsync()
        {
            const string val = "Val";

            var lazy = new UpdatableLazy<string>(() =>
            {
                Task.Delay(100).Wait();
                return val;
            });

            Assert.AreEqual(val, await lazy.GetValueAsync());
        }

        [Test]
        public void Should_Retry()
        {
            const string val = "Val";
            var counter = 0;

            var lazy = new UpdatableLazy<string>(() =>
            {
                if (counter++ < 2)
                    throw new Exception();
                return val;
            }, 3);

            Assert.AreEqual(val, lazy.Value);
            Assert.AreEqual(3, counter);
        }

        [Test]
        public async Task Should_RetryAsync()
        {
            const string val = "Val";
            var counter = 0;

            var lazy = new UpdatableLazy<string>(() =>
            {
                if (counter++ < 2)
                    throw new Exception();
                return val;
            }, 3);

            Assert.AreEqual(val, await lazy.GetValueAsync());
            Assert.AreEqual(3, counter);
        }


        [Test]
        public void Should_FireLoadedEvent()
        {
            var eventCount = 0;

            var lazy = new UpdatableLazy<string>(() => string.Empty);
            lazy.ValueCreated += (o, v) => eventCount++;
            var tmp = lazy.Value;

            Assert.GreaterOrEqual(eventCount, 1);
        }

        [Test]
        public void Should_PassValueToLoadedEventHandler()
        {
            const string val = "Val";
            string passedToEventValue = null;

            var lazy = new UpdatableLazy<string>(() => val);
            lazy.ValueCreated += (o, v) => passedToEventValue = v;
            var tmp = lazy.Value;

            Assert.AreEqual(val, passedToEventValue);
        }

        [Test]
        public void ShouldNot_RetryIfMaxRetriesIsZero()
        {
            var val = "Val";
            var counter = 0;

            var lazy = new UpdatableLazy<string>(() =>
            {
                if (counter++ == 0)
                    throw new Exception();
                return val;
            }, 0);

            lazy.UpdateOrWait();
            Assert.AreEqual(1, counter);
        }

        [Test]
        public void Should_FireFactoryErrorEvent()
        {
            var eventCount = 0;

            var lazy = new UpdatableLazy<string>(() => throw new Exception(), 3);
            lazy.FactoryError += (o, e) => eventCount++;
            var tmp = lazy.Value;

            Assert.AreEqual(4, eventCount);
        }

        [Test]
        public void Should_PassExceptionToFactoryErrorEventHandler()
        {
            var exceptions = new List<Exception>();

            var lazy = new UpdatableLazy<string>(() => throw new Exception(), 3);
            lazy.FactoryError += (o,e) => exceptions.Add(e);
            var tmp = lazy.Value;

            Assert.AreEqual(exceptions.Count, 4);
        }

        [Test]
        public void Should_FireMaxRetriesExceededEvent()
        {
            var eventCount = 0;

            var lazy = new UpdatableLazy<string>(() => throw new Exception(), 3);
            lazy.MaxRetriesExceeded += (o, e) => eventCount++;
            var tmp = lazy.Value;

            Assert.AreEqual(1, eventCount);
        }

        [Test]
        public void Should_PassExceptionsToMaxRetriesExceededEventHandler()
        {
            var exceptions = new List<Exception>();

            var lazy = new UpdatableLazy<string>(() => throw new Exception(), 3);
            lazy.MaxRetriesExceeded += (o, e) => exceptions.AddRange(e);
            var tmp = lazy.Value;

            Assert.AreEqual(exceptions.Count, 4);
        }
    }
}
