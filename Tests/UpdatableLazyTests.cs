using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using truepele;
using truepele.Common;

namespace Tests
{
    [TestFixture]
    public class UpdatableLazyTests
    {
        private const string TestValue = "Val";
        private const int DefaultMaxRetries = 3;
        private const int ShortDelay = 5;
        private const int HandlerDelay = 100;
        private const int WaitHandleDelay = 500;

        [Test]
        public async Task Should_ReturnValue()
        {
            var lazy = CreateLazyInstance();
            Assert.AreEqual(TestValue, lazy.Value);
        }

        
        [Test]
        public async Task Should_ReturnValueAsync()
        {
            var updater = CreateLazyInstance();
            Assert.AreEqual(TestValue, await updater.GetValueAsync());
        }

        [Test]
        public void Should_UpdateValue()
        {
            var val = TestValue;
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
            var lazy = new UpdatableLazy<string>(() =>
            {
                Task.Delay(ShortDelay).Wait();
                return val;
            });
            Assert.AreEqual(val, lazy.Value);

            val = "Changed";
            await lazy.UpdateOrWaitAsync();
            Assert.AreEqual(val, lazy.Value);
        }

        [Test]
        public void Should_WaitForValue()
        {
            var lazy = new UpdatableLazy<string>(() =>
            {
                Task.Delay(HandlerDelay).Wait();
                return TestValue;
            });

            Assert.AreEqual(TestValue, lazy.Value);
        }


        [Test]
        public async Task Should_WaitForValueAsync()
        {
            var lazy = new UpdatableLazy<string>(() =>
            {
                Task.Delay(ShortDelay).Wait();
                return TestValue;
            });

            Assert.AreEqual(TestValue, await lazy.GetValueAsync());
        }

        [Test]
        public void Should_Retry()
        {
            var counter = 0;
            var lazy = new UpdatableLazy<string>(() =>
            {
                if (counter++ < DefaultMaxRetries - 1)
                {
                    throw new Exception();
                }

                return TestValue;
            }, DefaultMaxRetries);

            Assert.AreEqual(TestValue, lazy.Value);
            Assert.AreEqual(DefaultMaxRetries, counter);
        }

        [Test]
        public async Task Should_RetryAsync()
        {
            var counter = 0;
            var lazy = new UpdatableLazy<string>(() =>
            {
                if (counter++ < DefaultMaxRetries - 1)
                {
                    throw new Exception();
                }

                return TestValue;
            }, DefaultMaxRetries);

            Assert.AreEqual(TestValue, await lazy.GetValueAsync());
            Assert.AreEqual(DefaultMaxRetries, counter);
        }


        [Test]
        public void Should_FireValueCreatedEvent()
        {
            var re = new AutoResetEvent(false);
            var lazy = CreateLazyInstance();
            lazy.ValueCreated += (o, v) => re.Set();

            var tmp = lazy.Value;
            var eventFired = re.WaitOne(WaitHandleDelay);
            Assert.IsTrue(eventFired);
        }

        [Test]
        public void Should_PassValueToValueCreatedEventHandler()
        {
            var re = new AutoResetEvent(false);
            string passedToEventValue = null;
            var lazy = CreateLazyInstance();
            lazy.ValueCreated += (o, v) =>
            {
                passedToEventValue = v;
                re.Set();
            };

            var tmp = lazy.Value;
            re.WaitOne(WaitHandleDelay);
            Assert.AreEqual(TestValue, passedToEventValue);
        }

        [Test]
        public void ShouldNot_RetryIfMaxRetriesIsZero()
        {
            var counter = 0;
            var lazy = new UpdatableLazy<string>(() =>
            {
                counter++;
                throw new Exception();
                return string.Empty;
            }, 0);

            lazy.UpdateOrWait();
            Assert.AreEqual(1, counter);
        }

