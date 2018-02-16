using System;
using System.Text.RegularExpressions;
using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.Java
{
    internal sealed class MavenActionEditor : ActionEditorBase
    {
        private FileBrowserTextBox ctlMavenPath;
        private ValidatingTextBox txtAdditionalArguments;
        private ValidatingTextBox txtGoalsAndPhases;

        public override bool DisplaySourceDirectory => true;

        public override void BindToForm(ActionBase extension)
        {
            var maven = (MavenAction)extension;
            this.txtGoalsAndPhases.Text = string.Join(" ", maven.GoalsAndPhases ?? new string[0]);
            this.txtAdditionalArguments.Text = string.Join(Environment.NewLine, maven.AdditionalArguments ?? new string[0]);
            this.ctlMavenPath.Text = string.IsNullOrEmpty(maven.MavenPath)
                ? this.ctlMavenPath.Text
                : maven.MavenPath;
        }
        public override ActionBase CreateFromForm()
        {
            return new MavenAction
            {
                MavenPath = this.ctlMavenPath.Text,
                GoalsAndPhases = this.txtGoalsAndPhases.Text.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries),
                AdditionalArguments = Regex.Split(this.txtAdditionalArguments.Text.Trim(' '), "\r?\n")
            };
        }

        protected override void CreateChildControls()
        {
            this.ctlMavenPath = new FileBrowserTextBox
            {
                ServerId = this.ServerId,
                Required = true
            };

            this.txtAdditionalArguments = new ValidatingTextBox
            {
                TextMode = TextBoxMode.MultiLine,
                Rows = 4
            };

            this.txtGoalsAndPhases = new ValidatingTextBox();

            this.Controls.Add(
                new SlimFormField("Maven path:", this.ctlMavenPath)
                {
                    HelpText = "The path to the Maven executable (mvn.bat on Windows)."
                },
                new SlimFormField("Goals and phases:", this.txtGoalsAndPhases)
                {
                    HelpText = "The goals and/or phases of the Maven build, separated by spaces."
                },
                new SlimFormField("Additional arguments:", this.txtAdditionalArguments)
                {
                    HelpText = "Any additional arguments for mvn, entered one per line."
                }
            );
        }
    }
}
