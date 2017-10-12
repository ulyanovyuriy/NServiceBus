namespace NServiceBus.Core.Tests.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Features;
    using NUnit.Framework;
    using ObjectBuilder;

    [TestFixture]
    public class FeatureStartupTests
    {
        [Test]
        public async Task Should_start_and_stop_tasks()
        {
            var feature = new FeatureWithStartupTask();

            var featureRunner = CreateRunner(feature);

            var runningFeatures = await featureRunner.Start(null);
            await runningFeatures.Stop();

            Assert.True(feature.TaskStarted);
            Assert.True(feature.TaskStopped);
        }

        [Test]
        public async Task Should_dispose_feature_startup_tasks_when_they_implement_IDisposable()
        {
            var feature = new FeatureWithStartupTaskWhichIsDisposable();

            var featureRunner = CreateRunner(feature);


            var runningFeatures = await featureRunner.Start(null);
            await runningFeatures.Stop();

            Assert.True(feature.TaskDisposed);
        }

        [Test]
        public void Should_not_throw_when_feature_task_fails_on_start_and_abort_starting()
        {
            var feature1 = new FeatureWithStartupTaskThatThrows(throwOnStart: true, throwOnStop: false);
            var feature2 = new FeatureWithStartupTaskThatThrows(throwOnStart: false, throwOnStop: false);

            var featureRunner = CreateRunner(feature1, feature2);

            Assert.ThrowsAsync<InvalidOperationException>(async () => await featureRunner.Start(null));

            Assert.False(feature1.TaskStarted && feature1.TaskStopped);
            Assert.False(feature2.TaskStarted && feature2.TaskStopped);
        }

        [Test]
        public async Task Should_not_throw_when_feature_task_fails_on_stop_and_not_abort_stopping()
        {
            var feature1 = new FeatureWithStartupTaskThatThrows(throwOnStart: false, throwOnStop: false);
            var feature2 = new FeatureWithStartupTaskThatThrows(throwOnStart: false, throwOnStop: true);

            var featureRunner = CreateRunner(feature1, feature2);


            var runningFeatures = await featureRunner.Start(null);

            Assert.DoesNotThrowAsync(async () => await runningFeatures.Stop());
            Assert.True(feature1.TaskStarted && feature1.TaskStopped);
            Assert.True(feature2.TaskStarted && !feature2.TaskStopped);
        }

        [Test]
        public async Task Should_dispose_feature_task_even_when_stop_throws()
        {
            var feature = new FeatureWithStartupTaskThatThrows(throwOnStart: false, throwOnStop: true);
            var featureRunner = CreateRunner(feature);

            var runningFeatures = await featureRunner.Start(null);
            await runningFeatures.Stop();

            Assert.True(feature.TaskDisposed);
        }
        static FeatureRunner CreateRunner(params Feature[] features)
        {
            var featureInfo = features.Select(f =>
            {
                return new FeatureActivator.FeatureInfo(f, null);
            }).ToList();

            var featureRunner = new FeatureRunner(null, featureInfo);
            return featureRunner;
        }

        class FeatureWithStartupTask : TestFeature
        {
            public FeatureWithStartupTask()
            {
                EnableByDefault();
            }

            public bool TaskStarted { get; private set; }
            public bool TaskStopped { get; private set; }

            protected internal override void Setup(FeatureConfigurationContext context)
            {
                context.RegisterStartupTask(new Runner(this));
            }

            public class Runner : FeatureStartupTask
            {
                public Runner(FeatureWithStartupTask parentFeature)
                {
                    this.parentFeature = parentFeature;
                }

                protected override Task OnStart(IMessageSession session)
                {
                    parentFeature.TaskStarted = true;
                    return TaskEx.CompletedTask;
                }

                protected override Task OnStop(IMessageSession session)
                {
                    parentFeature.TaskStopped = true;
                    return TaskEx.CompletedTask;
                }

                FeatureWithStartupTask parentFeature;
            }
        }

        class FeatureWithStartupTaskThatThrows : TestFeature
        {
            public FeatureWithStartupTaskThatThrows(bool throwOnStart = false, bool throwOnStop = false)
            {
                this.throwOnStart = throwOnStart;
                this.throwOnStop = throwOnStop;

                EnableByDefault();
            }

            public bool TaskStarted { get; private set; }
            public bool TaskStopped { get; private set; }
            public bool TaskDisposed { get; private set; }

            protected internal override void Setup(FeatureConfigurationContext context)
            {
                context.RegisterStartupTask(new Runner(this));
            }

            bool throwOnStart;
            bool throwOnStop;

            public class Runner : FeatureStartupTask, IDisposable
            {
                public Runner(FeatureWithStartupTaskThatThrows parentFeature)
                {
                    this.parentFeature = parentFeature;
                }

                public void Dispose()
                {
                    parentFeature.TaskDisposed = true;
                }

                protected override async Task OnStart(IMessageSession session)
                {
                    await Task.Yield();
                    if (parentFeature.throwOnStart)
                    {
                        throw new InvalidOperationException();
                    }
                    parentFeature.TaskStarted = true;
                }

                protected override async Task OnStop(IMessageSession session)
                {
                    await Task.Yield();
                    if (parentFeature.throwOnStop)
                    {
                        throw new InvalidOperationException();
                    }
                    parentFeature.TaskStopped = true;
                }

                FeatureWithStartupTaskThatThrows parentFeature;
            }
        }

        class FeatureWithStartupTaskWhichIsDisposable : TestFeature
        {
            public FeatureWithStartupTaskWhichIsDisposable()
            {
                EnableByDefault();
            }

            public bool TaskDisposed { get; private set; }

            protected internal override void Setup(FeatureConfigurationContext context)
            {
                context.RegisterStartupTask(new Runner(this));
            }

            public class Runner : FeatureStartupTask, IDisposable
            {
                public Runner(FeatureWithStartupTaskWhichIsDisposable parentFeature)
                {
                    this.parentFeature = parentFeature;
                }

                public void Dispose()
                {
                    parentFeature.TaskDisposed = true;
                }

                protected override Task OnStart(IMessageSession session)
                {
                    return TaskEx.CompletedTask;
                }

                protected override Task OnStop(IMessageSession session)
                {
                    return TaskEx.CompletedTask;
                }

                FeatureWithStartupTaskWhichIsDisposable parentFeature;
            }
        }
    }

    public class FakeBuilder : IBuilder
    {
        public FakeBuilder()
        {
        }

        public FakeBuilder(Type type)
        {
            this.type = type;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public object Build(Type typeToBuild)
        {
            if (typeToBuild != type)
            {
                throw new Exception("Not the expected type");
            }
            return Activator.CreateInstance(typeToBuild);
        }

        public IBuilder CreateChildBuilder()
        {
            throw new NotImplementedException();
        }

        public T Build<T>()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<T> BuildAll<T>()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<object> BuildAll(Type typeToBuild)
        {
            throw new NotImplementedException();
        }

        public void Release(object instance)
        {
            throw new NotImplementedException();
        }

        public void BuildAndDispatch(Type typeToBuild, Action<object> action)
        {
            throw new NotImplementedException();
        }

        Type type;
    }
}