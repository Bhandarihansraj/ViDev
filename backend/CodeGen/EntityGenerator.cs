using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using ViDev.Api.CodeGen.Models;

namespace ViDev.Api.CodeGen;

public sealed class EntityGenerator
{
    private static readonly Regex ValidNameRegex = new("^[A-Za-z][A-Za-z0-9_]*$", RegexOptions.Compiled);

    public string Generate(AstEntityNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (!IsValidName(node.Name))
        {
            throw new ArgumentException($"Invalid entity name: {node.Name}");
        }

        var usingDirectives = new[]
        {
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System")),
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.ComponentModel.DataAnnotations")),
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.ComponentModel.DataAnnotations.Schema"))
        };

        var namespaceDeclaration = SyntaxFactory.FileScopedNamespaceDeclaration(SyntaxFactory.ParseName("Generated.Entities"));

        var classAttributes = new List<AttributeListSyntax>();
        if (!string.IsNullOrWhiteSpace(node.TableName))
        {
            classAttributes.Add(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Attribute(SyntaxFactory.ParseName("Table"))
                    .WithArgumentList(SyntaxFactory.AttributeArgumentList(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression(
                            SyntaxKind.StringLiteralExpression,
                            SyntaxFactory.Literal(node.TableName)
                        ))
                    )))
            )));
        }

        var classMembers = new List<MemberDeclarationSyntax>();

        if (node.Properties != null)
        {
            foreach (var prop in node.Properties)
            {
                if (!IsValidName(prop.Name) || !IsValidName(prop.Type, allowGenerics: true)) continue;

                var propAttributes = new List<AttributeListSyntax>();

                if (prop.IsPrimaryKey)
                {
                    propAttributes.Add(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Attribute(SyntaxFactory.ParseName("Key")))));
                }

                if (prop.IsRequired)
                {
                    propAttributes.Add(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Attribute(SyntaxFactory.ParseName("Required")))));
                }

                if (prop.MaxLength.HasValue)
                {
                    propAttributes.Add(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Attribute(SyntaxFactory.ParseName("MaxLength"))
                            .WithArgumentList(SyntaxFactory.AttributeArgumentList(SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression(
                                    SyntaxKind.NumericLiteralExpression,
                                    SyntaxFactory.Literal(prop.MaxLength.Value)
                                ))
                            )))
                    )));
                }

                var propDeclaration = SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName(prop.Type), prop.Name)
                    .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                    .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.List(new[]
                    {
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                    })));

                if (propAttributes.Count > 0)
                {
                    propDeclaration = propDeclaration.WithAttributeLists(SyntaxFactory.List(propAttributes));
                }

                if (prop.Type == "string" && !prop.IsPrimaryKey)
                {
                    propDeclaration = propDeclaration.WithInitializer(SyntaxFactory.EqualsValueClause(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)),
                            SyntaxFactory.IdentifierName("Empty")
                        )
                    )).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
                }

                classMembers.Add(propDeclaration);
            }
        }

        var classDeclaration = SyntaxFactory.ClassDeclaration(node.Name)
            .WithAttributeLists(SyntaxFactory.List(classAttributes))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.SealedKeyword)))
            .WithMembers(SyntaxFactory.List(classMembers));

        namespaceDeclaration = namespaceDeclaration.AddMembers(classDeclaration);

        var compilationUnit = SyntaxFactory.CompilationUnit()
            .WithUsings(SyntaxFactory.List(usingDirectives))
            .AddMembers(namespaceDeclaration)
            .NormalizeWhitespace();

        using var workspace = new AdhocWorkspace();
        var formattedResult = Formatter.Format(compilationUnit, workspace);
        return formattedResult.ToFullString();
    }

    private static bool IsValidName(string? name, bool allowGenerics = false)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        
        if (allowGenerics)
        {
            var cleanName = Regex.Replace(name, @"[<>]", "");
            return ValidNameRegex.IsMatch(cleanName);
        }

        return ValidNameRegex.IsMatch(name);
    }
}
