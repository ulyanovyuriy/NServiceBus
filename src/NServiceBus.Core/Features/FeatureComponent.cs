namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using ObjectBuilder;
    using Settings;

    class FeatureComponent
    {
        FeatureComponent(IBuilder builder, FeatureActivator featureActivator)
        {
            this.builder = builder;
            this.featureActivator = featureActivator;
        }

        public static FeatureComponent Initialize(SettingsHolder settings, IBuilder builder, IEnumerable<Type> concreteTypes, FeatureConfigurationContext featureConfigurationContext)
        {
            var featureActivator = new FeatureActivator(settings);

            foreach (var type in concreteTypes.Where(t => IsFeature(t)))
            {
                featureActivator.Add(type.Construct<Feature>());
            }

            var featureStats = featureActivator.SetupFeatures(featureConfigurationContext);
            settings.AddStartupDiagnosticsSection("Features", featureStats);

            return new FeatureComponent(builder, featureActivator);
        }

        static bool IsFeature(Type type)
        {
            return typeof(Feature).IsAssignableFrom(type);
        }

        public Task<RunningFeatures> Start(IMessageSession messageSession)
        {
            var runner = new FeatureRunner(builder, featureActivator.ActiveFeatures);

            return runner.Start(messageSession);
        }

        FeatureActivator featureActivator;
        IBuilder builder;
    }
}