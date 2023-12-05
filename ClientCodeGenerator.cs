using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System.IO;
using System.Linq;
using System.Reflection;

namespace RoslynCompiler.csharp
{
    public static class ClientCodeGenerator
    {
        public static void GenerateAndSaveClientExecutable(string ipAddress, int port, string outputPath)
        {
            string clientCode = GetClientCodeTemplate().Replace("{{IP_ADDRESS}}", ipAddress)
                                                       .Replace("{{PORT}}", port.ToString());

            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(clientCode);

            var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);
            string coreLibPath = typeof(int).Assembly.Location;

            CSharpCompilation compilation = CSharpCompilation.Create("CtepiaClient")
                .WithOptions(new CSharpCompilationOptions(OutputKind.ConsoleApplication))
                .AddReferences(
                    MetadataReference.CreateFromFile(coreLibPath),
                    MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Runtime.dll")), // For .NET 5+
                    MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Console.dll")), // For Console
                    MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Net.Sockets.dll")), // For TcpClient
                    MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location), // Base reference
                    MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Net.Primitives.dll")), // For SocketException
                    MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "Microsoft.Win32.Primitives.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "mscorlib.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Core.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.dll")))
                                
                .AddSyntaxTrees(syntaxTree);

            EmitResult result = compilation.Emit(outputPath);

            if (!result.Success)
            {
                var errorMessages = result.Diagnostics
                                          .Where(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error)
                                          .Select(diagnostic => $"{diagnostic.Id}: {diagnostic.GetMessage()}")
                                          .ToList();

                // Log the error messages or throw an exception
                foreach (var errorMessage in errorMessages)
                {
                    Console.WriteLine(errorMessage);
                }
                throw new InvalidOperationException("Compilation failed: " + string.Join("\n", errorMessages));
            }
        }

        private static string GetClientCodeTemplate()
        {
            return @"
            using System;
            using System.Net.Sockets;
            using System.Threading;
            using System.Linq;

            class Program
            {
                static void Main(string[] args)
                {
                    while (true)
                    {
                        try
                        {
                            Console.WriteLine(""Attempting to connect..."");
                            using (var client = new TcpClient(""{{IP_ADDRESS}}"", {{PORT}}))
                            {
                                Console.WriteLine(""Connected to server. Press any key to exit."");
                                Console.ReadKey();
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(""Error: "" + ex.Message);
                            Console.WriteLine(""Retrying in 5 seconds..."");
                            Thread.Sleep(5000);
                        }
                    }
                    Console.WriteLine(""Press any key to close."");
                    Console.ReadKey();
                }
            }";
        }
    }
}