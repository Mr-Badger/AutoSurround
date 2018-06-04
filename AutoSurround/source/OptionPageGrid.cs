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
        
        private string CollectionName { get; } = typeof(OptionPage).FullName;

        private static OptionPage instance = null;

        internal static OptionPage Instance {
            get {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (instance == null) {
                    instance = new OptionPage();
                }

                instance.LoadSettingsFromStorage();
                return instance;
            }
        }

        public static event EventHandler OnUpdate;

        [ExtensionSetting]
        [Category("Auto Surround")]
        [DisplayName("Enable Square Bracket")]
        [Description("Enable Square Bracket => [sampletext] ")]
        public bool EnableSquareBrackets { get; set; } = true;

        [ExtensionSetting]
        [Category("Auto Surround")]
        [DisplayName("Enable Angle Bracket")]
        [Description("Enable Angle Bracket => <sampletext> ")]
        public bool EnableAngleBracket { get; set; } = true;

        [ExtensionSetting]
        [Category("Auto Surround")]
        [DisplayName("Enable Parentheses1")]
        [Description("Enable Parentheses1 => {sampletext} ")]
        public bool EnableParentheses { get; set; } = true;

        [ExtensionSetting]
        [Category("Auto Surround")]
        [DisplayName("Enable Braces")]
        [Description("Enable Braces => (sampletext) ")]
        public bool EnableBraces { get; set; } = true;

        [ExtensionSetting]
        [Category("Auto Surround")]
        [DisplayName("Enable Doublequotes")]
        [Description("Enable Doublequotes => \"sampletext\" ")]
        public bool EnableDoublequotes { get; set; } = true;

        [ExtensionSetting]
        [Category("Auto Surround")]
        [DisplayName("Enable Singequotes")]
        [Description("Enable Singequotes => 'sampletext' ")]
        public bool EnableSingequotes { get; set; } = true;

        [ExtensionSetting]
        [Category("Auto Surround")]
        [DisplayName("Enable Comment Block")]
        [Description("Enable Comment Block => /*sampletext*/ ")]
        public bool EnableCommentBlock { get; set; } = true;

        public override void SaveSettingsToStorage()
        {
            SettingsManager settingsManager = new ShellSettingsManager(ServiceProvider.GlobalProvider);
            WritableSettingsStore settingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

            if (!settingsStore.CollectionExists(CollectionName))
            {
                settingsStore.CreateCollection(CollectionName);
            }

            foreach (PropertyInfo property in GetOptionProperties())
            {
                string output = SerializeValue(property.GetValue(this));
                settingsStore.SetString(CollectionName, property.Name, output);
            }

            LoadSettingsFromStorage();

            OnUpdate?.Invoke(this, null);
        }

        public override void LoadSettingsFromStorage()
        {
            SettingsManager settingsManager = new ShellSettingsManager(ServiceProvider.GlobalProvider);
            SettingsStore settingsStore = settingsManager.GetReadOnlySettingsStore(SettingsScope.UserSettings);

            if (!settingsStore.CollectionExists(CollectionName))
            {
                return;
            }

            foreach (PropertyInfo property in GetOptionProperties())
            {
                try
                {
                    string serializedProp = settingsStore.GetString(CollectionName, property.Name);
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
                .Where(
                    p => p.PropertyType.IsSerializable
                      && p.PropertyType.IsPublic
                      && Attribute.IsDefined(p, typeof(ExtensionSettingAttribute))
                );
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
            {
                return new BinaryFormatter().Deserialize(stream);
            }
        }
    }
}
