using System;
using System.Threading.Tasks;
using NUnit.Framework;
using truepele;

namespace Tests
{
    [TestFixture]
    public class UpdatableLazyTests
    {
        [Test]
        public async Task Should_returnValue()
        {
            const string val = "Val";

            var lazy = new UpdatableLazy<string>(() => val);
            Assert.AreEqual(val, lazy.Value);
        }


        [Test]
        public async Task Should_returnValueAsync()
        {
            const string val = "Val";

            var updater = new UpdatableLazy<string>(() => val);
            Assert.AreEqual(val, await updater.GetValueAsync());
        }

        [Test]
        public void Should_updateValue()
        {
            var val = "Val";

            var lazy = new UpdatableLazy<string>(() => val);
            Assert.AreEqual(val, lazy.Value);

            val = "Changed";
            lazy.UpdateOrWait();
            Assert.AreEqual(val, lazy.Value);
        }

        [Test]
        public async Task Should_updateValueAsync()
        {
            var val = "Val";

            var lazy = new UpdatableLazy<string>(() => val);
            Assert.AreEqual(val, lazy.Value);

            val = "Changed";
            await lazy.UpdateOrWaitAsync();
            Assert.AreEqual(val, lazy.Value);
        }

        [Test]
        public void Should_waitForValue()
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
        public async Task Should_waitForValueAsync()
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
        public void Should_retry()
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
        public async Task Should_retryAsync()
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
            int eventCount = 0;

            var lazy = new UpdatableLazy<string>(() => string.Empty);
            lazy.ValueCreated += v => eventCount++;
            var tmp = lazy.Value;

            Assert.GreaterOrEqual(eventCount, 1);
        }

        [Test]
        public void Should_PassValueToLoadedEventHandler()
        {
            const string val = "Val";
            string passedToEventValue = null;

            var lazy = new UpdatableLazy<string>(() => val);
            lazy.ValueCreated += v => passedToEventValue = v;
            var tmp = lazy.Value;

            Assert.AreEqual(val, passedToEventValue);
        }

        [Test]
        public void Should_notRetryIfMaxRetriesIsZero()
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
    }
}
