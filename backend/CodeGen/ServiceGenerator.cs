using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using ViDev.Api.CodeGen.Models;

namespace ViDev.Api.CodeGen;

public sealed class ServiceGenerator
{
    private static readonly HashSet<string> AllowedAnnotations = new(StringComparer.OrdinalIgnoreCase)
    {
        "JWT", "ValidateModel"
    };

    private static readonly Regex ValidNameRegex = new("^[A-Za-z][A-Za-z0-9_]*$", RegexOptions.Compiled);

    public Dictionary<string, string> Generate(AstServiceNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (!IsValidName(node.Name))
        {
            throw new ArgumentException($"Invalid service name: {node.Name}");
        }

        var results = new Dictionary<string, string>();

        var usingDirectives = new[]
        {
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System")),
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Threading.Tasks"))
        };

        var namespaceDeclarationInterface = SyntaxFactory.FileScopedNamespaceDeclaration(SyntaxFactory.ParseName("Generated.Services"));
        var namespaceDeclarationClass = SyntaxFactory.FileScopedNamespaceDeclaration(SyntaxFactory.ParseName("Generated.Services"));

        // 1. Interface
        var interfaceMembers = new List<MemberDeclarationSyntax>();
        if (node.Methods != null)
        {
            foreach (var method in node.Methods)
            {
                if (!IsValidName(method.Name)) continue;

                var parameters = new List<ParameterSyntax>();
                if (method.Parameters != null)
                {
                    foreach (var param in method.Parameters)
                    {
                        if (!IsValidName(param.Name) || !IsValidName(param.Type, allowGenerics: true)) continue;
                        parameters.Add(SyntaxFactory.Parameter(SyntaxFactory.Identifier(param.Name))
                            .WithType(SyntaxFactory.ParseTypeName(param.Type)));
                    }
                }

                var methodDeclaration = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName(method.ReturnType ?? "void"), method.Name)
                    .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters)))
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
                
                interfaceMembers.Add(methodDeclaration);
            }
        }

        var interfaceDeclaration = SyntaxFactory.InterfaceDeclaration($"I{node.Name}")
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithMembers(SyntaxFactory.List(interfaceMembers));
        
        namespaceDeclarationInterface = namespaceDeclarationInterface.AddMembers(interfaceDeclaration);

        var compilationUnitInterface = SyntaxFactory.CompilationUnit()
            .WithUsings(SyntaxFactory.List(usingDirectives))
            .AddMembers(namespaceDeclarationInterface)
            .NormalizeWhitespace();

        using var workspaceInterface = new AdhocWorkspace();
        results[$"I{node.Name}.cs"] = Formatter.Format(compilationUnitInterface, workspaceInterface).ToFullString();

        // 2. Class
        var classMembers = new List<MemberDeclarationSyntax>();

        // Fields
        if (node.Sockets != null)
        {
            foreach (var socket in node.Sockets)
            {
                if (IsValidName(socket.TargetField) && IsValidName(socket.DataType))
                {
                    var fieldDeclaration = SyntaxFactory.FieldDeclaration(
                        SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName(socket.DataType))
                            .WithVariables(SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(socket.TargetField))
                            ))
                    ).WithModifiers(SyntaxFactory.TokenList(
                        SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                        SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)
                    ));
                    classMembers.Add(fieldDeclaration);
                }
            }
        }

        // Constructor
        if (node.Sockets != null && node.Sockets.Count > 0)
        {
            var constructorParameters = new List<ParameterSyntax>();
            var constructorAssignments = new List<StatementSyntax>();

            foreach (var socket in node.Sockets)
            {
                if (IsValidName(socket.TargetField) && IsValidName(socket.DataType))
                {
                    var paramName = socket.TargetField.TrimStart('_');
                    constructorParameters.Add(SyntaxFactory.Parameter(SyntaxFactory.Identifier(paramName))
                        .WithType(SyntaxFactory.ParseTypeName(socket.DataType)));

                    constructorAssignments.Add(SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            SyntaxFactory.IdentifierName(socket.TargetField),
                            SyntaxFactory.IdentifierName(paramName)
                        )
                    ));
                }
            }

            var constructor = SyntaxFactory.ConstructorDeclaration(node.Name)
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(constructorParameters)))
                .WithBody(SyntaxFactory.Block(constructorAssignments));
            
            classMembers.Add(constructor);
        }

        // Methods
        if (node.Methods != null)
        {
            foreach (var method in node.Methods)
            {
                if (!IsValidName(method.Name)) continue;

                var methodAttributes = new List<AttributeListSyntax>();
                if (method.Annotations != null)
                {
                    foreach (var ann in method.Annotations)
                    {
                        if (AllowedAnnotations.Contains(ann))
                        {
                            methodAttributes.Add(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Attribute(SyntaxFactory.ParseName(ann))
                            )));
                        }
                    }
                }

                var parameters = new List<ParameterSyntax>();
                if (method.Parameters != null)
                {
                    foreach (var param in method.Parameters)
                    {
                        if (!IsValidName(param.Name) || !IsValidName(param.Type, allowGenerics: true)) continue;
                        parameters.Add(SyntaxFactory.Parameter(SyntaxFactory.Identifier(param.Name))
                            .WithType(SyntaxFactory.ParseTypeName(param.Type)));
                    }
                }

                var statements = new List<StatementSyntax>();
                if (method.Body != null)
                {
                    foreach (var stmt in method.Body)
                    {
                        if (stmt.Type == "ServiceCall" && !string.IsNullOrWhiteSpace(stmt.Service) && !string.IsNullOrWhiteSpace(stmt.Method))
                        {
                            var targetField = node.Sockets?.FirstOrDefault(s => s.DataType == stmt.Service)?.TargetField ?? $"_{stmt.Service.ToLowerInvariant()}";

                            statements.Add(SyntaxFactory.LocalDeclarationStatement(
                                SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                                    .WithVariables(SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("result"))
                                            .WithInitializer(SyntaxFactory.EqualsValueClause(
                                                SyntaxFactory.AwaitExpression(
                                                    SyntaxFactory.InvocationExpression(
                                                        SyntaxFactory.MemberAccessExpression(
                                                            SyntaxKind.SimpleMemberAccessExpression,
                                                            SyntaxFactory.IdentifierName(targetField),
                                                            SyntaxFactory.IdentifierName(stmt.Method)
                                                        )
                                                    )
                                                )
                                            ))
                                    ))
                            ));
                        }
                        else if (stmt.Type == "Return")
                        {
                            if (!string.IsNullOrWhiteSpace(stmt.Value))
                            {
                                statements.Add(SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName(stmt.Value)));
                            }
                            else
                            {
                                statements.Add(SyntaxFactory.ReturnStatement());
                            }
                        }
                    }
                }

                var methodModifiers = SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
                if (method.ReturnType?.Contains("Task") == true || method.Body?.Any(b => b.Type == "ServiceCall") == true)
                {
                    methodModifiers = methodModifiers.Add(SyntaxFactory.Token(SyntaxKind.AsyncKeyword));
                }

                var methodDeclaration = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName(method.ReturnType ?? "void"), method.Name)
                    .WithAttributeLists(SyntaxFactory.List(methodAttributes))
                    .WithModifiers(methodModifiers)
                    .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters)))
                    .WithBody(SyntaxFactory.Block(statements));

                classMembers.Add(methodDeclaration);
            }
        }

        var classAttributes = new List<AttributeListSyntax>();
        if (node.Annotations != null)
        {
            foreach (var ann in node.Annotations)
            {
                if (AllowedAnnotations.Contains(ann))
                {
                    classAttributes.Add(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Attribute(SyntaxFactory.ParseName(ann))
                    )));
                }
            }
        }

        var classDeclaration = SyntaxFactory.ClassDeclaration(node.Name)
            .WithAttributeLists(SyntaxFactory.List(classAttributes))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.SealedKeyword)))
            .WithBaseList(SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName($"I{node.Name}")))))
            .WithMembers(SyntaxFactory.List(classMembers));

        namespaceDeclarationClass = namespaceDeclarationClass.AddMembers(classDeclaration);

        // Add DI comment
        var diComment = SyntaxFactory.Comment($"// Register: builder.Services.Add{node.Lifetime ?? "Scoped"}<I{node.Name}, {node.Name}>();\n");
        namespaceDeclarationClass = namespaceDeclarationClass.WithLeadingTrivia(SyntaxFactory.TriviaList(diComment));

        var compilationUnitClass = SyntaxFactory.CompilationUnit()
            .WithUsings(SyntaxFactory.List(usingDirectives))
            .AddMembers(namespaceDeclarationClass)
            .NormalizeWhitespace();

        using var workspaceClass = new AdhocWorkspace();
        results[$"{node.Name}.cs"] = Formatter.Format(compilationUnitClass, workspaceClass).ToFullString();

        return results;
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
