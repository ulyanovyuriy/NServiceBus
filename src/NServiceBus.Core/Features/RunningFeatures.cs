namespace NServiceBus.Features
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    class RunningFeatures
    {
        public RunningFeatures(IList<FeatureActivator.FeatureInfo> features, IMessageSession messageSession)
        {
            this.features = features;
            this.messageSession = messageSession;
        }

        public Task Stop()
        {
            var featureStopTasks = features.SelectMany(f => f.TaskControllers)
                .Select(task => task.Stop(messageSession));

            return Task.WhenAll(featureStopTasks);
        }

        IList<FeatureActivator.FeatureInfo> features;
        IMessageSession messageSession;
    }
}