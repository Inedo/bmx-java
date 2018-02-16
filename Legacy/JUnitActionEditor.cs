using System;
using System.Text.RegularExpressions;
using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.JUnit
{
    internal sealed class JUnitActionEditor : ActionEditorBase
    {
        private ValidatingTextBox txtGroupName;
        private FileBrowserTextBox ctlJavaPath;
        private ValidatingTextBox txtExtensionDirectories;
        private ValidatingTextBox txtSearchPattern;

        public override bool DisplaySourceDirectory => true;

        public override void BindToForm(ActionBase extension)
        {
            var jUnit = (JUnitAction)extension;
            if (jUnit.ExtensionDirectories != null)
                txtExtensionDirectories.Text = string.Join(Environment.NewLine, jUnit.ExtensionDirectories);
            ctlJavaPath.Text = jUnit.JavaPath;
            txtSearchPattern.Text = jUnit.SearchPattern;
            txtGroupName.Text = jUnit.GroupName;
        }

        public override ActionBase CreateFromForm()
        {
            return new JUnitAction
            {
                ExtensionDirectories = Regex.Split(this.txtExtensionDirectories.Text, "\r?\n"),
                JavaPath = ctlJavaPath.Text,
                SearchPattern = txtSearchPattern.Text,
                GroupName = txtGroupName.Text
            };
        }

        protected override void CreateChildControls()
        {
            this.txtGroupName = new ValidatingTextBox
            {
                Required = true,
                Text = this.Plan.Deployable_Name
            };

            this.ctlJavaPath = new FileBrowserTextBox { ServerId = this.ServerId };

            this.txtExtensionDirectories = new ValidatingTextBox
            {
                TextMode = TextBoxMode.MultiLine,
                Rows = 4
            };

            this.txtSearchPattern = new ValidatingTextBox
            {
                Text = (new JUnitAction()).SearchPattern
            };

            this.Controls.Add(
                new SlimFormField("Test group:", this.txtGroupName),
                new SlimFormField("Java path:", this.ctlJavaPath),
                new SlimFormField("Extensions paths:", this.txtExtensionDirectories)
                {
                    HelpText = "The relative path of Java extensions. Note that jUnit 4.x must be in the default or one of these directories."
                },
                new SlimFormField("Search pattern:", this.txtSearchPattern)
                {
                    HelpText = "The file mask that indicates which classes are to be tested."
                }
            );
        }
    }
}
