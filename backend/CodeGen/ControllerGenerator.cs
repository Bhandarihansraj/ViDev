using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using ViDev.Api.CodeGen.Models;

namespace ViDev.Api.CodeGen;

/// <summary>
/// Generates C# Web API controllers from an AstControllerNode using Roslyn SyntaxFactory.
/// </summary>
public sealed class ControllerGenerator
{
    private static readonly HashSet<string> AllowedAnnotations = new(StringComparer.OrdinalIgnoreCase)
    {
        "ApiController", "Authorize", "AllowAnonymous", "JWT", "Route", "ValidateModel"
    };

    private static readonly Regex ValidNameRegex = new("^[A-Za-z][A-Za-z0-9_]*$", RegexOptions.Compiled);

    /// <summary>
    /// Generates the controller code as a formatted string.
    /// </summary>
    public string Generate(AstControllerNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (!IsValidName(node.Name))
        {
            throw new ArgumentException($"Invalid controller name: {node.Name}");
        }

        var usingDirectives = new[]
        {
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System")),
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Threading.Tasks")),
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Microsoft.AspNetCore.Mvc")),
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Microsoft.AspNetCore.Authorization"))
        };

        var namespaceDeclaration = SyntaxFactory.FileScopedNamespaceDeclaration(SyntaxFactory.ParseName("Generated.Controllers"));

        // Build class attributes
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
        
        // Add Route attribute
        if (!string.IsNullOrWhiteSpace(node.RoutePrefix))
        {
            classAttributes.Add(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Attribute(SyntaxFactory.ParseName("Route"))
                    .WithArgumentList(SyntaxFactory.AttributeArgumentList(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression(
                            SyntaxKind.StringLiteralExpression,
                            SyntaxFactory.Literal(node.RoutePrefix)
                        ))
                    )))
            )));
        }

        // Build class members (fields, constructor, methods)
        var classMembers = new List<MemberDeclarationSyntax>();

        // 1. Dependency Injection Fields
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

        // 2. Constructor
        if (node.Sockets != null && node.Sockets.Count > 0)
        {
            var constructorParameters = new List<ParameterSyntax>();
            var constructorAssignments = new List<StatementSyntax>();

            foreach (var socket in node.Sockets)
            {
                if (IsValidName(socket.TargetField) && IsValidName(socket.DataType))
                {
                    var paramName = socket.TargetField.TrimStart('_');
                    // Parameter: DataType paramName
                    constructorParameters.Add(SyntaxFactory.Parameter(SyntaxFactory.Identifier(paramName))
                        .WithType(SyntaxFactory.ParseTypeName(socket.DataType)));

                    // Assignment: this.targetField = paramName;
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

        // 3. Methods
        if (node.Methods != null)
        {
            foreach (var method in node.Methods)
            {
                if (!IsValidName(method.Name)) continue;

                var methodAttributes = new List<AttributeListSyntax>();

                // HTTP Verb Attribute
                if (!string.IsNullOrWhiteSpace(method.Verb))
                {
                    var httpVerbAttr = method.Verb.ToUpperInvariant() switch
                    {
                        "GET" => "HttpGet",
                        "POST" => "HttpPost",
                        "PUT" => "HttpPut",
                        "DELETE" => "HttpDelete",
                        "PATCH" => "HttpPatch",
                        _ => null
                    };

                    if (httpVerbAttr != null)
                    {
                        var attr = SyntaxFactory.Attribute(SyntaxFactory.ParseName(httpVerbAttr));
                        if (!string.IsNullOrWhiteSpace(method.Route))
                        {
                            attr = attr.WithArgumentList(SyntaxFactory.AttributeArgumentList(SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression(
                                    SyntaxKind.StringLiteralExpression,
                                    SyntaxFactory.Literal(method.Route)
                                ))
                            )));
                        }
                        methodAttributes.Add(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(attr)));
                    }
                }

                // Other method annotations
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

                // Parameters
                var parameters = new List<ParameterSyntax>();
                if (method.Parameters != null)
                {
                    foreach (var param in method.Parameters)
                    {
                        if (!IsValidName(param.Name) || !IsValidName(param.Type, allowGenerics: true)) continue;

                        var paramSyntax = SyntaxFactory.Parameter(SyntaxFactory.Identifier(param.Name))
                            .WithType(SyntaxFactory.ParseTypeName(param.Type));

                        var paramAttributes = new List<AttributeListSyntax>();
                        if (param.FromBody)
                            paramAttributes.Add(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Attribute(SyntaxFactory.ParseName("FromBody")))));
                        if (param.FromRoute)
                            paramAttributes.Add(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Attribute(SyntaxFactory.ParseName("FromRoute")))));
                        if (param.FromQuery)
                            paramAttributes.Add(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Attribute(SyntaxFactory.ParseName("FromQuery")))));

                        if (paramAttributes.Count > 0)
                        {
                            paramSyntax = paramSyntax.WithAttributeLists(SyntaxFactory.List(paramAttributes));
                        }
                        parameters.Add(paramSyntax);
                    }
                }

                // Body statements
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
                                statements.Add(SyntaxFactory.ReturnStatement(
                                    SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName("Ok"))
                                        .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(
                                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName(stmt.Value))
                                        )))
                                ));
                            }
                            else
                            {
                                statements.Add(SyntaxFactory.ReturnStatement(SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName("Ok"))));
                            }
                        }
                    }
                }

                var methodModifiers = SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
                if (method.ReturnType?.Contains("Task") == true || method.Body?.Any(b => b.Type == "ServiceCall") == true)
                {
                    methodModifiers = methodModifiers.Add(SyntaxFactory.Token(SyntaxKind.AsyncKeyword));
                }

                var methodDeclaration = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName(method.ReturnType ?? "IActionResult"), method.Name)
                    .WithAttributeLists(SyntaxFactory.List(methodAttributes))
                    .WithModifiers(methodModifiers)
                    .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters)))
                    .WithBody(SyntaxFactory.Block(statements));

                classMembers.Add(methodDeclaration);
            }
        }

        var classDeclaration = SyntaxFactory.ClassDeclaration(node.Name)
            .WithAttributeLists(SyntaxFactory.List(classAttributes))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.SealedKeyword)))
            .WithBaseList(SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("ControllerBase")))))
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
