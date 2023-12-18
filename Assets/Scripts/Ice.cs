using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CSharp;
using System.CodeDom;
using System.Reflection;
using System.Text;
using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using System.Runtime.Loader;
using Basic.Reference.Assemblies;
using System.Linq.Expressions;

class Ice
{
    void Start()
    {
        
    }
    public interface ITask
    {
        void CanRun<T>(Expression<Func<T, bool>> predicate);
        void Run();
    }
    public static void Execute(string source)
    {
        var syntaxTree = SyntaxFactory.ParseSyntaxTree(source);
        var compilation = CSharpCompilation.Create(assemblyName: Path.GetRandomFileName())            
            .WithReferenceAssemblies(ReferenceAssemblyKind.NetStandard20)
            .AddReferences(
                MetadataReference.CreateFromFile(typeof(ITask).Assembly.Location))
            .AddReferences(ReferenceAssemblies.NetStandard20)
            .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddSyntaxTrees(syntaxTree);

        using (var ms = new MemoryStream())
        {
            var result = compilation.Emit(ms);
            if (!result.Success)
            {
                throw new Exception(result.ToString()); //CRASH
            }

            ms.Seek(0, SeekOrigin.Begin);
            var assembly = Assembly.Load(ms.ToArray());

            try
            {
                var types = assembly.GetTypes();
            }
            catch (Exception ex)
            {
                throw; //CRASH
            }

            dynamic task = assembly.CreateInstance("Consumer.MyTask");
            task.Run();
        }
    }
}

class Compiler
{
    public byte[] Compile(string filepath)
    {
        var sourceCode = File.ReadAllText(filepath);

        using (var peStream = new MemoryStream())
        {
            var result = GenerateCode(sourceCode).Emit(peStream);

            if (!result.Success)
            {
                Console.WriteLine("Compilation done with error.");

                var failures = result.Diagnostics.Where(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);

                foreach (var diagnostic in failures)
                {
                    Console.Error.WriteLine("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
                }

                return null;
            }

            Console.WriteLine("Compilation done without any error.");

            peStream.Seek(0, SeekOrigin.Begin);

            return peStream.ToArray();
        }
    }

    private static CSharpCompilation GenerateCode(string sourceCode)
    {
        var codeString = SourceText.From(sourceCode);
        var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7_3);

        var parsedSyntaxTree = SyntaxFactory.ParseSyntaxTree(codeString, options);

        var references = new MetadataReference[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Runtime.AssemblyTargetedPatchBandAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo).Assembly.Location),
            // Todo : to load current dll in the compilation context
            MetadataReference.CreateFromFile(typeof(Family.Startup).Assembly.Location),
        };

        return CSharpCompilation.Create("Hello.dll",
            new[] { parsedSyntaxTree }, 
            references: references, 
            options: new CSharpCompilationOptions(OutputKind.ConsoleApplication, 
                optimizationLevel: OptimizationLevel.Release,
                assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default));
    }
}