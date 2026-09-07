using CustomAvatar.Tasks.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace CustomAvatar.Tasks;

internal class ClassGenerator
{
    public record struct TargetType(string Name, string AssemblyName);

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        IndentSize = 4,
    };

    private readonly BaseAssemblyResolver assemblyResolver;

    private readonly ModuleDefinition mainModule;

    private readonly TypeDefinition nonSerializedAttribute;

    private readonly TypeDefinition serializeFieldAttribute;

    public ClassGenerator(string assemblyFile, IEnumerable<string> searchDirectories)
    {
        assemblyResolver = new DefaultAssemblyResolver();

        foreach (string folder in searchDirectories)
        {
            assemblyResolver.AddSearchDirectory(folder);
        }

        mainModule = AssemblyDefinition.ReadAssembly(assemblyFile, new ReaderParameters()
        {
            AssemblyResolver = assemblyResolver,
        }).MainModule;

        nonSerializedAttribute = mainModule.ImportReference(typeof(NonSerializedAttribute)).Resolve();
        serializeFieldAttribute = assemblyResolver.Resolve(new AssemblyNameReference("UnityEngine.CoreModule", new Version())).MainModule.GetType("UnityEngine.SerializeField");
    }

    public void Generate(IEnumerable<TargetType> types, string outputDirectory)
    {
        RemoveSourceFiles(outputDirectory);
        WriteSourceFiles(types, outputDirectory);
        RemoveEmptyDirectories(outputDirectory);
    }

    private static TypeSyntax ConvertTypeReference(TypeReference type)
    {
        if (type.IsArray)
        {
            return ArrayType(ConvertTypeReference(type.GetElementType()), List([ArrayRankSpecifier(SeparatedList<ExpressionSyntax>())]));
        }
        else
        {
            return IdentifierName(type.FullName);
        }
    }

    private static bool IsBuiltInReference(AssemblyNameReference assemblyReferenceName)
    {
        string name = assemblyReferenceName.Name;
        return name.StartsWith("Unity") || name.StartsWith("System") || name is "mscorlib" or "netstandard";
    }

    private void RemoveSourceFiles(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return;
        }

        foreach (string file in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
        {
            File.Delete(file);
        }
    }

    private void WriteSourceFiles(IEnumerable<TargetType> types, string outputDirectory)
    {
        TypeCollection typeCollection = new();

        foreach (TargetType type in types)
        {
            typeCollection.Push(ResolveType(type));
        }

        while (typeCollection.HasItems)
        {
            WriteSourceFile(typeCollection, typeCollection.Pop(), outputDirectory);
        }

        foreach (AssemblyDefinition assembly in typeCollection.Assemblies)
        {
            WriteAssemblyDefinition(assembly, typeCollection.GetReferences(assembly), outputDirectory);
        }
    }

    private TypeDefinition ResolveType(TargetType type)
    {
        ModuleDefinition module;

        if (!string.IsNullOrEmpty(type.AssemblyName))
        {
            module = assemblyResolver.Resolve(AssemblyNameReference.Parse(type.AssemblyName)).MainModule;

        }
        else
        {
            module = mainModule;
        }

        TypeDefinition typeDefinition = module.GetType(type.Name);

        if (type == null)
        {
            throw new Exception($"Failed to resolve '{type.Name}'");
        }

        return typeDefinition;
    }

    private void WriteSourceFile(TypeCollection typeCollection, TypeDefinition typeDefinition, string outputDirectory)
    {
        AssemblyDefinition assembly = typeDefinition.Module.Assembly;

        ClassDeclarationSyntax klass = ClassDeclaration(typeDefinition.Name).WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)));

        TypeDefinition baseType = typeDefinition.BaseType.Resolve();

        if (baseType != null)
        {
            klass = klass.WithBaseList(BaseList(SingletonSeparatedList<BaseTypeSyntax>(SimpleBaseType(ConvertTypeReference(baseType)))));

            typeCollection.Push(assembly, baseType);
        }

        foreach (PropertyDefinition property in typeDefinition.Properties.Where(IsSerializableProperty))
        {
            TypeReference propertyType = property.PropertyType;

            klass = klass.AddMembers(
                PropertyDeclaration(ConvertTypeReference(propertyType), Identifier(property.Name))
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddAccessorListAccessors(
                    AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                    AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(Token(SyntaxKind.SemicolonToken)))
                .AddAttributeLists(AttributeList(SingletonSeparatedList(Attribute(IdentifierName("UnityEngine.SerializeField"))))
                    .WithTarget(AttributeTargetSpecifier(Token(SyntaxKind.FieldKeyword)))));

            typeCollection.Push(assembly, propertyType.Resolve());
        }

        foreach (MethodDefinition method in typeDefinition.Methods.Where(IsUnityLifecycleMethod))
        {
            klass = klass.AddMembers(
                MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), Identifier(method.Name))
                .WithBody(Block()));
        }

        foreach (FieldDefinition field in typeDefinition.Fields.Where(IsSerializableField))
        {
            TypeReference fieldType = field.FieldType;

            klass = klass.AddMembers(
                FieldDeclaration(
                    VariableDeclaration(ConvertTypeReference(fieldType))
                        .AddVariables(VariableDeclarator(Identifier(field.Name))))
                .AddModifiers(Token(SyntaxKind.PublicKeyword)));

            typeCollection.Push(assembly, fieldType.Resolve());
        }

        MemberDeclarationSyntax member;

        if (!string.IsNullOrEmpty(typeDefinition.Namespace))
        {
            member = NamespaceDeclaration(IdentifierName(typeDefinition.Namespace)).AddMembers(klass);
        }
        else
        {
            member = klass;
        }

        CompilationUnitSyntax compilationUnit = CompilationUnit().AddMembers(member).WithLeadingTrivia(Comment("// <auto-generated />")).NormalizeWhitespace();

        string path = Path.Combine(outputDirectory, assembly.Name.Name, typeDefinition.FullName.Replace('.', Path.DirectorySeparatorChar) + ".cs");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, compilationUnit.ToFullString(), Encoding.UTF8);
    }

    private bool IsSerializableField(FieldDefinition field)
    {
        if (field.IsStatic)
        {
            return false;
        }

        if (field.Name.EndsWith("k__BackingField"))
        {
            return false;
        }

        if (field.IsPublic && !field.CustomAttributes.Any(a => a.AttributeType.Resolve() == nonSerializedAttribute))
        {
            return true;
        }

        if (!field.IsPublic && field.CustomAttributes.Any(a => a.AttributeType.Resolve() == serializeFieldAttribute))
        {
            return true;
        }

        return false;
    }

    private bool IsSerializableProperty(PropertyDefinition property)
    {
        FieldDefinition field = property.DeclaringType.Fields.SingleOrDefault(f => f.Name == $"<{property.Name}>k__BackingField");

        if (field == null)
        {
            return false;
        }

        return field.CustomAttributes.Any(a => a.AttributeType.Resolve() == serializeFieldAttribute);
    }

    private bool IsUnityLifecycleMethod(MethodDefinition method)
    {
        return method.Name is "Awake" or "OnEnable" or "Reset" or "Start" or "FixedUpdate" or "Update" or "LateUpdate" or "OnDisable" or "OnDestroy";
    }

    private void WriteAssemblyDefinition(AssemblyDefinition assembly, IEnumerable<AssemblyDefinition> references, string outputDirectory)
    {
        string assemblyName = assembly.Name.Name;
        string path = Path.Combine(outputDirectory, assemblyName, $"{assemblyName}.asmdef");

        File.WriteAllText(path, JsonSerializer.Serialize(
            new UnityAssemblyDefinition()
            {
                Name = assemblyName,
                References = references.Select(a => a.Name.Name),
            },
            JsonSerializerOptions));
    }

    private void RemoveEmptyDirectories(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return;
        }

        foreach (string subdirectory in Directory.EnumerateDirectories(directory))
        {
            RemoveEmptyDirectories(subdirectory);
        }

        if (!Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories).Any())
        {
            foreach (string asmDef in Directory.EnumerateFiles(directory, "*.asmdef"))
            {
                File.Delete(asmDef);
            }
        }

        foreach (string meta in Directory.EnumerateFiles(directory, "*.meta"))
        {
            string path = meta[0..^5];
            if (!File.Exists(path) && !Directory.Exists(path))
            {
                File.Delete(meta);
            }
        }

        if (!Directory.EnumerateFileSystemEntries(directory).Any())
        {
            Directory.Delete(directory);
            File.Delete(directory + ".meta");
        }
    }

    private class TypeCollection
    {
        private readonly Stack<TypeDefinition> stack = [];
        private readonly HashSet<TypeDefinition> encountered = [];
        private readonly HashSet<AssemblyDefinition> assemblies = [];
        private readonly Dictionary<AssemblyDefinition, HashSet<AssemblyDefinition>> referenceMap = [];

        public bool HasItems => stack.Count > 0;

        public IEnumerable<AssemblyDefinition> Assemblies => assemblies;

        public IEnumerable<AssemblyDefinition> GetReferences(AssemblyDefinition assembly) => referenceMap.TryGetValue(assembly, out HashSet<AssemblyDefinition> references) ? references : Enumerable.Empty<AssemblyDefinition>();

        public void Push(AssemblyDefinition assembly, TypeDefinition type)
        {
            RegisterReference(assembly, type.Module.Assembly);
            Push(type);
        }

        public void Push(TypeDefinition type)
        {
            if (encountered.Contains(type))
            {
                return;
            }

            AssemblyDefinition assembly = type.Module.Assembly;

            if (IsBuiltInReference(assembly.Name))
            {
                return;
            }

            encountered.Add(type);
            assemblies.Add(assembly);
            stack.Push(type);
        }

        public TypeDefinition Pop() => stack.Pop();

        private void RegisterReference(AssemblyDefinition assembly, AssemblyDefinition reference)
        {
            if (assembly == reference)
            {
                return;
            }

            if (IsBuiltInReference(reference.Name))
            {
                return;
            }

            if (!referenceMap.TryGetValue(assembly, out HashSet<AssemblyDefinition> references))
            {
                references = new HashSet<AssemblyDefinition>();
                referenceMap.Add(assembly, references);
            }

            references.Add(reference);
        }
    }
}
