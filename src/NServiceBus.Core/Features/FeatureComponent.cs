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
        public FeatureComponent(SettingsHolder settings,IBuilder builder)
        {
            this.settings = settings;
            this.builder = builder;
             featureActivator = new FeatureActivator(settings);
        }

        public void Initialize(IEnumerable<Type> concreteTypes, FeatureConfigurationContext featureConfigurationContext)
        {
            foreach (var type in concreteTypes.Where(t => IsFeature(t)))
            {
                featureActivator.Add(type.Construct<Feature>());
            }

            var featureStats = featureActivator.SetupFeatures(featureConfigurationContext);
            settings.AddStartupDiagnosticsSection("Features", featureStats);
        }

        public Task Start(IMessageSession session)
        {
            messageSession = session;
            return featureActivator.StartFeatures(builder, session);
        }

        public Task Stop()
        {
            return featureActivator.StartFeatures(builder, messageSession);
        }

        static bool IsFeature(Type type)
        {
            return typeof(Feature).IsAssignableFrom(type);
        }

        SettingsHolder settings;
        IBuilder builder;
        FeatureActivator featureActivator;
        IMessageSession messageSession;
    }
}