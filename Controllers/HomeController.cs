using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace OnlineCSharpCompiler.Controllers
{
    [Route("compiler")]
    public class CompilerController : Controller
    {
        [HttpPost("compileToDll")]
        public IActionResult CompileToDll([FromBody] string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return BadRequest("No code submitted for compilation.");
            }

            // Create a syntax tree from the provided code
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var assemblyName = Path.GetRandomFileName();

            // Reference necessary assemblies
            var references = new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Console).Assembly.Location),
                // Add more references if needed
                MetadataReference.CreateFromFile(Path.Combine(AppContext.BaseDirectory, "AElf.Sdk.CSharp.dll")),
                MetadataReference.CreateFromFile(Path.Combine(AppContext.BaseDirectory, "AElf.Tools.dll"))

            };

            // Create a compilation object
            var compilation = CSharpCompilation.Create(
                assemblyName,
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            // Emit the assembly to a memory stream
            using var ms = new MemoryStream();
            var result = compilation.Emit(ms);

            if (!result.Success)
            {
                // Retrieve diagnostics
                var errors = string.Join("\n", result.Diagnostics);
                return BadRequest($"Compilation failed:\n{errors}");
            }

            // Reset the memory stream position
            ms.Seek(0, SeekOrigin.Begin);

            // Return the compiled DLL as a file download
            return File(ms.ToArray(), "application/octet-stream", $"{assemblyName}.dll");
        }
    }
}