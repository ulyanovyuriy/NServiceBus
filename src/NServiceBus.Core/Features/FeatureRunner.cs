namespace NServiceBus.Features
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using ObjectBuilder;

    class FeatureRunner
    {
        public FeatureRunner(IBuilder builder, IList<FeatureActivator.FeatureInfo> features)
        {
            this.builder = builder;
            this.features = features;
        }

        public async Task<RunningFeatures> Start(IMessageSession messageSession)
        {
            foreach (var feature in features)
            {
                foreach (var taskController in feature.TaskControllers)
                {
                    await taskController.Start(builder, messageSession).ConfigureAwait(false);
                }
            }

            return new RunningFeatures(features, messageSession);
        }

        IBuilder builder;
        IList<FeatureActivator.FeatureInfo> features;
    }
}