using System;
using System.ComponentModel;
using System.Text;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web;
using Inedo.Documentation;
using Inedo.Serialization;
using Inedo.Web;

namespace Inedo.BuildMasterExtensions.Java
{
    [DisplayName("Build Ant Project")]
    [Description("Builds an Ant project using a build file.")]
    [Tag(Tags.Java)]
    [Tag(Tags.Builds)]
    [CustomEditor(typeof(AntActionEditor))]
    [ConvertibleToOperation(typeof(AntActionImporter))]
    public sealed class AntAction : AgentBasedActionBase
    {
        private string buildPath;

        /// <summary>
        /// Gets or sets the project's target for the build script.
        /// </summary>
        [Persistent]
        public string ProjectBuildTarget { get; set; }
        /// <summary>
        /// Gets or sets the project's path for the build script.
        /// </summary>
        [Persistent]
        public string BuildPath 
        { 
            get { return Util.CoalesceStr(this.buildPath, "build.xml"); }
            set { this.buildPath = value; }
        }
        /// <summary>
        /// Gets or sets the project's properties for the build script.
        /// </summary>
        /// <remarks>
        /// This should be an array of strings of the form property=value.
        /// </remarks>
        [Persistent]
        public string[] BuildProperties { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            if (this.BuildProperties != null && this.BuildProperties.Length > 0)
            {
                return string.Format(
                    "Build {0} Target:{1} Properties:{2}",
                    this.BuildPath,
                    this.ProjectBuildTarget,
                    string.Join("; ", this.BuildProperties));
            }
            else
            {
                return string.Format(
                    "Build {0} Target:{1}",
                    this.BuildPath,
                    this.ProjectBuildTarget);
            }
        }

        /// <summary>
        /// This method is called to execute the Action.
        /// </summary>
        protected override void Execute()
        {
            var cfg = (JavaExtensionConfigurer)GetExtensionConfigurer();
            if (string.IsNullOrEmpty(cfg.AntPath))
                throw new InvalidOperationException("Ant path is not specified in the Java extension configuration.");

            var buffer = new StringBuilder();
            buffer.AppendFormat("-buildfile \"{0}\" ", this.BuildPath);

            if (this.BuildProperties != null && this.BuildProperties.Length > 0)
            {
                foreach (var property in this.BuildProperties)
                    buffer.AppendFormat("\"-D{0}\" ", property);
            }

            buffer.Append(this.ProjectBuildTarget);

            this.ExecuteCommandLine(cfg.AntPath, buffer.ToString(), this.Context.SourceDirectory);
        }
    }
}
