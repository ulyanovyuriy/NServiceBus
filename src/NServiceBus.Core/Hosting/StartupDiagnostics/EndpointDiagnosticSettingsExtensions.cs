﻿namespace NServiceBus
{
    using Features;
    using Settings;

    /// <summary>
    /// Provides an API to add startup diagnostics.
    /// </summary>
    public static class EndpointDiagnosticSettingsExtensions
    {
        /// <summary>
        /// Adds a section to the startup diagnostics.
        /// </summary>
        public static void AddStartupDiagnosticsSection(this ReadOnlySettings settings, string sectionName, object section)
        {
            Guard.AgainstNull(nameof(settings), settings);
            Guard.AgainstNullAndEmpty(nameof(sectionName), sectionName);
            Guard.AgainstNull(nameof(section), section);

            settings.Get<StartupDiagnosticEntries>().Add(sectionName, section);
        }

        /// <summary>
        /// Adds a section to the startup diagnostics.
        /// </summary>
        public static void AddStartupDiagnosticsSection(this FeatureConfigurationContext context, string sectionName, object section)
        {
            Guard.AgainstNull(nameof(context), context);
            Guard.AgainstNullAndEmpty(nameof(sectionName), sectionName);
            Guard.AgainstNull(nameof(section), section);

            context.Settings.Get<StartupDiagnosticEntries>().Add(sectionName, section);
        }
    }
}