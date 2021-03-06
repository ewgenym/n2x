﻿using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using n2x.Converter.Utils;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;

namespace n2x.Converter.Converters.TearDownAttribute
{
    internal class TearDownMethodMover : IConverter
    {
        public SyntaxNode Convert(SyntaxNode root, SemanticModel semanticModel)
        {
            var dict = new Dictionary<SyntaxNode, SyntaxNode>();

            foreach (var @class in root.Classes())
            {
                var hasTearDownMethod = @class.HasTearDownMethods(semanticModel);

                if (hasTearDownMethod)
                {
                    var tearDownMethod = @class.GetTearDownMethods(semanticModel).FirstOrDefault();

                    if (@class.HasDisposeMethod())
                    {
                        var disposeMethod = @class.GetDisposeMethod();
                        var newDisposeMethod = disposeMethod.AddBodyStatements(tearDownMethod.Body.Statements.ToArray());
                        dict.Add(disposeMethod, newDisposeMethod);
                    }
                    else
                    {
                        var disposeMethod = GetDisposeMethodDeclaration(tearDownMethod, @class.HasDisposableBaseClass(semanticModel));
                        var modifiedTestDataClass = @class.AddMembers(disposeMethod);
                        dict.Add(@class, modifiedTestDataClass);
                    }
                }
            }

            return root.ReplaceNodes(dict);
        }

        private MemberDeclarationSyntax GetDisposeMethodDeclaration(MethodDeclarationSyntax tearDownMethod, bool baseClassIsDisposable)
        {
            var tokens = SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
            if (baseClassIsDisposable)
            {
                tokens = tokens.Add(SyntaxFactory.Token(SyntaxKind.OverrideKeyword));
            }
            else
            {
                tokens = tokens.Add(SyntaxFactory.Token(SyntaxKind.VirtualKeyword));
            }

            var body = tearDownMethod.Body;
            if (baseClassIsDisposable)
            {
                var baseDisposeCall = SyntaxFactory.ExpressionStatement(SyntaxFactory.ParseExpression("base.Dispose()"));
                var list = new List<StatementSyntax>();
                list.Add(baseDisposeCall);
                list.AddRange(body.Statements);
                body = SyntaxFactory.Block(list);
            }

            return SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), "Dispose")
                .WithModifiers(SyntaxFactory.TokenList(tokens))
                .WithBody(body);
        }
    }
}