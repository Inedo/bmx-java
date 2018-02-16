using System;
using System.IO;
using Inedo.BuildMaster.Extensibility.Configurers.Extension;
using Inedo.BuildMaster.Web;
using Inedo.Serialization;
using Inedo.Web;

[assembly: ExtensionConfigurer(typeof(Inedo.BuildMasterExtensions.Java.JavaExtensionConfigurer))]

namespace Inedo.BuildMasterExtensions.Java
{
    /// <summary>
    /// Contains extension level configuration settings for Java actions.
    /// </summary>
    [CustomEditor(typeof(JavaExtensionConfigurerEditor))]
    public sealed class JavaExtensionConfigurer : ExtensionConfigurerBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JavaExtensionConfigurer"/> class.
        /// </summary>
        public JavaExtensionConfigurer() 
        {
            this.JdkPath = Environment.GetEnvironmentVariable("JAVA_HOME");
            var antHome = Environment.GetEnvironmentVariable("ANT_HOME");
            if (!string.IsNullOrEmpty(antHome))
            {
                var ant = Path.Combine(antHome, "bin");
                this.AntPath = Path.Combine(ant, "ant");
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                    this.AntPath += ".exe";
            }
        }

        /// <summary>
        /// Gets or sets the JDK path.
        /// </summary>
        [Persistent]
        public string JdkPath { get; set; }

        /// <summary>
        /// Gets or sets the Ant path.
        /// </summary>
        [Persistent]
        public string AntPath { get; set; }
    }
}
