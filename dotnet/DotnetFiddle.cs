
using System.Reflection;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using System.Text;

public partial class DotnetFiddle
{
    
    HttpClient _httpClient = new HttpClient();

    Dictionary<string, MetadataReference> MetadataReferenceCache = new Dictionary<string, MetadataReference>();
    async Task<MetadataReference?> GetAssemblyMetadataReference(Assembly assembly)
    {
        //_httpClient.BaseAddress = new Uri("https://probable-halibut-9764xwxqpjpcxpv9-3000.app.github.dev/AppBundle");
        MetadataReference? ret = null;
        var assemblyName = assembly.GetName().Name;
        if (MetadataReferenceCache.TryGetValue(assemblyName, out ret)) return ret;
        var assemblyUrl = $"https://probable-halibut-9764xwxqpjpcxpv9-3000.app.github.dev/AppBundle/_framework/{assemblyName}.dll";
        try
        {
            var tmp = await _httpClient.GetAsync(assemblyUrl);
            if (tmp.IsSuccessStatusCode)
            {
                var bytes = await tmp.Content.ReadAsByteArrayAsync();
                ret = MetadataReference.CreateFromImage(bytes);
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"metadataReference not loaded: {assembly} {ex.Message}");
        }
        if (ret == null){
            Console.Error.WriteLine("ReferenceMetadata not found. If using .Net 8, <WasmEnableWebcil>false</WasmEnableWebcil> must be set in the project .csproj file.");
            return null;
            //throw new Exception("ReferenceMetadata not found. If using .Net 8, <WasmEnableWebcil>false</WasmEnableWebcil> must be set in the project .csproj file.");
        }
        MetadataReferenceCache[assemblyName] = ret;
        return ret;
    }

