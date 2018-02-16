using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Inedo.Agents;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Data;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Operations;
using Inedo.IO;

namespace Inedo.BuildMasterExtensions.Java.Operations
{
    [ScriptAlias("Execute-JUnit")]
    [ScriptNamespace("Java", PreferUnqualified = true)]
    [DisplayName("Execute jUnit Tests")]
    [Description("Executes jUnit 4.x and later tests.")]
    [Tag(Tags.Java), Tag(Tags.UnitTests)]
    public sealed class JUnitOperation : ExecuteOperation
    {
        private StringBuilder standardOut = new StringBuilder();

        [Required]
        [ScriptAlias("Include")]
        [DefaultValue("**Test.class")]
        [Description(CommonDescriptions.IncludeMask)]
        public IEnumerable<string> Includes { get; set; }
        [ScriptAlias("Exclude")]
        [Description(CommonDescriptions.ExcludeMask)]
        public IEnumerable<string> Excludes { get; set; }
        [ScriptAlias("From")]
        [Description(CommonDescriptions.SourceDirectory)]
        [DisplayName("Source directory")]
        public string SourceDirectory { get; set; }
        [ScriptAlias("ExtensionDirectories")]
        [DisplayName("Extension directories")]
        [Description("Cross-compile against the specified extension directories. Each JAR archive in the specified directories is searched for class files.")]
        public IEnumerable<string> ExtensionDirectories { get; set; }
        [ScriptAlias("JavaPath")]
        [DefaultValue("$JavaPath")]
        [DisplayName("Java path")]
        [Description("Full path to java (java.exe on Windows) on the target server.")]
        public string JavaPath { get; set; }

        public override async Task ExecuteAsync(IOperationExecutionContext context)
        {
            var fileOps = context.Agent.GetService<IFileOperationsExecuter>();
            var bmjPath = fileOps.CombinePath(fileOps.GetBaseWorkingDirectory(), "ExtTemp", "Java", "BuildMaster.jar");

            var sourceDirectory = context.ResolvePath(this.SourceDirectory);
            if (!fileOps.DirectoryExists(sourceDirectory))
            {
                this.LogError($"Source directory {sourceDirectory} not found.");
                return;
            }

            this.LogDebug("Source directory: " + sourceDirectory);

            var testClasses = (from f in fileOps.GetFileSystemInfos(sourceDirectory, new MaskingContext(this.Includes, this.Excludes)).OfType<SlimFileSystemInfo>()
                               where f.FullName.EndsWith(".class", StringComparison.OrdinalIgnoreCase)
                               select f.FullName.Substring(sourceDirectory.Length, f.FullName.Length - sourceDirectory.Length - ".class".Length).Trim('/', '\\').Replace('/', '.').Replace('\\', '.')).ToList();

            if (testClasses.Count == 0)
            {
                this.LogWarning($"No test class files found in {sourceDirectory}.");
                return;
            }

            var testClassesFileName = fileOps.CombinePath(sourceDirectory, Guid.NewGuid().ToString("N"));
            try
            {
                fileOps.WriteAllText(testClassesFileName, string.Join(fileOps.NewLine, testClasses));

                await this.ExecuteCommandLineAsync(
                    context,
                    new RemoteProcessStartInfo
                    {
                        FileName = this.JavaPath,
                        Arguments = $"-cp .;{bmjPath} -Djava.exe.dirs=\"{string.Join(";", this.ExtensionDirectories ?? Enumerable.Empty<string>())}\" inedo.buildmasterextensions.java.jUnitAction \"@{testClassesFileName}\"",
                        WorkingDirectory = sourceDirectory
                    }
                );

                XDocument testResults;
                try
                {
                    testResults = XDocument.Parse(standardOut.ToString());
                }
                catch (Exception ex)
                {
                    this.LogError("Unable to load test results: " + ex.Message);
                    this.LogDebug(standardOut.ToString());
                    return;
                }

                var testTime = DateTime.UtcNow;

                foreach (var tr in testResults.Descendants("TestResult"))
                {
                    var failures = tr.Descendants("Failure").ToList();

                    int runCount = (int)tr.Attribute("RunCount");
                    int failCount = failures.Count;
                    int ignoreCount = (int)tr.Attribute("IgnoreCount");
                    int runTime = (int)tr.Attribute("RunTime");
                    var testName = (string)tr.Attribute("Class");

                    var testResult = new StringBuilder();
                    if (runCount > 0)
                    {
                        testResult.AppendLine("Tests run: " + runCount.ToString());
                        if (failCount == 0)
                            testResult.AppendLine().AppendLine("No failures.");
                        else if (failCount == 1)
                            testResult.AppendLine().AppendLine("1 failure.");
                        else
                            testResult.AppendLine().AppendLine(failCount.ToString() + " failures.");
                    }

                    foreach (var fail in failures)
                    {
                        testResult.AppendLine();
                        testResult.AppendLine();
                        testResult.AppendLine("__" + fail.Attribute("TestHeader").Value + "__");
                        testResult.AppendLine("Exception: " + fail.Attribute("ExceptionClass").Value);
                        testResult.AppendLine("Message: " + fail.Attribute("Message").Value);
                    }

                    var tout = tr.Element("TestOutput");
                    if (tout != null)
                    {
                        testResult.AppendLine();
                        testResult.AppendLine();
                        testResult.AppendLine("__Output__");
                        foreach (var cdata in tout.Nodes().OfType<XCData>())
                            testResult.AppendLine(cdata.Value);
                    }

                    DB.BuildTestResults_RecordTestResult(
                        Execution_Id: context.ExecutionId,
                        Group_Name: null,
                        Test_Name: testName,
                        TestStatus_Code: failCount == 0 ? Domains.TestStatusCodes.Passed : Domains.TestStatusCodes.Failed,
                        TestResult_Text: testResult.ToString(),
                        TestStarted_Date: testTime,
                        TestEnded_Date: testTime.AddMilliseconds(runTime)
                    );

                    testTime = testTime.AddMilliseconds(runTime);
                }
            }
            finally
            {
                try
                {
                    fileOps.DeleteFile(testClassesFileName);
                }
                catch
                {
                }
            }
        }

        protected override void LogProcessOutput(string text)
        {
            lock (this.standardOut)
            {
                this.standardOut.AppendLine(text);
            }
        }
        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(
                new RichDescription(
                    "Run jUnit tests on ",
                    new MaskHilite(config[nameof(this.Includes)], config[nameof(this.Excludes)])
                ),
                new RichDescription(
                    "in ",
                    new DirectoryHilite(config[nameof(this.SourceDirectory)])
                )
            );
        }
    }
}
