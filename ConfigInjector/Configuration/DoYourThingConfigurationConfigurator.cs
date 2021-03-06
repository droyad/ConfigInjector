﻿using System;
using System.Collections.Generic;
using System.Reflection;
using ConfigInjector.Exceptions;
using ConfigInjector.SettingsConventions;
using ConfigInjector.ValueParsers;

namespace ConfigInjector.Configuration
{
    public class DoYourThingConfigurationConfigurator
    {
        private readonly Assembly[] _assemblies;
        private readonly Action<IConfigurationSetting> _registerAsSingleton;

        private bool _allowConfigurationEntriesThatDoNotHaveSettingsClasses;
        private readonly List<IValueParser> _customValueParsers = new List<IValueParser>();
        private ISettingsReader _settingsReader;

        private readonly List<ISettingKeyConvention> _settingKeyConventions = new List<ISettingKeyConvention>();
        private readonly List<string> _excludedKeys = new List<string>();

        internal DoYourThingConfigurationConfigurator(Assembly[] assemblies, Action<IConfigurationSetting> registerAsSingleton)
        {
            _assemblies = assemblies;
            _registerAsSingleton = registerAsSingleton;

            _settingKeyConventions.AddRange(SettingKeyConventions.BuiltInConventions);
        }

        /// <summary>
        /// If set to false (default), ConfigInjector will blow up when there are settings in the [web|app].config file that
        /// do not have corresponding setting types in your application.
        /// </summary>
        public DoYourThingConfigurationConfigurator AllowConfigurationEntriesThatDoNotHaveSettingsClasses(bool allow)
        {
            _allowConfigurationEntriesThatDoNotHaveSettingsClasses = allow;
            return this;
        }

        public DoYourThingConfigurationConfigurator WithCustomValueParsers(params IValueParser[] valueParsers)
        {
            _customValueParsers.AddRange(valueParsers);
            return this;
        }

        public DoYourThingConfigurationConfigurator WithSettingKeyConventions(params ISettingKeyConvention[] settingKeyConventions)
        {
            _settingKeyConventions.AddRange(settingKeyConventions);
            return this;
        }

        public DoYourThingConfigurationConfigurator ExcludeSettingKeys(params string[] settingKeys)
        {
            _excludedKeys.AddRange(settingKeys);
            return this;
        }

        /// <summary>
        /// This allows you to substitute your own application settings reader. A good use case for this is in having a unit/convention
        /// test suite that opens your application's app.config file (rather than the test project's one) and asserts that all configuration
        /// settings are present and accounted for.
        /// </summary>
        public DoYourThingConfigurationConfigurator WithAppSettingsReader(ISettingsReader settingsReader)
        {
            _settingsReader = settingsReader;
            return this;
        }

        public void DoYourThing()
        {
            if (_assemblies == null) throw new ConfigurationException("You must specify the assemblies to scan for configuration settings.");
            if (_registerAsSingleton == null) throw new ConfigurationException("You must provide a registration action.");

            var settingsReader = _settingsReader ?? new AppSettingsReader(_excludedKeys.ToArray());
            var settingValueConverter = new SettingValueConverter(_customValueParsers.ToArray());

            var appConfigConfigurationProvider = new SettingsRegistrationService(_assemblies,
                                                                                 _registerAsSingleton,
                                                                                 _allowConfigurationEntriesThatDoNotHaveSettingsClasses,
                                                                                 settingValueConverter,
                                                                                 settingsReader,
                                                                                 _settingKeyConventions.ToArray());
            appConfigConfigurationProvider.RegisterConfigurationSettings();
        }
    }
}