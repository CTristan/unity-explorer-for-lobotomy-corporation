using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Harmony;
using Mono.CSharp;
using UnityExplorerForLobotomyCorporation.UnityExplorer.CSConsole;
using UnityExplorerForLobotomyCorporation.UnityExplorer.Extensions;
using UnityExplorerForLobotomyCorporation.UniverseLib.Reflection;
using UnityExplorerForLobotomyCorporation.UniverseLib.Runtime.Mono;

namespace UnityExplorerForLobotomyCorporation.UnityExplorer.Hooks
{
    public class HookInstance
    {
        // Static

        private static readonly StringBuilder evaluatorOutput;
        private static readonly ScriptEvaluator scriptEvaluator = new ScriptEvaluator(new StringWriter(evaluatorOutput = new StringBuilder()));

        // Evaluator.source_file
        private static readonly FieldInfo fi_sourceFile = AccessTools.Field(typeof(Evaluator), "source_file");

        // TypeDefinition.Definition
        private static readonly PropertyInfo pi_Definition = AccessTools.Property(typeof(TypeDefinition), "Definition");

        private readonly string signature;

        // Instance

        public bool Enabled;
        private MethodInfo finalizer;
        private PatchProcessor patchProcessor;
        public string PatchSourceCode;

        private MethodInfo postfix;
        private MethodInfo prefix;

        public MethodInfo TargetMethod;
        private MethodInfo transpiler;

        static HookInstance()
        {
            scriptEvaluator.Run("using System;");
            scriptEvaluator.Run("using System.Text;");
            scriptEvaluator.Run("using System.Reflection;");
            scriptEvaluator.Run("using System.Collections;");
            scriptEvaluator.Run("using System.Collections.Generic;");
        }

        public HookInstance(MethodInfo targetMethod)
        {
            TargetMethod = targetMethod;
            signature = TargetMethod.FullDescription();

            GenerateDefaultPatchSourceCode(targetMethod);

            if (CompileAndGenerateProcessor(PatchSourceCode))
            {
                Patch();
            }
        }

        public bool CompileAndGenerateProcessor(string patchSource)
        {
            // Unpatch();

            var codeBuilder = new StringBuilder();

            try
            {
                // Dynamically compile the patch method

                codeBuilder.AppendLine($"static class DynamicPatch_{DateTime.Now.Ticks}");
                codeBuilder.AppendLine("{");
                codeBuilder.AppendLine(patchSource);
                codeBuilder.AppendLine("}");

                scriptEvaluator.Run(codeBuilder.ToString());

                if (ScriptEvaluator._reportPrinter.ErrorsCount > 0)
                {
                    throw new FormatException("Unable to compile the generated patch!");
                }

                // TODO: Publicize MCS to avoid this reflection
                // Get the most recent Patch type in the source file
                var typeContainer = ((CompilationSourceFile)fi_sourceFile.GetValue(scriptEvaluator)).Containers.Last(it => it.MemberName.Name.StartsWith("DynamicPatch_"));
                // Get the TypeSpec from the TypeDefinition, then get its "MetaInfo" (System.Type)
                var patchClass = ((TypeSpec)pi_Definition.GetValue((Class)typeContainer, null)).GetMetaInfo();

                // Create the harmony patches as defined

                postfix = patchClass.GetMethod("Postfix", ReflectionUtility.FLAGS);
                prefix = patchClass.GetMethod("Prefix", ReflectionUtility.FLAGS);
                // finalizer = patchClass.GetMethod("Finalizer", ReflectionUtility.FLAGS);
                transpiler = patchClass.GetMethod("Transpiler", ReflectionUtility.FLAGS);

                patchProcessor = new PatchProcessor(ExplorerCore.Harmony, TargetMethod, new HarmonyMethod(prefix), new HarmonyMethod(postfix), new HarmonyMethod(transpiler));

                return true;
            }
            catch (Exception ex)
            {
                if (ex is FormatException)
                {
                    var output = scriptEvaluator._textWriter.ToString();
                    var outputSplit = output.Split('\n');
                    if (outputSplit.Length >= 2)
                    {
                        output = outputSplit[outputSplit.Length - 2];
                    }

                    evaluatorOutput.Clear();

                    if (ScriptEvaluator._reportPrinter.ErrorsCount > 0)
                    {
                        ExplorerCore.LogWarning($"Unable to compile the code. Evaluator's last output was:\r\n{output}");
                    }
                    else
                    {
                        ExplorerCore.LogWarning($"Exception generating patch source code: {ex}");
                    }
                }
                else
                {
                    ExplorerCore.LogWarning($"Exception generating patch source code: {ex}");
                }

                // ExplorerCore.Log(codeBuilder.ToString());

                return false;
            }
        }