    public async Task<Assembly> CompileToDLLAssembly(string sourceCode, string assemblyName = "", bool release = true, SourceCodeKind sourceCodeKind = SourceCodeKind.Regular)
    {
        if (string.IsNullOrEmpty(assemblyName)) assemblyName = Path.GetRandomFileName();
        var codeString = SourceText.From(sourceCode);
        var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp11).WithKind(sourceCodeKind);
        var parsedSyntaxTree = SyntaxFactory.ParseSyntaxTree(codeString, options);
        //Console.WriteLine("sourceCode: " + sourceCode);
        //var appAssemblies = Assembly.GetEntryAssembly()!.GetReferencedAssemblies().Select(o => Assembly.Load(o)).ToList();
        var appAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(x => !x.IsDynamic).ToList();
        //Console.WriteLine("asm: " + appAssemblies.Count);
        appAssemblies.Add(typeof(object).Assembly);
        var references = new List<MetadataReference>();
        foreach (var assembly in appAssemblies)
        {
            var metadataReference = await GetAssemblyMetadataReference(assembly);
            if (metadataReference is not null)
            {
                references.Add(metadataReference);
            }
        }
        CSharpCompilation compilation;
        if (sourceCodeKind == SourceCodeKind.Script)
        {
            compilation = CSharpCompilation.CreateScriptCompilation(
            assemblyName,
            syntaxTree: parsedSyntaxTree,
            references: references,
            options: new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    concurrentBuild: false,
                    optimizationLevel: release ? OptimizationLevel.Release : OptimizationLevel.Debug
                )
            );
        }
        else
        {
            compilation = CSharpCompilation.Create(
            assemblyName,
            syntaxTrees: new[] { parsedSyntaxTree },
            references: references,
            options: new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    concurrentBuild: false,
                    optimizationLevel: release ? OptimizationLevel.Release : OptimizationLevel.Debug
                )
            );
        }
        using (var ms = new MemoryStream())
        {
            EmitResult result = compilation.Emit(ms);
            if (!result.Success)
            {
                var errors = new StringBuilder();
                IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);
                foreach (Diagnostic diagnostic in failures)
                {
                    var startLinePos = diagnostic.Location.GetLineSpan().StartLinePosition;
                    var err = $"Line: {startLinePos.Line} Col:{startLinePos.Character} Code: {diagnostic.Id} Message: {diagnostic.GetMessage()}";
                    errors.AppendLine(err);
                    Console.Error.WriteLine("Diag: " + err);
                }
                throw new Exception(errors.ToString());
            }
            else
            {
                ms.Seek(0, SeekOrigin.Begin);
                var assembly = Assembly.Load(ms.ToArray());
                return assembly;
            }
        }
    }

    [JSExport]
    public static async Task RunCode(string code)
    {
        var textArea = @$"using System;

namespace RoslynCompileSample
{{
    public class Writer
    {{
        public string Write()
        {{
            var ret = string.Empty;
            {code}
            return ret;
        }}
    }}
}}
        ";

        try
        {
            var fiddle = new DotnetFiddle();
            // compile script to in memory dll assembly
            var scriptAssembly = await fiddle.CompileToDLLAssembly(textArea, release: true);
            // use reflection to load our type (a shared project with interfaces would help here ... )
            Type type = scriptAssembly.GetType("RoslynCompileSample.Writer");
            // create an instance
            object obj = Activator.CreateInstance(type);
            // call our test function
            Console.WriteLine((string)type.InvokeMember("Write", BindingFlags.Default | BindingFlags.InvokeMethod, null, obj, new object[] { }));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
        }
    }
    /*
    Dictionary<string, MetadataReference> MetadataReferenceCache = new Dictionary<string, MetadataReference>();
    async Task<MetadataReference> GetAssemblyMetadataReference(Assembly assembly)
    {
        MetadataReference? ret = null;
        var assemblyName = assembly.GetName().Name;
        if (MetadataReferenceCache.TryGetValue(assemblyName, out ret)) return ret;
        var assemblyUrl = $"./_framework/{assemblyName}.dll";
        try
        {
            var tmp = await _httpClient.GetAsync(assemblyUrl);
            if (tmp.IsSuccessStatusCode)
            {
                var bytes = await tmp.Content.ReadAsByteArrayAsync();
                ret = MetadataReference.CreateFromImage(bytes);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"metadataReference not loaded: {assembly} {ex.Message}");
        }
        if (ret == null) throw new Exception("ReferenceMetadata nto found. If using .Net 8, <WasmEnableWebcil>false</WasmEnableWebcil> must be set in the project .csproj file.");
        MetadataReferenceCache[assemblyName] = ret;
        return ret;
    }
    
    [JSExport]
    internal static async Task RunCode(string code)
    {
        var codeString = SourceText.From(code);
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(codeString);
        // string assemblyName = "MyAssembly";
        // MetadataReference[] references = { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) };

        // CSharpCompilation compilation = CSharpCompilation.Create(assemblyName)
        //     .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
        //     //.AddReferences(references)
        //     .AddSyntaxTrees(syntaxTree);

        var refs = AppDomain.CurrentDomain.GetAssemblies();
        Console.WriteLine(string.Join(", ", refs.Select(r => r.FullName)));
        var client = new HttpClient 
        {
                BaseAddress = new Uri("https://probable-halibut-9764xwxqpjpcxpv9-3000.app.github.dev/AppBundle")
        };

        var references = new List<MetadataReference>();

        foreach(var reference in refs.Where(x => !x.IsDynamic && !string.IsNullOrWhiteSpace(x.Location)))
        {
            var stream = await client.GetStreamAsync($"_framework/{reference.Location}");
            references.Add(MetadataReference.CreateFromStream(stream));
            Console.WriteLine(reference.FullName);
        }
        
        var scriptCompilation = CSharpCompilation.CreateScriptCompilation(
                Path.GetRandomFileName(), 
                CSharpSyntaxTree.ParseText(codeString, CSharpParseOptions.Default.WithKind(SourceCodeKind.Script).WithLanguageVersion(LanguageVersion.Preview)),
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, concurrentBuild: false, usings: new[] 
                { 
                    "System",
                    "System.IO",
                    "System.Collections.Generic",
                    "System.Console",
                    "System.Diagnostics",
                    "System.Dynamic",
                    "System.Linq",
                    "System.Linq.Expressions",
                    "System.Net.Http",
                    "System.Text",
                    "System.Threading.Tasks" 
                })
            );

        using (var ms = new System.IO.MemoryStream())
        {
            EmitResult result = scriptCompilation.Emit(ms);

            if (!result.Success)
            {
                foreach (var diagnostic in result.Diagnostics)
                {
                    Console.WriteLine("Diag:" + diagnostic.GetMessage());
                }
            }
            else
            {
                ms.Seek(0, System.IO.SeekOrigin.Begin);
                Assembly assembly = Assembly.Load(ms.ToArray());
                Type type = assembly.GetType("MyClass");
                object instance = Activator.CreateInstance(type);
                MethodInfo method = type.GetMethod("MyFunction");
                method.Invoke(instance, null);
            }
        }
    }*/
}