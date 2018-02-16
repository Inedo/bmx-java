using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
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
    [ScriptAlias("Build-AntProject")]
    [DisplayName("Build Ant Project")]
    [Description("Builds an Ant project using a build file.")]
    [ScriptNamespace("Java", PreferUnqualified = true)]
    [Tag(Tags.Java)]
    [Tag(Tags.Builds)]
    public sealed class AntOperation : ExecuteOperation
    {
        [Required]
        [ScriptAlias("BuildPath")]
        [DefaultValue("build.xml")]
        [DisplayName("Build path")]
        [Description("The project path for the build script.")]
        public string BuildPath { get; set; }
        [Required]
        [ScriptAlias("BuildTarget")]
        [DisplayName("Build target")]
        [Description("The project target for the build script.")]
        public string ProjectBuildTarget { get; set; }
        [ScriptAlias("Properties")]
        [DisplayName("Properties")]
        [Description("The project properties for the build script, in the format PROPERTY=VALUE.")]
        public IEnumerable<string> BuildProperties { get; set; }
        [ScriptAlias("AntPath")]
        [DefaultValue("$AntPath")]
        [DisplayName("ant path")]
        [Description("Full path to ant on the server.")]
        public string AntPath { get; set; }

        public override async Task ExecuteAsync(IOperationExecutionContext context)
        {
            if (string.IsNullOrEmpty(this.AntPath))
            {
                this.LogError("Could not determine the location of ant on this server. To resolve this issue, ensure that ant is available on this server and create a server-scoped variable named $AntPath set to the location of ant (or ant.exe on Windows).");
                return;
            }

            var fileOps = context.Agent.GetService<IFileOperationsExecuter>();
            if (!fileOps.FileExists(this.AntPath))
            {
                this.LogError("Ant not found at: " + this.AntPath);
                return;
            }

            var buildPath = context.ResolvePath(this.BuildPath);
            this.LogDebug("Build path: " + buildPath);

            var buffer = new StringBuilder();
            buffer.Append($"-buildfile \"{this.BuildPath}\" ");

            var properties = this.BuildProperties?.ToList();
            if (properties != null && properties.Count > 0)
            {
                foreach (var property in properties)
                    buffer.Append($"\"-D{property}\" ");
            }

            buffer.Append(this.ProjectBuildTarget);

            fileOps.CreateDirectory(context.WorkingDirectory);

            await this.ExecuteCommandLineAsync(
                context,
                new RemoteProcessStartInfo
                {
                    FileName = this.AntPath,
                    Arguments = buffer.ToString(),
                    WorkingDirectory = context.WorkingDirectory
                }
            );
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(
                new RichDescription(
                    "Build ",
                    new DirectoryHilite(config[nameof(this.BuildPath)])
                ),
                new RichDescription(
                    "with target ",
                    new Hilite(config[nameof(this.ProjectBuildTarget)])
                )
            );
        }
    }
}
