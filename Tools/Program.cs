﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NConsoler;
using TechTalk.SpecFlow.Generator;
using TechTalk.SpecFlow.Generator.Configuration;
using TechTalk.SpecFlow.Generator.Project;
using TechTalk.SpecFlow.Reporting.MsTestExecutionReport;
using TechTalk.SpecFlow.Reporting.NUnitExecutionReport;
using TechTalk.SpecFlow.Reporting.StepDefinitionReport;
using TechTalk.SpecFlow.Tracing;
using MsBuildProjectReader = TechTalk.SpecFlow.Generator.Project.MsBuildProjectReader;
using TextWriterTraceListener = TechTalk.SpecFlow.Tracing.TextWriterTraceListener;

namespace TechTalk.SpecFlow.Tools
{
    public class Program
    {
        private static void Main(string[] args)
        {
            Consolery.Run(typeof(Program), args);
            return;
        }

        [Action("Generate tests from all feature files in a project")]
        public static void GenerateAll(
            [Required(Description = "Visual Studio Project File containing features")] string projectFile,
            [Optional(false, "force", "f")] bool forceGeneration,
            [Optional(false, "verbose", "v")] bool verboseOutput,
            [Optional(false, "debug", Description = "Used for tool integration")] bool requestDebuggerToAttach)
        {
            if (requestDebuggerToAttach)
                Debugger.Launch();


            SpecFlowProject specFlowProject = MsBuildProjectReader.LoadSpecFlowProjectFromMsBuild(projectFile);
            ITraceListener traceListener = verboseOutput ? (ITraceListener)new TextWriterTraceListener(Console.Out) : new NullListener();
            var batchGenerator = new BatchGenerator(traceListener, new TestGeneratorFactory());

            batchGenerator.OnError += batchGenerator_OnError;

            batchGenerator.ProcessProject(specFlowProject, forceGeneration);
        }

        static void batchGenerator_OnError(Generator.Interfaces.FeatureFileInput arg1, Generator.Interfaces.TestGeneratorResult arg2)
        {
            Console.Error.WriteLine("Error file {0}", arg1.ProjectRelativePath);
            Console.Error.WriteLine(String.Join(Environment.NewLine, arg2.Errors.Select(e => String.Format("Line {0}:{1} - {2}", e.Line, e.LinePosition, e.Message))));
        }

        #region Reports

		// TODO: Make the default value for binFolder more cross-platform compatible.  Easy to work around for now, by providing a value.  This could mean not using NConsoler.
        [Action("Generates a report about usage and binding of steps")]
        public static void StepDefinitionReport(
            [Required(Description = "Visual Studio Project File containing specs")] string projectFile,
            [Optional("", Description = "Xslt file to use, defaults to built-in stylesheet if not provided")] string xsltFile,
            [Optional("bin\\Debug", Description = @"Path for Spec dll e.g. Company.Specs.dll. Defaults to bin\Debug ")] string binFolder,
            [Optional("StepDefinitionReport.html", "out", Description = "Generated Output File. Defaults to StepDefinitionReport.html")] string outputFile)
        {
            StepDefinitionReportParameters reportParameters = 
                new StepDefinitionReportParameters(projectFile, outputFile, xsltFile, binFolder, true);
            var generator = new StepDefinitionReportGenerator(reportParameters);
            generator.GenerateAndTransformReport();
        }

        [Action("Formats an NUnit execution report to SpecFlow style")]
        public static void NUnitExecutionReport([Required(Description = "Visual Studio Project File containing specs")] string projectFile,
            [Optional("TestResult.xml", Description = "Xml Test Result file generated by NUnit. Defaults to TestResult.xml")] string xmlTestResult,
            [Optional("", Description = "Xslt file to use, defaults to built-in stylesheet if not provided")] string xsltFile,
            [Optional("TestResult.txt", "testOutput", Description = "The labeled test output file generated by nunit-console. Defaults to TestResult.txt")] string labeledTestOutput,
            [Optional("TestResult.html", "out", Description = "Generated Output File. Defaults to TestResult.html")] string outputFile)
        {
            var reportParameters = 
                new NUnitExecutionReportParameters(projectFile, xmlTestResult, labeledTestOutput, outputFile, xsltFile);         
            var generator = new NUnitExecutionReportGenerator(reportParameters);
            generator.GenerateAndTransformReport();
        }

        [Action("Formats an MsTest execution report to SpecFlow style")]
        public static void MsTestExecutionReport([Required(Description = "Visual Studio Project File containing specs")] string projectFile,
            [Optional("TestResult.trx", Description = "Test Result file generated by MsTest. Defaults to TestResult.trx")] string testResult,
            [Optional("", Description = "Xslt file to use, defaults to built-in stylesheet if not provided")] string xsltFile,
            [Optional("TestResult.html", "out", Description = "Generated Output File. Defaults to TestResult.html")] string outputFile)
        {
            var reportParameters =
                new MsTestExecutionReportParameters(projectFile, testResult, outputFile, xsltFile);
            var generator = new MsTestExecutionReportGenerator(reportParameters);
            generator.GenerateAndTransformReport();
        }
        #endregion
    }
}