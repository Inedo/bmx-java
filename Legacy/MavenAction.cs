using System.ComponentModel;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web;
using Inedo.Documentation;
using Inedo.Serialization;
using Inedo.Web;

namespace Inedo.BuildMasterExtensions.Java
{
    [DisplayName("Execute Maven")]
    [Description("Runs the Maven executable.")]
    [Tag(Tags.Java)]
    [Tag(Tags.Builds)]
    [CustomEditor(typeof(MavenActionEditor))]
    [ConvertibleToOperation(typeof(MavenActionImporter))]
    public sealed class MavenAction : AgentBasedActionBase
    {
        /// <summary>
        /// Gets or sets the path to Maven.
        /// </summary>
        [Persistent]
        public string MavenPath { get; set; }
        /// <summary>
        /// Gets or sets the Maven goals and/or phases.
        /// </summary>
        [Persistent]
        public string[] GoalsAndPhases { get; set; }
        /// <summary>
        /// Gets or sets any additional arguments to be passed to javac.
        /// </summary>
        [Persistent]
        public string[] AdditionalArguments { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format("Execute Maven action \"{0}\" in \"{1}\"",
                string.Join(" ", this.GoalsAndPhases ?? new string[] { "" }),
                Util.CoalesceStr(this.OverriddenSourceDirectory, "default directory")
            );
        }

        /// <summary>
        /// This method is called to execute the Action.
        /// </summary>
        protected override void Execute()
        {
            this.LogInformation("Executing Maven...");

            int retCode = this.ExecuteCommandLine(
                this.MavenPath,
                string.Format(
                    " {0} {1}",
                    string.Join(" ", this.AdditionalArguments ?? new string[0]),
                    string.Join(" ", this.GoalsAndPhases ?? new string[0])
                ),
                this.Context.SourceDirectory
            );

            if (retCode != 0)
                this.LogError("Maven returned error code {0} (expected 0)", retCode);

            this.LogInformation("Maven execution complete.");
        }
        /// <summary>
        /// Invoked when data is written to the process's Standard Out output.
        /// </summary>
        /// <param name="data">Data written to Standard Out.</param>
        protected override void LogProcessOutputData(string data)
        {
            if (data.Contains("[ERROR]"))
                this.LogError(data);
            else if (data.Contains("[INFO]"))
                this.LogInformation(data.Substring("[INFO]".Length));
            else if (data.Contains("Downloading"))
                this.LogInformation(data);
        }
    }
}