        [Test]
        public void Should_FireFactoryErrorEvent()
        {
            var eventCount = 0;
            var resetEvents = InitWaitHandles(DefaultMaxRetries+1);
            var lazy = CreateFaultyLazyInstance();
            lazy.FactoryError += (o, e) => resetEvents[eventCount++].Set();

            var tmp = lazy.Value;
            WaitHandle.WaitAll(resetEvents, WaitHandleDelay);
            Assert.AreEqual(DefaultMaxRetries + 1, eventCount);
        }


        [Test]
        public void Should_CallAllValueCreatedHandlersEvenIfSomeFail()
        {
            CheckCallsAllHAndlersEvenIfSomeFail<string, string>(CreateLazyInstance(), (l, handler) => l.ValueCreated += handler);
        }


        [Test]
        public void Should_CallAllFactoryErrorHandlersEvenIfSomeFail()
        {
            CheckCallsAllHAndlersEvenIfSomeFail<string, Exception>(CreateFaultyLazyInstance(), (l, handler) => l.FactoryError += handler);
        }


        [Test]
        public void Should_CallAllMaxRetriesExceededHandlersEvenIfSomeFail()
        {
            CheckCallsAllHAndlersEvenIfSomeFail<string, IEnumerable<Exception>>(CreateFaultyLazyInstance(), (l, handler) => l.MaxRetriesExceeded += handler);
        }

        private static void CheckCallsAllHAndlersEvenIfSomeFail<T, TEventArgs>(UpdatableLazy<T> lazyInstance, Action<UpdatableLazy<T>, EventHandler<TEventArgs>> addEventAction)
        {
            var resetEvents = InitWaitHandles(3);
            addEventAction(lazyInstance, (o, e) => resetEvents[0].Set());
            addEventAction(lazyInstance, (o, e) =>
            {
                try
                {
                    throw new Exception();
                }
                finally
                {
                    resetEvents[1].Set();
                }
            });

            addEventAction(lazyInstance, (o, e) =>
            {
                Task.Delay(HandlerDelay);
                resetEvents[2].Set();
            });

            Assert.DoesNotThrow(() => { var tmp = lazyInstance.Value; });
            WaitHandle.WaitAll(resetEvents, WaitHandleDelay);
            Assert.IsTrue(resetEvents.All(resetEvent => resetEvent.WaitOne(0)));
        }


        [Test]
        public void Should_PassExceptionToFactoryErrorEventHandler()
        {
            var exceptions = new List<Exception>();
            var resetEvents = InitWaitHandles(DefaultMaxRetries + 1);
            var lazy = CreateFaultyLazyInstance();
            lazy.FactoryError += (o,e) =>
            {
                resetEvents[exceptions.Count].Set();
                exceptions.Add(e);
            };

            var tmp = lazy.Value;
            WaitHandle.WaitAll(resetEvents, WaitHandleDelay);
            Assert.AreEqual(DefaultMaxRetries + 1, exceptions.Count);
        }

        [Test]
        public void Should_FireMaxRetriesExceededEvent()
        {
            var re = new AutoResetEvent(false);
            var lazy = CreateFaultyLazyInstance();
            lazy.MaxRetriesExceeded += (o, e) => re.Set();

            var tmp = lazy.Value;
            var eventFired = re.WaitOne(WaitHandleDelay);
            Assert.IsTrue(eventFired);
        }

        [Test]
        public void Should_PassExceptionsToMaxRetriesExceededEventHandler()
        {
            var re = new AutoResetEvent(false);
            var exceptions = new List<Exception>();
            var lazy = CreateFaultyLazyInstance();
            lazy.MaxRetriesExceeded += (o, e) =>
            {
                exceptions.AddRange(e);
                re.Set();
            };

            var tmp = lazy.Value;
            re.WaitOne(WaitHandleDelay);
            Assert.AreEqual(DefaultMaxRetries + 1, exceptions.Count);
        }

