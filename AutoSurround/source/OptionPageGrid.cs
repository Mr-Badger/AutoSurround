using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;

namespace AutoSurround
{
    internal class OptionPage : DialogPage
    {
        private readonly string collectionName = typeof(OptionPage).FullName;

        private static OptionPage instance;

        internal static OptionPage Instance {
            get {
                ThreadHelper.ThrowIfNotOnUIThread();

                (instance ?? (instance = new OptionPage())).LoadSettingsFromStorage();
                return instance;
            }
        }

        internal static event EventHandler OnUpdate;

        [ExtensionSetting]
        [Category("Auto Surround")]
        [DisplayName("Enable square bracket")]
        [Description("Enable square bracket : [Sample Text] ")]
        public bool EnableSquareBrackets { get; set; } = true;

        [ExtensionSetting]
        [Category("Auto Surround")]
        [DisplayName("Enable angle bracket")]
        [Description("Enable angle bracket : <Sample Text> ")]
        public bool EnableAngleBracket { get; set; } = true;

        [ExtensionSetting]
        [Category("Auto Surround")]
        [DisplayName("Enable parentheses")]
        [Description("Enable parentheses : {Sample Text} ")]
        public bool EnableParentheses { get; set; } = true;

        [ExtensionSetting]
        [Category("Auto Surround")]
        [DisplayName("Enable braces")]
        [Description("Enable braces : (Sample Text) ")]
        public bool EnableBraces { get; set; } = true;

        [ExtensionSetting]
        [Category("Auto Surround")]
        [DisplayName("Enable double quotes")]
        [Description("Enable double quotes : \"Sample Text\" ")]
        public bool EnableDoublequotes { get; set; } = true;

        [ExtensionSetting]
        [Category("Auto Surround")]
        [DisplayName("Enable singe quotes")]
        [Description("Enable singe quotes : 'Sample text' ")]
        public bool EnableSingequotes { get; set; } = true;

        public override void SaveSettingsToStorage()
        {
            var settingsManager = new ShellSettingsManager(ServiceProvider.GlobalProvider);
            var settingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

            if (!settingsStore.CollectionExists(collectionName))
            {
                settingsStore.CreateCollection(collectionName);
            }

            foreach (var property in GetOptionProperties())
            {
                var output = SerializeValue(property.GetValue(this));
                settingsStore.SetString(collectionName, property.Name, output);
            }

            LoadSettingsFromStorage();

            OnUpdate?.Invoke(this, null);
        }

        public override void LoadSettingsFromStorage()
        {
            var settingsManager = new ShellSettingsManager(ServiceProvider.GlobalProvider);
            var settingsStore = settingsManager.GetReadOnlySettingsStore(SettingsScope.UserSettings);

            if (!settingsStore.CollectionExists(collectionName))
                return;

            foreach (var property in GetOptionProperties())
            {
                try
                {
                    var serializedProp = settingsStore.GetString(collectionName, property.Name);
                    object value = DeserializeValue(serializedProp, property.PropertyType);
                    property.SetValue(this, value);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.Write(ex);
                }
            }
        }

        private IEnumerable<PropertyInfo> GetOptionProperties()
        {
            return GetType()
                  .GetProperties()
                  .Where(p => p.PropertyType.IsSerializable
                           && p.PropertyType.IsPublic
                           && Attribute.IsDefined(p, typeof(ExtensionSettingAttribute)));
        }

        /// <summary>
        /// Serializes an object value to a string using the binary serializer.
        /// </summary>
        protected virtual string SerializeValue(object value)
        {
            using (var stream = new MemoryStream())
            {
                new BinaryFormatter().Serialize(stream, value);
                stream.Flush();
                return Convert.ToBase64String(stream.ToArray());
            }
        }

        /// <summary>
        /// Deserializes a string to an object using the binary serializer.
        /// </summary>
        protected virtual object DeserializeValue(string value, Type type)
        {
            using (var stream = new MemoryStream(Convert.FromBase64String(value)))
                return new BinaryFormatter().Deserialize(stream);
        }
    }
}
