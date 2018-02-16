using System.ComponentModel;
using System.Threading.Tasks;
using Inedo.Agents;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Operations;

namespace Inedo.BuildMasterExtensions.Java.Operations
{
    [DisplayName("Execute Maven")]
    [Description("Runs the Maven executable.")]
    [ScriptAlias("Execute-Maven")]
    [ScriptNamespace("Java", PreferUnqualified = true)]
    [Tag(Tags.Java)]
    [Tag(Tags.Builds)]
    public sealed class MavenOperation : ExecuteOperation
    {
        [ScriptAlias("GoalsAndPhases")]
        [DisplayName("Goals/phases")]
        [Description("Space-separated list of Maven goals and/or phases.")]
        public string GoalsAndPhases { get; set; }
        [ScriptAlias("In")]
        [DisplayName("Source directory")]
        [Description("The working directory in which Maven will be executed.")]
        public string SourceDirectory { get; set; }
        [ScriptAlias("Arguments")]
        [DisplayName("Additional arguments")]
        [Description("Raw command line arguments to pass to Maven.")]
        public string AdditionalArguments { get; set; }
        [ScriptAlias("MavenPath")]
        [DefaultValue("$MavenPath")]
        [DisplayName("Maven path")]
        [Description("Full path to mvn on the server.")]
        public string MavenPath { get; set; }

        public override async Task ExecuteAsync(IOperationExecutionContext context)
        {
            if (string.IsNullOrEmpty(this.MavenPath))
            {
                this.LogError("Could not determine the location of mvn on this server. To resolve this issue, ensure that Maven is available on this server and create a server-scoped variable named $MavenPath set to the location of mvn (or mvn.bat on Windows).");
                return;
            }

            var fileOps = context.Agent.GetService<IFileOperationsExecuter>();
            if (!fileOps.FileExists(this.MavenPath))
            {
                this.LogError("Maven not found at: " + this.MavenPath);
                return;
            }

            var sourceDirectory = context.ResolvePath(this.SourceDirectory);
            this.LogDebug("Source directory: " + sourceDirectory);
            fileOps.CreateDirectory(sourceDirectory);

            await this.ExecuteCommandLineAsync(
                context,
                new RemoteProcessStartInfo
                {
                    FileName = this.MavenPath,
                    Arguments = this.GoalsAndPhases + " " + this.AdditionalArguments,
                    WorkingDirectory = sourceDirectory
                }
            );
        }

        protected override void LogProcessOutput(string text)
        {
            if (text.Contains("[ERROR]"))
                this.LogError(text);
            else
                this.LogInformation(text);
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(
                new RichDescription(
                    "Execute Maven ",
                    new Hilite(config[nameof(this.GoalsAndPhases)])
                ),
                new RichDescription(
                    "in ",
                    new DirectoryHilite(config[nameof(this.SourceDirectory)])
                )
            );
        }
    }
}