        [Test]
        public void ShouldNot_BlockWorkingThreadByValueCreatedHandler()
        {
            var lazy = CreateLazyInstance();
            lazy.ValueCreated += (o, v) => Thread.Sleep(HandlerDelay);

            var elapsed = MeasureExecutionTime(() => { var tmp = lazy.Value; });
            Assert.Less(elapsed.TotalMilliseconds, HandlerDelay);
        }

        [Test]
        public async Task ShouldNot_BlockWorkingThreadByValueCreatedHandlerAsync()
        {
            var lazy = CreateLazyInstance();
            lazy.ValueCreated += (o, v) => Thread.Sleep(HandlerDelay);

            var elapsed = await MeasureExecutionTimeAsync(() => lazy.GetValueAsync());
            Assert.Less(elapsed.TotalMilliseconds, HandlerDelay);
        }

       
        [Test]
        public void ShouldNot_BlockWorkingThreadByFactoryErrorHandler()
        {
            var lazy = CreateFaultyLazyInstance();
            lazy.FactoryError += (o, e) => Thread.Sleep(HandlerDelay);
            var elapsed = MeasureExecutionTime(() => { var tmp = lazy.Value; });
            Assert.Less(elapsed.TotalMilliseconds, HandlerDelay);
            Assert.Less(elapsed.TotalMilliseconds, HandlerDelay);
        }

        [Test]
        public async Task ShouldNot_BlockWorkingThreadByFactoryErrorHandlerAsync()
        {
            var lazy = CreateFaultyLazyInstance();
            lazy.FactoryError += (o, e) => Thread.Sleep(HandlerDelay);
            var elapsed = await MeasureExecutionTimeAsync(async () => await lazy.GetValueAsync());
            Assert.Less(elapsed.TotalMilliseconds, HandlerDelay);
            Assert.Less(elapsed.TotalMilliseconds, HandlerDelay);
        }


        [Test]
        public void ShouldNot_BlockWorkingThreadMaxRetriesExceededHandler()
        {
            var lazy = CreateFaultyLazyInstance();
            lazy.MaxRetriesExceeded += (o, e) => Thread.Sleep(HandlerDelay);
            var elapsed = MeasureExecutionTime(() => { var tmp = lazy.Value; });
            Assert.Less(elapsed.TotalMilliseconds, HandlerDelay);
        }

        [Test]
        public async Task ShouldNot_BlockWorkingThreadByMaxRetriesExceededHandlerAsync()
        {
            var lazy = CreateFaultyLazyInstance();
            lazy.MaxRetriesExceeded += (o, e) => Thread.Sleep(HandlerDelay);
            var elapsed = await MeasureExecutionTimeAsync(async () => await lazy.GetValueAsync());
            Assert.Less(elapsed.TotalMilliseconds, HandlerDelay);
        }


        private static TimeSpan MeasureExecutionTime(Action func)
        {
            var sw = new Stopwatch();
            sw.Start();
            func();
            sw.Stop();
            return sw.Elapsed;
        }


        private static async Task<TimeSpan> MeasureExecutionTimeAsync<T>(Func<Task<T>> func)
        {
            var sw = new Stopwatch();
            sw.Start();
            await func();
            sw.Stop();
            return sw.Elapsed;
        }

        private static ManualResetEvent[] InitWaitHandles(int count)
        {
            var resetEvents = new ManualResetEvent[count];
            for (var i = 0; i < resetEvents.Length; i++)
            {
                resetEvents[i] = new ManualResetEvent(false);
            }
            return resetEvents;
        }


        private static UpdatableLazy<string> CreateLazyInstance(string val = TestValue)
        {
            return new UpdatableLazy<string>(() => val);
        }

        private static UpdatableLazy<string> CreateFaultyLazyInstance()
        {
            return new UpdatableLazy<string>((Func<string>)(() => throw new Exception()), DefaultMaxRetries);
        }
    }
}
