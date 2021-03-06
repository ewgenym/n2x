﻿using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using n2x.Converter.Converters.TestFixtureAttribute;
using n2x.Converter.Utils;
using n2x.Tests.Utils;
using NUnit.Framework;
using Xunit;
using Assert = Xunit.Assert;

namespace n2x.Tests.Converters
{
    public class behaves_like_converting_TestFixtureAttribute : ConverterSpecification<TestFixtureAttributeConverterProvider>
    {
        
        protected SyntaxTree SyntaxTree { get; set; }
        protected NamespaceDeclarationSyntax NamespaceSyntax { get; set; }
        protected ClassDeclarationSyntax TestClassSyntax { get; set; }
        protected SemanticModel SemanticModel { get; set; }
        protected ClassDeclarationSyntax TestDataClassSyntax { get; set; }

        public override void Context()
        {
            base.Context();

            Code = new TestCode(
                @"using NUnit.Framework;

namespace n2x
{
    public struct TestCategoryProvider
    {
        public const string FullRegression = ""FullRegression"";
        public const string Smoke = ""Smoke"";
    }

    [TestFixture(Category = TestCategoryProvider.FullRegression + "", "" + TestCategoryProvider.Smoke)]
    [Explicit]
    public class Test
    {
        [Test]
        public void should_do_the_magic()
        {
        }
    }
}");

        }

        public override void Because()
        {
            base.Because();

            SyntaxTree = Result.GetSyntaxTreeAsync().Result;
            NamespaceSyntax = (NamespaceDeclarationSyntax)Compilation.Members.Single();
            TestClassSyntax = NamespaceSyntax.Members.OfType<ClassDeclarationSyntax>().Single(c => c.Identifier.Text == "Test");
            SemanticModel = Result.GetSemanticModelAsync().Result;

            Console.Out.WriteLine("{0}", Compilation.ToFullString());
        }

    }

    public class when_converting_TestFixtureAttribute_with_category : behaves_like_converting_TestFixtureAttribute
    {
        //TODO: move to base ConvterTest class
        [Fact]
        public override void should_not_produce_compilation_errors_and_warnings()
        {
            var hasCompilationErrorsOrWarnings = Compilation.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error || d.Severity == DiagnosticSeverity.Warning);

            Assert.False(hasCompilationErrorsOrWarnings);
        }

        [Fact]
        public void should_replace_categorized_TestFixtureAttribute_with_Trait_attribute()
        {
            var hasTraitAttributeWithCategory = TestClassSyntax.AttributeLists.SelectMany(a => a.Attributes)
                .Any(a => a.IsTraitAttributeWith("Category", "FullRegression, Smoke", SemanticModel)
                );

            Assert.True(hasTraitAttributeWithCategory);
        }

        //TODO: move to base ConvterTest class
        [Fact]
        public void should_match_etalon_document()
        {
            Assert.Equal(@"using NUnit.Framework;

namespace n2x
{
    public struct TestCategoryProvider
    {
        public const string FullRegression = ""FullRegression"";
        public const string Smoke = ""Smoke"";
    }

    [Xunit.TraitAttribute(""Category"", TestCategoryProvider.FullRegression + "", "" + TestCategoryProvider.Smoke)]
    [Explicit]
    public class Test
    {
        [Test]
        public void should_do_the_magic()
        {
        }
    }
}",
Compilation.ToFullString());
        }
    }

    public class when_converting_TestFixtureAttribute_without_category : behaves_like_converting_TestFixtureAttribute
    {
        public override void Context()
        {
            base.Context();

            Code = new TestCode(
@"using NUnit.Framework;

namespace n2x
{
    [TestFixture]
    [Explicit]
    public class Test
    {
        [Test]
        public void should_do_the_magic()
        {
        }
    }
}");
        }

        //TODO: move to base ConvterTest class
        [Fact]
        public override void should_not_produce_compilation_errors_and_warnings()
        {
            var hasCompilationErrorsOrWarnings = Compilation.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error || d.Severity == DiagnosticSeverity.Warning);

            Assert.False(hasCompilationErrorsOrWarnings);
        }

        [Fact]
        public void should_remove_TestFixtureAttribute()
        {
            var hasTestFixtureAttribute = TestClassSyntax.AttributeLists.SelectMany(a => a.Attributes)
                .Any(a => a.IsOfType<TestFixtureAttribute>(SemanticModel));

            Assert.False(hasTestFixtureAttribute);
        }

        [Fact]
        public void should_not_add_Trait_attribute()
        {
            var hasTraitAttributes = TestClassSyntax.AttributeLists.SelectMany(a => a.Attributes)
                .Any(a => a.IsOfType<TraitAttribute>(SemanticModel));

            Assert.False(hasTraitAttributes);
        }

        [Fact]
        public void should_not_leave_empty_attribute_lists()
        {
            var hasEmptyAttributeLists = TestClassSyntax.AttributeLists.Any(l => !l.Attributes.Any());

            Assert.False(hasEmptyAttributeLists);
        }

        //TODO: move to base ConvterTest class
        [Fact]
        public void should_match_etalon_document()
        {
            Assert.Equal(
@"using NUnit.Framework;

namespace n2x
{
    [Explicit]
    public class Test
    {
        [Test]
        public void should_do_the_magic()
        {
        }
    }
}", Compilation.ToFullString());
        }
    }
}