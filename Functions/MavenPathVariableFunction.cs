using System;
using System.ComponentModel;
using Inedo.Agents;
using Inedo.Extensibility;
using Inedo.Extensibility.Operations;
using Inedo.Extensibility.VariableFunctions;

namespace Inedo.BuildMasterExtensions.Java.Functions
{
    [ScriptAlias("MavenPath")]
    [Description("Returns the full path to mvn (or mvn.bat on Windows) for the server in context.")]
    public sealed class MavenPathVariableFunction : ScalarVariableFunction
    {
        protected override object EvaluateScalar(IVariableFunctionContext context)
        {
            var operationContext = context as IOperationExecutionContext;
            if (operationContext == null)
                throw new InvalidOperationException("Operation execution context is not available.");

            var env = operationContext.Agent.GetService<IRemoteProcessExecuter>();
            var antPath = env.GetEnvironmentVariableValue("M2_HOME");
            if (string.IsNullOrEmpty(antPath))
                return string.Empty;

            var fileOps = operationContext.Agent.GetService<IFileOperationsExecuter>();
            antPath = fileOps.CombinePath(antPath, "bin", "mvn");
            if (fileOps.FileExists(antPath))
                return antPath;

            return antPath + ".bat";
        }
    }
}
