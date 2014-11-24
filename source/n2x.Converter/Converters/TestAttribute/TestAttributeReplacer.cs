﻿using System.Linq;
using Microsoft.CodeAnalysis;
using n2x.Converter.Utils;
using Xunit;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;

namespace n2x.Converter.Converters.TestAttribute
{
    public class TestAttributeReplacer : IConverter
    {
        public SyntaxNode Convert(SyntaxNode root, SemanticModel semanticModel)
        {
            var methods = root.Classes().SelectMany(p => p.GetTestMethods(semanticModel));
            var attributes = methods.SelectMany(p => p.GetAttributes<NUnit.Framework.TestAttribute>(semanticModel));

            return root.ReplaceNodes(attributes, (n1, n2) => CreateFactAttributeDeclaration(semanticModel)).NormalizeWhitespace();
        }

        private AttributeSyntax CreateFactAttributeDeclaration(SemanticModel semanticModel)
        {
            return SyntaxFactory.Attribute(SyntaxFactory.ParseName(typeof(FactAttribute).FullName)).NormalizeWhitespace();
        }
    }
}
