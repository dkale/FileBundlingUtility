using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FileBundlingUtility
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("** FILE BUNDLING UTILITY **");

            var userOption = DisplayMenu();
            if (userOption != 1 && userOption != 2 && userOption != 3)
            {
                return;
            }

            if (userOption == 1)
            {
                PerformFileBundling();
            }
            else if (userOption == 2)
            {
                PerformFileUnBundling();
            }
            else if (userOption == 3)
            {
                PerformFileSplit();
            }
            else if (userOption == 4)
            {
                //MergeFiles();
            }
        }

        public static void BundleFiles(string sourceDirectoryPath, string outputPath, string encryptionKey, int numberOfBundlesToCreate)
        {
            var includedFileTypeExtensions = new string[]
            {
                ".html", ".ts", ".css", ".scss", ".cs", ".csproj", ".sqlproj",
                ".config", ".config_DEV", ".config_STG", ".config_QA1", ".config_PRD", ".sln", ".sql", ".nswag", ".ico", ".js",
                ".user", ".json", ".npmrc", ".gitignore", ".editorconfig", ".targets", ".lock",
                ".xslt"
            };
            var excludedFolders = new string[] { @"\obj\", @"\bin\", @"\node_modules\", @"\.vs\", @"\dist\", @"\.git\" };
            var namedFiles = new string[] { "karma.conf.js", "protractor.conf.js" };

            var filesAfterRemovingExcludedFolders = Directory.GetFiles(sourceDirectoryPath, "*.*", SearchOption.AllDirectories)
                                                             .Where(f => !f.ContainsAny(excludedFolders)).ToList();

            var filesTakenByName = filesAfterRemovingExcludedFolders.Where(f => f.ContainsAny(namedFiles)).ToList();

            var filesTakenByExtension = filesAfterRemovingExcludedFolders.Where(f => includedFileTypeExtensions.Contains(Path.GetExtension(f))).ToList();

            var filesToBeBundled = filesTakenByName.Union(filesTakenByExtension).ToList();

            var fileBundle = new StringBuilder();

            var totalFileSize = filesToBeBundled.Sum(file => new FileInfo(file).Length);

            var fileSizeInMb = totalFileSize > (1024 * 1024) ? (decimal)totalFileSize / (1024 * 1024) : 1;

            var filesPerBucket = (int)Math.Ceiling(filesToBeBundled.Count() / Math.Ceiling(fileSizeInMb));

            if (numberOfBundlesToCreate == 1)
            {
                var bundleName = $"{Path.GetFileName(sourceDirectoryPath).Replace(" ", "")}_Bundle ({encryptionKey})";
                BatchBundle(filesToBeBundled, bundleName);
            }
            else
            {
                var batchId = 1;
                foreach (var fileList in filesToBeBundled.Batch(filesPerBucket))
                {
                    var batchName = $"{Path.GetFileName(sourceDirectoryPath).Replace(" ", "")}_Bundle_{batchId} ({encryptionKey})";
                    BatchBundle(fileList, batchName);
                    batchId++;
                }
            }

            Console.WriteLine(Environment.NewLine + "Bundles have been generated. Check the provided output path.");
            Console.ReadLine();

            void BatchBundle(IEnumerable<string> fileList, string batchName)
            {
                foreach (var filePath in fileList)
                {
                    if (!File.Exists(filePath)) continue;

                    var dirUri = new Uri(sourceDirectoryPath);
                    var fileUri = new Uri(filePath);
                    var relativeUri = dirUri.MakeRelativeUri(fileUri);
                    fileBundle.AppendLine($"FileStart #{relativeUri}");
                    var fileContent = File.ReadAllLines(filePath);
                    fileBundle.AppendLine(fileContent.JoinLines());
                    fileBundle.AppendLine($"FileEnd #{relativeUri}");
                }

                var encryptedText = CryptoService.Encrypt(fileBundle.ToString(), encryptionKey/*"sblw-3hn8-sqoy19"*/);

                var chunks = Split(encryptedText, 10);

                for (var index = 0; index < chunks.Count; index++)
                {
                    File.WriteAllText(Path.Combine(outputPath, $"{batchName} Part{index.ToString("D2")}") + ".txt", chunks[index]);
                }

                //if (!string.IsNullOrEmpty(outputPath))
                //{
                //File.WriteAllText(Path.Combine(outputPath, batchName), encryptedText);
                //Console.WriteLine(Environment.NewLine + "File has been generated !. Check the provided output path.");
                //Console.ReadLine();
                //}
                //else
                //{
                //    Clipboard.SetText(encryptedText);
                //    Console.WriteLine("Bundled file text has been copied to clipboard !");
                //    Console.ReadLine();
                //}
            }
        }

        public static void MergeAndUnbundleFiles(string bundledFolderPath, string destinationDirectory, string encryptionKey, string filePrefix)
        {
            if (Directory.Exists(bundledFolderPath))
            {
                var filesToMerge = Directory.GetFiles(bundledFolderPath).Where(f => f.Contains(filePrefix));

                var mergedFileContent = new StringBuilder();

                filesToMerge.ToList().ForEach(file => mergedFileContent.Append(File.ReadAllText(file)));

                //var fileContent = File.ReadAllText(bundledFolderPath);

                var decryptedText = CryptoService.Decrypt(mergedFileContent.ToString(), encryptionKey/*"sblw-3hn8-sqoy19"*/);

                var stringReader = new StringReader(decryptedText);

                string currentFileRelativePath = null;
                var currentFileContent = new List<string>();

                while (true)
                {
                    var line = stringReader.ReadLine();
                    if (line == null)
                        break;

                    if (line.StartsWith("FileStart"))
                    {
                        currentFileRelativePath = line.Substring(line.IndexOf('#') + 1);
                        continue;
                    }
                    else if (line.StartsWith("FileEnd"))
                    {
                        var fileInfo = new FileInfo(Path.Combine(destinationDirectory, currentFileRelativePath));
                        fileInfo.Directory.Create();
                        File.WriteAllLines(fileInfo.ToString(), currentFileContent);
                        currentFileRelativePath = null;
                        currentFileContent.Clear();
                    }
                    else
                        currentFileContent.Add(line);
                }

                Console.WriteLine(Environment.NewLine + "File has been un-bundled. Check your destination directory.");
                Console.ReadLine();
            }
        }

        public static void UnBundleFiles(string bundledFilePath, string destinationDirectory, string encryptionKey)
        {
            if (!File.Exists(bundledFilePath)) return;

            var fileContent = File.ReadAllText(bundledFilePath);

            var decryptedText = CryptoService.Decrypt(fileContent, encryptionKey/*"sblw-3hn8-sqoy19"*/);

            var stringReader = new StringReader(decryptedText);

            string currentFileRelativePath = null;
            var currentFileContent = new List<string>();

            while (true)
            {
                var line = stringReader.ReadLine();
                if (line == null)
                    break;

                if (line.StartsWith("FileStart"))
                {
                    currentFileRelativePath = line.Substring(line.IndexOf('#') + 1);
                    continue;
                }
                else if (line.StartsWith("FileEnd"))
                {
                    var fileInfo = new FileInfo(Path.Combine(destinationDirectory, currentFileRelativePath));
                    fileInfo.Directory.Create();
                    File.WriteAllLines(fileInfo.ToString(), currentFileContent);
                    currentFileRelativePath = null;
                    currentFileContent.Clear();
                }
                else
                    currentFileContent.Add(line);
            }

            Console.WriteLine(Environment.NewLine + "File has been un-bundled. Check your destination directory.");
            Console.ReadLine();
        }

        private static void PerformFileBundling()
        {
            var sourceDirectoryPath = GetSourceDestinationDirectoryPath();

            var fileBundlePath = GetOutputPath();

            var encryptionKey = GetEncryptionKey();

            var numberOfBundlesToCreate = GetNumberOfBundlesToCreate();

            var outputFilePath = string.IsNullOrWhiteSpace(fileBundlePath) ? null : fileBundlePath;

            Console.WriteLine(Environment.NewLine + "Please wait while directory is being bundled and encrypted...");

            BundleFiles(sourceDirectoryPath, outputFilePath, encryptionKey, numberOfBundlesToCreate);
            //UnBundleFiles(fileBundlePath, @"C:\");
        }

        private static void PerformFileUnBundling()
        {
            //var bundledFilePath = GetBundledFilePath();
            var bundledFilePath = GetSplitBundleFolderPath();

            var filePrefix = GetFilePrefix();

            var destinationDirectoryPath = GetSourceDestinationDirectoryPath();

            var encryptionKey = GetEncryptionKey();

            Console.WriteLine(Environment.NewLine + "Please wait while file is being decrypted and un-bundled...");

            MergeAndUnbundleFiles(bundledFilePath, destinationDirectoryPath, encryptionKey, filePrefix);
            //UnBundleFiles(bundledFilePath, destinationDirectoryPath, encryptionKey);
        }

        private static void PerformFileSplit()
        {
            var bundledFilePath = GetBundledFilePath();

            var fac = new Chilkat.FileAccess();

            var fileInfo = new FileInfo(bundledFilePath);

            var partPrefix = $"{fileInfo.Name}_";
            var partExtension = fileInfo.Extension;
            var maxChunkSize = 2000000;
            var destDirPath = fileInfo.DirectoryName;

            //  Splits hamlet.xml into hamlet1.part, hamlet2.part, ...
            //  Output files are written to the current working directory.
            //  Each chunk will be 50000 bytes except for the last which
            //  will be the remainder.
            var success = fac.SplitFile(bundledFilePath, partPrefix, partExtension, maxChunkSize, destDirPath);

            Console.WriteLine(success ? "Success." : fac.LastErrorText);
        }

        #region Menu
        private static int DisplayMenu()
        {
            var userOption = 0;
            bool retryMenu;
            do
            {
                Console.WriteLine("Choose your option:");
                Console.WriteLine("1. Bundle");
                Console.WriteLine("2. Unbundle");
                Console.WriteLine("3. Split File");
                Console.WriteLine("4. Exit");
                Console.Write("Enter your option: ");
                var optionStr = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(optionStr) || !int.TryParse(optionStr, out userOption) || userOption < 1 || userOption > 4)
                {
                    Console.Clear();
                    Console.WriteLine(Environment.NewLine + "Enter valid option (1 or 2 or 3 or 4) !" + Environment.NewLine);
                    retryMenu = true;
                }
                else
                {
                    retryMenu = false;
                }
            } while (retryMenu);

            Console.Clear();

            return userOption;
        }

        private static int GetNumberOfBundlesToCreate()
        {
            int numberOfBundlesToCreate;
            var retry = false;
            do
            {
                Console.Write("Enter Number of bundles to create: ");
                if (!int.TryParse(Console.ReadLine(), out numberOfBundlesToCreate))
                {
                    Console.WriteLine(Environment.NewLine + "Invalid value provided !!" + Environment.NewLine);
                    retry = true;
                }
                retry = false;

            } while (retry);

            return numberOfBundlesToCreate;
        }

        private static string GetSourceDestinationDirectoryPath()
        {
            string sourceDirectoryPath;
            do
            {
                Console.Write("Enter Source/Destination Directory Path: ");
                sourceDirectoryPath = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(sourceDirectoryPath) && Directory.Exists(sourceDirectoryPath)) continue;
                Console.Clear();
                Console.WriteLine(Environment.NewLine + "Invalid Source/Destination Directory Path !!" + Environment.NewLine);

            } while (string.IsNullOrWhiteSpace(sourceDirectoryPath) && !Directory.Exists(sourceDirectoryPath));

            return sourceDirectoryPath;
        }

        private static string GetOutputPath()
        {
            string fileBundlePath;
            bool retry;
            do
            {
                Console.Write("Enter output directory path to generate file bundles: ");
                fileBundlePath = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(fileBundlePath) || !Directory.Exists(fileBundlePath.Trim()))
                {
                    retry = true;
                    Console.WriteLine(Environment.NewLine + "Invalid Output File Path !!" + Environment.NewLine);
                }
                else
                {
                    retry = false;
                }
            } while (retry);

            return fileBundlePath;
        }

        private static string GetEncryptionKey()
        {
            bool retryKeyInput;
            string encryptionKey;
            do
            {
                Console.Write("Enter encryption key: ");
                encryptionKey = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(encryptionKey))
                {
                    Console.WriteLine(Environment.NewLine + "Invalid encryption key entered !" + Environment.NewLine);
                    retryKeyInput = true;
                }
                else
                {
                    retryKeyInput = false;
                }

            } while (retryKeyInput);

            return encryptionKey;
        }

        private static string GetSplitBundleFolderPath()
        {
            string bundledFolderPath;
            do
            {
                Console.Write(Environment.NewLine + "Enter Bundled Folder Path: ");
                bundledFolderPath = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(bundledFolderPath) || !Directory.Exists(bundledFolderPath))
                {
                    Console.WriteLine(Environment.NewLine + "Invalid Source Directory Path !!" + Environment.NewLine);
                }

            } while (!string.IsNullOrWhiteSpace(bundledFolderPath) && !Directory.Exists(bundledFolderPath));

            return bundledFolderPath;
        }

        private static string GetFilePrefix()
        {
            string filePrefix;
            do
            {
                Console.Write(Environment.NewLine + "Enter File Prefix: ");
                filePrefix = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(filePrefix))
                {
                    Console.WriteLine(Environment.NewLine + "Invalid File Prefix !!" + Environment.NewLine);
                }

            } while (string.IsNullOrWhiteSpace(filePrefix));

            return filePrefix;
        }

        private static string GetBundledFilePath()
        {
            string bundledFilePath;
            do
            {
                Console.Write(Environment.NewLine + "Enter Bundled File Path: ");
                bundledFilePath = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(bundledFilePath) || !File.Exists(bundledFilePath))
                {
                    Console.WriteLine(Environment.NewLine + "Invalid Source Directory Path !!" + Environment.NewLine);
                }

            } while (!string.IsNullOrWhiteSpace(bundledFilePath) && !File.Exists(bundledFilePath));

            return bundledFilePath;
        }
        #endregion

        public static List<string> Split(string str, int chunks)
        {
            var l = new List<string>();
            if (string.IsNullOrEmpty(str))
                return l;
            if (str.Length < chunks)
            {
                l.Add(str);
                return l;
            }
            var chunkSize = str.Length / chunks;

            var stringLength = str.Length;
            for (var i = 0; i < stringLength; i += chunkSize)
            {
                if (i + chunkSize > stringLength)
                    chunkSize = stringLength - i;
                l.Add(str.Substring(i, chunkSize));
            }
            var residual = "";
            l.Where((f, i) => i > chunks - 1).ToList().ForEach(f => residual += f);
            l[chunks - 1] += residual;
            return l.Take(chunks).ToList();
        }
    }
}
