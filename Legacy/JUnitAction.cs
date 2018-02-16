using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Xml;
using Inedo.Agents;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Extensibility.Actions.Testing;
using Inedo.BuildMaster.Web;
using Inedo.Documentation;
using Inedo.Serialization;
using Inedo.Web;

namespace Inedo.BuildMasterExtensions.JUnit
{
    [DisplayName("Execute jUnit Tests")]
    [Description("Executes jUnit 4.x and later tests.")]
    [CustomEditor(typeof(JUnitActionEditor))]
    [Tag(Tags.Java), Tag(Tags.UnitTests)]
    [ConvertibleToOperation(typeof(JUnitActionImporter))]
    public sealed class JUnitAction : UnitTestActionBase
    {
        private StringBuilder standardOut = new StringBuilder();

        /// <summary>
        /// Gets or sets the path to java.
        /// </summary>
        [Persistent]
        public string JavaPath { get; set; }

        /// <summary>
        /// Gets or sets the search pattern to use for test classes.
        /// </summary>
        [Persistent]
        public string SearchPattern { get; set; } = "*Test.class";

        /// <summary>
        /// Gets or sets the extension directories.
        /// </summary>
        /// <remarks>
        /// Cross-compile against the specified extension directories. Directories is a semicolon-separated list of directories. Each JAR archive in the specified directories is searched for class files. 
        /// </remarks>
        [Persistent]
        public string[] ExtensionDirectories { get; set; }

        public override string ToString()
        {
            return string.Format(
                "Run jUnit Tests on {0} in {1}",
                this.SearchPattern,
                Util.CoalesceStr(this.OverriddenSourceDirectory, "default directory"));
        }

        protected override void RunTests()
        {
            //Output jars
            var bmjPath = Path.Combine(
                this.Context.Agent.GetService<IFileOperationsExecuter>().GetBaseWorkingDirectory(),
                "ExtTemp",
                "JUnit",
                "BuildMaster.jar"
            );

            // Build file list
            var testClasses = Directory.GetFiles(
                this.Context.SourceDirectory,
                this.SearchPattern,
                SearchOption.AllDirectories);
            for (int i = 0; i < testClasses.Length; i++)
            {
                if (testClasses[i].EndsWith(".class"))
                    testClasses[i] = testClasses[i].Substring(0, testClasses[i].Length - ".class".Length);
                testClasses[i] = testClasses[i]
                    .Substring(this.Context.SourceDirectory.Length)
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .Replace(Path.DirectorySeparatorChar, '.')
                    .Replace(Path.AltDirectorySeparatorChar, '.');
            }

            // Write to file
            string FILE_testClasses = Path.GetTempFileName();
            File.WriteAllLines(FILE_testClasses, testClasses);

            // Capture Command Output
            standardOut = new StringBuilder();
            this.ExecuteCommandLine(
                this.JavaPath,
                string.Format(
                    "-cp .;{0} -Djava.ext.dirs=\"{1}\" {2} \"@{3}\"",
                    bmjPath,
                    ExtensionDirectories == null ? null : string.Join(";", ExtensionDirectories),
                    "inedo.buildmasterextensions.java.jUnitAction",
                    FILE_testClasses),
                this.Context.SourceDirectory);

            // Load as XML
            var testResults = new XmlDocument();
            try { testResults.LoadXml(standardOut.ToString()); }
            catch (Exception ex)
            {
                this.LogError("Unable to load test results: " + ex.Message);
                this.LogDebug(standardOut.ToString());
                return;
            }

            var testTime = DateTime.UtcNow;

            // Log Results
            foreach (XmlElement tr in testResults.SelectNodes("TestResults/TestResult"))
            {
                XmlNodeList failures = tr.SelectNodes("Failures/Failure");

                int runCount = int.Parse(tr.Attributes["RunCount"].Value);
                int failCount = failures.Count;
                int ignoreCount = int.Parse(tr.Attributes["IgnoreCount"].Value);
                int runTime = int.Parse(tr.Attributes["RunTime"].Value);
                string testName = tr.Attributes["Class"].Value;

                StringBuilder testResult = new StringBuilder();
                if (runCount > 0)
                {
                    testResult.AppendLine("Tests Run: " + runCount.ToString());
                    if (failCount == 0)
                        testResult.AppendLine().AppendLine("No Failures.");
                    else if (failCount == 1)
                        testResult.AppendLine().AppendLine("1 Failure.");
                    else
                        testResult.AppendLine().AppendLine(failCount.ToString() + " Failures.");
                }

                foreach (XmlElement fail in failures)
                {
                    testResult.AppendLine();
                    testResult.AppendLine();
                    testResult.AppendLine("__" + fail.Attributes["TestHeader"].Value + "__");
                    testResult.AppendLine("Exception: " + fail.Attributes["ExceptionClass"].Value);
                    testResult.AppendLine("Message: " + fail.Attributes["Message"].Value);
                }

                var tout = tr.SelectSingleNode("TestOutput") as XmlElement;
                if (tout != null)
                {
                    testResult.AppendLine();
                    testResult.AppendLine();
                    testResult.AppendLine("__Output__"); 
                    foreach (XmlCDataSection cdata in tout.ChildNodes)
                    {
                        testResult.AppendLine(cdata.Value);
                    }
                }

                this.RecordResult(
                    testName, 
                    failCount == 0, 
                    testResult.ToString(),
                    testTime, testTime.AddMilliseconds(runTime));
                testTime = testTime.AddMilliseconds(runTime);
            }
        }

        protected override void LogProcessOutputData(string data)
        {
            standardOut.AppendLine(data);
        }
    }
}