        private static string FullDescriptionClean(Type type)
        {
            var description = type.FullDescription().Replace("+", ".");
            if (description.EndsWith("&"))
            {
                description = $"ref {description.Substring(0, description.Length - 1)}";
            }

            return description;
        }

        private string GenerateDefaultPatchSourceCode(MethodInfo targetMethod)
        {
            var codeBuilder = new StringBuilder();

            codeBuilder.Append("static void Postfix(");

            var isStatic = targetMethod.IsStatic;

            var arguments = new List<string>();

            if (!isStatic)
            {
                arguments.Add($"{FullDescriptionClean(targetMethod.DeclaringType)} __instance");
            }

            if (targetMethod.ReturnType != typeof(void))
            {
                arguments.Add($"{FullDescriptionClean(targetMethod.ReturnType)} __result");
            }

            var parameters = targetMethod.GetParameters();

            var paramIdx = 0;
            foreach (var param in parameters)
            {
                arguments.Add($"{FullDescriptionClean(param.ParameterType)} __{paramIdx}");
                paramIdx++;
            }

            codeBuilder.Append(string.Join(", ", arguments.ToArray()));

            codeBuilder.Append(")\n");

            // Patch body

            codeBuilder.AppendLine("{");
            codeBuilder.AppendLine("    try {");
            codeBuilder.AppendLine("       StringBuilder sb = new StringBuilder();");
            codeBuilder.AppendLine("       sb.AppendLine(\"--------------------\");");
            codeBuilder.AppendLine($"       sb.AppendLine(\"{signature}\");");

            if (!targetMethod.IsStatic)
            {
                codeBuilder.AppendLine("       sb.Append(\"- __instance: \").AppendLine(__instance.ToString());");
            }

            paramIdx = 0;
            foreach (var param in parameters)
            {
                codeBuilder.Append($"       sb.Append(\"- Parameter {paramIdx} '{param.Name}': \")");

                var pType = param.ParameterType;
                if (pType.IsByRef)
                {
                    pType = pType.GetElementType();
                }

                if (pType.IsValueType)
                {
                    codeBuilder.AppendLine($".AppendLine(__{paramIdx}.ToString());");
                }
                else
                {
                    codeBuilder.AppendLine($".AppendLine(__{paramIdx}?.ToString() ?? \"null\");");
                }

                paramIdx++;
            }

            if (targetMethod.ReturnType != typeof(void))
            {
                codeBuilder.Append("       sb.Append(\"- Return value: \")");
                if (targetMethod.ReturnType.IsValueType)
                {
                    codeBuilder.AppendLine(".AppendLine(__result.ToString());");
                }
                else
                {
                    codeBuilder.AppendLine(".AppendLine(__result?.ToString() ?? \"null\");");
                }
            }

            codeBuilder.AppendLine("       UnityExplorer.ExplorerCore.Log(sb.ToString());");
            codeBuilder.AppendLine("    }");
            codeBuilder.AppendLine("    catch (System.Exception ex) {");
            codeBuilder.AppendLine($"        UnityExplorer.ExplorerCore.LogWarning($\"Exception in patch of {signature}:\\n{{ex}}\");");
            codeBuilder.AppendLine("    }");

            codeBuilder.AppendLine("}");

            return PatchSourceCode = codeBuilder.ToString();
        }

        // public void TogglePatch()
        // {
        //     if (!Enabled)
        //     {
        //         Patch();
        //     }
        //     else
        //     {
        //         Unpatch();
        //     }
        // }

        public void Patch()
        {
            try
            {
                patchProcessor.Patch();

                Enabled = true;
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning($"Exception hooking method!\r\n{ex}");
            }
        }

        // public void Unpatch()
        // {
        //     try
        //     {
        //         if (prefix != null)
        //         {
        //             patchProcessor.Unpatch(prefix);
        //         }
        //
        //         if (postfix != null)
        //         {
        //             patchProcessor.Unpatch(postfix);
        //         }
        //
        //         if (finalizer != null)
        //         {
        //             patchProcessor.Unpatch(finalizer);
        //         }
        //
        //         if (transpiler != null)
        //         {
        //             patchProcessor.Unpatch(transpiler);
        //         }
        //
        //         Enabled = false;
        //     }
        //     catch (Exception ex)
        //     {
        //         ExplorerCore.LogWarning($"Exception unpatching method: {ex}");
        //     }
        // }
    }
}
