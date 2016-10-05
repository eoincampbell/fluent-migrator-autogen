using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using CommandLine;
using CommandLine.Text;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Announcers;
using FluentMigrator.Runner.Initialization;

namespace FluentMigratorRD
{
    public enum Mode
    {
        GeneratePackage,
        ExecutePackage
    }

    public class RunOptions
    {
        [Option('m', "mode", Required = true, HelpText = "Specify 'GeneratePackage' or 'ExecutePackage'")]
        public Mode Mode { get; set; }

        [Option('s', "scriptDirectory", Required = false, HelpText = "Specify the directory containing your scripts")]
        public string ScriptDirectory { get; set; }

        [Option('o', "outputAssembly", Required = false, HelpText = "Specify the path of the assembly to generate")]
        public string OutputAssembly { get; set; }

        [Option('i', "inputAssembly", Required = false, HelpText = "Specify the path of the assembly to execute migrations from.")]
        public string InputAssembly { get; set; }

        [Option('c', "conn", Required = false, HelpText = "Specify the database connection string.")]
        public string Connection { get; set; }

        [ParserState]
        public IParserState LastParserState { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this, (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }

    public class MigrationExecutor
    {
        private RunOptions _options;
        public MigrationExecutor(RunOptions options)
        {
            _options = options;
        }

        public void Run()
        {
            var announcer = new TextWriterAnnouncer(s => Console.WriteLine(s));
            var assembly = Assembly.LoadFile(_options.InputAssembly);


            var migrationContext = new RunnerContext(announcer)
            {
                Namespace = "AutoGen"
            };

            var options = new FluentMigrator.Runner.Processors.ProcessorOptions { PreviewOnly = false, Timeout = 60 };
            var factory =
                new FluentMigrator.Runner.Processors.SqlServer.SqlServer2008ProcessorFactory();

            using (var processor = factory.Create(_options.Connection, announcer, options))
            {
                var runner = new MigrationRunner(assembly, migrationContext, processor);
                runner.MigrateUp(true);
            }

        }
    }

    public class PackageGenerator
    {
        private RunOptions _options;

        public PackageGenerator(RunOptions options)
        {
            _options = options;
        }

        public void Run()
        {
            var tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var tempDir = Directory.CreateDirectory(tempPath);
            var files = new List<string>();
            var codeProvider = new CSharpCodeProvider();
            var parameters = new CompilerParameters();

            parameters.GenerateExecutable = false;
            parameters.OutputAssembly = _options.OutputAssembly;
            parameters.ReferencedAssemblies.Add("C:\\work\\Code\\FluentMigratorRD\\packages\\FluentMigrator.1.6.2\\lib\\40\\FluentMigrator.dll");

            foreach (var s in Directory.GetFiles(_options.ScriptDirectory, "*.sql"))
            {
                //Verify script adheres to YYYYMMDDHHmm.sql

                parameters.EmbeddedResources.Add(s);
                var fileinfo = new FileInfo(s);
                File.Copy(s, Path.Combine(tempDir.FullName, fileinfo.Name), true);
                var csFilePath = GenCodeFile(fileinfo, tempDir.FullName);
                files.Add(csFilePath);
            }

            CompilerResults results = codeProvider.CompileAssemblyFromFile(parameters, files.ToArray());
            Console.WriteLine(tempPath);

        }

        private string GenCodeFile(FileInfo sqlFile, string tempDir)
        {
            var filename = sqlFile.Name.Replace(".sql", "");

            string filecontent = $@"
using System;
using FluentMigrator;
namespace AutoGen {{
    [Migration({filename})]
    public class M{filename} : Migration {{
        public override void Up() {{ Execute.EmbeddedScript(""{filename}.sql""); }}
        public override void Down() {{ }}
    }}
}}";
            var csFilePath = Path.Combine(tempDir, $"{filename}.cs");
            File.WriteAllText(csFilePath, filecontent);

            return csFilePath;
        }
    }

    public class Program
    {
        static void Main(string[] args)
        {
            var options = new RunOptions();

            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {
                if (options.Mode == Mode.GeneratePackage)
                {
                    new PackageGenerator(options).Run();
                }
                else
                {
                    new MigrationExecutor(options).Run();
                }
            }
            else
            {
                Console.WriteLine("NOPE 3");
            }
        }
    }
}

