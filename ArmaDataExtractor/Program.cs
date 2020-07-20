using CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BIS.PBO;

namespace ArmaDataExtractor
{
    class Options
    {
        [Value(0, Required = true, MetaName = "SourceFolder", HelpText = "Source folder with PBOs")]
        public string SourceFolder { get; set; }

        [Value(1, Required = true, MetaName = "OutputFolder", HelpText = "Output folder")]
        public string OutputFolder { get; set; }

        [Option("ignore-prefix", Separator = ';', HelpText = "PBO prefixes to ignore")]
        public IEnumerable<string> IgnorePrefixes { get; set; }

        [Option("ignore-dubbing", HelpText = "Ignore Arma 3 dubbing files")]
        public bool IgnoreDubbing { get; set; }

        [Option("ignore-map-layers", HelpText = "Ignore Arma 3 map layers files")]
        public bool IgnoreMapLayers { get; set; }

        [Option("ignore-missions", HelpText = "Ignore Arma 3 mission files")]
        public bool IgnoreMissions { get; set; }

        [Option("minify", HelpText = "Write empty files for large binary files")]
        public bool Minify { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(RunOptionsAndReturnExitCode)
                .WithNotParsed(HandleParseError);
        }

        private static void HandleParseError(IEnumerable<Error> errs)
        {
            Environment.Exit(1);
        }

        private static void RunOptionsAndReturnExitCode(Options opts)
        {
            var ignorePrefixes = GenerateIgnorePrefixes(opts);

            var pboFiles = Directory.GetFiles(opts.SourceFolder, "*.pbo", SearchOption.AllDirectories);

            foreach (var pboFile in pboFiles)
            {
                ExtractPbo(pboFile, opts.OutputFolder, ignorePrefixes, opts.Minify);
            }
        }

        private static IEnumerable<string> GenerateIgnorePrefixes(Options opts)
        {
            var ignorePrefixes = opts.IgnorePrefixes;

            if (opts.IgnoreDubbing)
            {
                ignorePrefixes = ignorePrefixes.Append("a3\\dubbing");
            }

            if (opts.IgnoreMapLayers)
            {
                ignorePrefixes = ignorePrefixes.Concat(new[]
                {
                    "a3\\map_altis\\data\\layers",
                    "a3\\map_malden\\data\\layers",
                    "a3\\map_stratis\\data\\layers",
                    "a3\\map_tanoabuka\\data\\layers",
                    "a3\\map_vr\\data\\layers"
                });
            }

            if (opts.IgnoreMissions)
            {
                ignorePrefixes = ignorePrefixes.Append("a3\\missions");
            }

            return ignorePrefixes;
        }

        private static void ExtractPbo(string pboFile, string outputFolder, IEnumerable<string> ignorePrefixes, bool minify)
        {
            var pboArchive = new PBO(pboFile, true);

            if (pboArchive.Prefix == null)
            {
                Console.WriteLine($"{pboArchive.FileName} has no prefix");
                return;
            }

            foreach (var ignorePrefix in ignorePrefixes)
            {
                if (pboArchive.Prefix.Contains(ignorePrefix))
                {
                    return;
                }
            }

            Console.WriteLine($"Extracting {pboArchive.FileName}");

            var pboDirectoryPath = Path.Join(outputFolder, ConvertPathSeparator(pboArchive.Prefix));

            if (!minify)
            {
                pboArchive.ExtractFiles(pboArchive.FileEntries, outputFolder);
                return;
            }

            var whitelistedEntries = pboArchive.FileEntries.Where(IsWhitelistedFile);
            var blacklistedEntries = pboArchive.FileEntries.Except(whitelistedEntries);
            pboArchive.ExtractFiles(whitelistedEntries, pboDirectoryPath);

            foreach (var blacklistedEntry in blacklistedEntries)
            {
                var filename = Path.Join(pboDirectoryPath, ConvertPathSeparator(blacklistedEntry.FileName));
                Directory.CreateDirectory(Path.GetDirectoryName(filename));
                File.Create(filename).Dispose();
            }
        }

        private static bool IsWhitelistedFile(FileEntry fileEntry)
        {
            var blacklistedFileExtensions = new string[]
            {
                ".fsm",
                ".fxy",
                ".jpg",
                ".p3d",
                ".rtm",
                ".shdc",
                ".wrp",
                ".wss",
                ".xml",
            };

            return blacklistedFileExtensions.All(fileExtension => !fileEntry.FileName.EndsWith(fileExtension));
        }

        private static string ConvertPathSeparator(string path)
        {
            return path.Replace('\\', Path.DirectorySeparatorChar);
        }
    }
}
