﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Reqnroll.FeatureSourceGenerator;
using Reqnroll.FeatureSourceGenerator.CSharp;
using Reqnroll.FeatureSourceGenerator.SourceModel;
using Xunit.Abstractions;

namespace Reqnroll.FeatureSourceGenerator;

public class MSTestFeatureSourceGenerationTests(ITestOutputHelper output)
{
    [Fact]
    public void GeneratorProducesMSTestOutputWhenWhenBuildPropertyConfiguredForMSTest()
    {
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(asm => !asm.IsDynamic)
            .Select(asm => MetadataReference.CreateFromFile(asm.Location));

        var compilation = CSharpCompilation.Create("test", references: references);

        var generator = new CSharpTestFixtureSourceGenerator([BuiltInTestFrameworkHandlers.MSTest]);

        const string featureText =
            """
            #language: en
            @featureTag1
            Feature: Calculator

            @mytag
            Scenario: Add two "simple" numbers
                Given the first number is 50
                And the second number is 70
                When the two numbers are added
                Then the result should be 120
            """;

        var optionsProvider = new FakeAnalyzerConfigOptionsProvider(
            new InMemoryAnalyzerConfigOptions(new Dictionary<string, string>
            {
                { "build_property.ReqnrollTargetTestFramework", "MSTest" }
            }));

        var driver = CSharpGeneratorDriver
            .Create(generator)
            .AddAdditionalTexts([new FeatureFile("Calculator.feature", featureText)])
            .WithUpdatedAnalyzerConfigOptions(optionsProvider)
            .RunGeneratorsAndUpdateCompilation(compilation, out var generatedCompilation, out var diagnostics);

        diagnostics.Should().BeEmpty();

        var generatedSyntaxTree = generatedCompilation.SyntaxTrees.Should().ContainSingle()
            .Which.Should().BeAssignableTo<CSharpSyntaxTree>().Subject!;

        generatedSyntaxTree.GetDiagnostics().Should().BeEmpty();

        generatedSyntaxTree.GetRoot().Should().ContainSingleNamespaceDeclaration("test")
            .Which.Should().ContainSingleClassDeclaration("CalculatorFeature")
            .Which.Should().HaveSingleAttribute("global::Microsoft.VisualStudio.TestTools.UnitTesting.TestClass");
    }

    [Fact]
    public void GeneratorProducesMSTestOutputWhenWhenEditorConfigConfiguredForMSTest()
    {
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(asm => !asm.IsDynamic)
            .Select(asm => MetadataReference.CreateFromFile(asm.Location));

        var compilation = CSharpCompilation.Create("test", references: references);

        var generator = new CSharpTestFixtureSourceGenerator([BuiltInTestFrameworkHandlers.MSTest]);

        const string featureText =
            """
            #language: en
            @featureTag1
            Feature: Calculator

            @mytag
            Scenario: Add two numbers
                Given the first number is 50
                And the second number is 70
                When the two numbers are added
                Then the result should be 120
            """;

        var optionsProvider = new FakeAnalyzerConfigOptionsProvider(
            new InMemoryAnalyzerConfigOptions(new Dictionary<string, string>
            {
                { "reqnroll.target_test_framework", "MSTest" }
            }));

        var driver = CSharpGeneratorDriver
            .Create(generator)
            .AddAdditionalTexts([new FeatureFile("Calculator.feature", featureText)])
            .WithUpdatedAnalyzerConfigOptions(optionsProvider)
            .RunGeneratorsAndUpdateCompilation(compilation, out var generatedCompilation, out var diagnostics);

        diagnostics.Should().BeEmpty();

        var generatedSyntaxTree = generatedCompilation.SyntaxTrees.Should().ContainSingle()
            .Which.Should().BeAssignableTo<CSharpSyntaxTree>().Subject!;

        output.WriteLine($"Generated source:\n{generatedSyntaxTree}");

        generatedSyntaxTree.GetDiagnostics().Should().BeEmpty();

        generatedSyntaxTree.GetRoot().Should().ContainSingleNamespaceDeclaration("test")
            .Which.Should().ContainSingleClassDeclaration("CalculatorFeature")
            .Which.Should().HaveSingleAttribute("global::Microsoft.VisualStudio.TestTools.UnitTesting.TestClass");
    }
}

internal static class MSTestSyntax
{
    private static readonly NamespaceString Namespace = new("Microsoft.VisualStudio.TestTools.UnitTesting");

    public static AttributeDescriptor Attribute(string type, params object?[] args)
    {
        return new AttributeDescriptor(
            new NamedTypeIdentifier(Namespace, new IdentifierString(type)),
            [.. args]);
    }

    private static ExpressionSyntax Argument(object? arg)
    {
        return arg switch
        {
            string s => SyntaxFactory.LiteralExpression(
                SyntaxKind.StringLiteralExpression,
                SyntaxFactory.Literal(s)),

            string[] array => ArrayCreation(array),
            object[] array => ArrayCreation(array),

            _ => throw new NotImplementedException()
        };
    }

    private static ArrayCreationExpressionSyntax ArrayCreation(string[] array) =>
        ArrayCreation(
            SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)),
            array);

    private static ArrayCreationExpressionSyntax ArrayCreation(object[] array) =>
        ArrayCreation(
            SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)),
            array);

    private static ArrayCreationExpressionSyntax ArrayCreation<T>(TypeSyntax type, T[] array)
    {
        var creation = SyntaxFactory.ArrayCreationExpression(
            SyntaxFactory.ArrayType(
                type,
                SyntaxFactory.SingletonList(
                    SyntaxFactory.ArrayRankSpecifier(
                        SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                            array.Length > 0 ? 
                                SyntaxFactory.LiteralExpression(
                                    SyntaxKind.NumericLiteralExpression,
                                    SyntaxFactory.Literal(0)) : 
                                SyntaxFactory.OmittedArraySizeExpression())))));

        if (array.Length > 0)
        {
            return creation
                .WithInitializer(
                    SyntaxFactory.InitializerExpression(
                        SyntaxKind.ArrayInitializerExpression,
                        SyntaxFactory.SeparatedList(
                            array.Select(arg => Argument(arg)))));
        }
        else
        {
            return creation;
        }
    }
}
