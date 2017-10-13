﻿namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Features;
    using Logging;
    using MessageInterfaces;
    using MessageInterfaces.MessageMapper.Reflection;
    using Pipeline;
    using Serialization;
    using Settings;
    using Unicast.Messages;

    class SerializationFeature : Feature
    {
        public SerializationFeature()
        {
            EnableByDefault();
        }

        protected internal sealed override void Setup(FeatureConfigurationContext context)
        {
            var mapper = new MessageMapper();
            var settings = context.Settings;
            var messageMetadataRegistry = settings.Get<MessageMetadataRegistry>();
            mapper.Initialize(messageMetadataRegistry.GetAllMessages().Select(m => m.MessageType));

            var defaultSerializerAndDefinition = settings.GetMainSerializer();

            var defaultSerializer = CreateMessageSerializer(defaultSerializerAndDefinition, mapper, settings);

            var additionalDeserializerDefinitions = context.Settings.GetAdditionalSerializers();
            var additionalDeserializers = new List<IMessageSerializer>();

            var additionalDeserializerDiagnostics = new List<Tuple<Type, IMessageSerializer>>();
            foreach (var definitionAndSettings in additionalDeserializerDefinitions)
            {
                var deserializer = CreateMessageSerializer(definitionAndSettings, mapper, settings);
                additionalDeserializers.Add(deserializer);

                additionalDeserializerDiagnostics.Add(new Tuple<Type, IMessageSerializer>(definitionAndSettings.Item1.GetType(), deserializer));
            }

            var resolver = new MessageDeserializerResolver(defaultSerializer, additionalDeserializers);

            var logicalMessageFactory = new LogicalMessageFactory(messageMetadataRegistry, mapper);
            context.Pipeline.Register(new DeserializeLogicalMessagesConnector(resolver, logicalMessageFactory, messageMetadataRegistry, mapper), "Deserializes the physical message body into logical messages");
            context.Pipeline.Register(new SerializeMessageConnector(defaultSerializer, messageMetadataRegistry), "Converts a logical message into a physical message");

            context.Container.ConfigureComponent(_ => mapper, DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent(_ => messageMetadataRegistry, DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent(_ => logicalMessageFactory, DependencyLifecycle.SingleInstance);

            LogFoundMessages(messageMetadataRegistry.GetAllMessages().ToList());

            context.AddStartupDiagnosticsSection("Serialization", new
            {
                DefaultSerializer = new
                {
                    Type = defaultSerializerAndDefinition.Item1.GetType().FullName,
                    Version = FileVersionRetriever.GetFileVersion(defaultSerializerAndDefinition.Item1.GetType()),
                    defaultSerializer.ContentType
                },
                AdditionalDeserializers = additionalDeserializerDiagnostics.Select(d => new
                {
                    Type = d.Item1.FullName,
                    Version = FileVersionRetriever.GetFileVersion(d.Item1),
                    d.Item2.ContentType
                })
            });
        }

        static IMessageSerializer CreateMessageSerializer(Tuple<SerializationDefinition, SettingsHolder> definitionAndSettings, IMessageMapper mapper, ReadOnlySettings mainSettings)
        {
            var definition = definitionAndSettings.Item1;
            var deserializerSettings = definitionAndSettings.Item2;
            deserializerSettings.Merge(mainSettings);
            deserializerSettings.PreventChanges();

            var serializerFactory = definition.Configure(deserializerSettings);
            var serializer = serializerFactory(mapper);
            return serializer;
        }

        static void LogFoundMessages(IReadOnlyCollection<MessageMetadata> messageDefinitions)
        {
            if (!Logger.IsInfoEnabled)
            {
                return;
            }
            Logger.DebugFormat("Number of messages found: {0}", messageDefinitions.Count);
            if (!Logger.IsDebugEnabled)
            {
                return;
            }
            Logger.Debug($"Message definitions: {Environment.NewLine}{string.Join(Environment.NewLine, messageDefinitions.Select(md => md.MessageType.FullName))}");
        }

        static ILog Logger = LogManager.GetLogger<SerializationFeature>();
    }
}