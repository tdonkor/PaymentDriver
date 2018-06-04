using Acrelec.Library.Logger;
using com.ingenico.cli.comconcert;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using Acrelec.Mockingbird.Payment.Configuration;
using Acrelec.Mockingbird.Payment.Contracts;

namespace Acrelec.Mockingbird.Payment.Settlement
{
    public class SettlementWorker
    {
        public static void OnSettlement(Action<string> fileSender)
        {
            Log.Info("Auto settlement has been triggered...");

            try
            {
                ExecuteSettlement();

                Log.Info("Auto settlement executed!");

                Log.Info("Ziping files...");
                var files = GetFiles();
                var zipPath = ZipFiles(files);

                Log.Info("Sending files...");
                fileSender(zipPath);

                files.Add(zipPath, null);

                Log.Info("Removing sent files...");
                foreach (var file in files.Keys)
                {
                    if (File.Exists(file))
                    {
                        File.Delete(file);
                    }
                }

                Log.Info("Auto settlement executed succesfully!");
            }
            catch (Exception ex)
            {
                Log.Error("Could not execute auto settlement");
                Log.Error(ex);
                throw ex;
            }
        }

        private static IDictionary<string, string> GetFiles()
        {
            var outPath = Path.GetFullPath(AppConfiguration.Instance.OutPath);
            var outFiles = Directory.GetFiles(outPath, "*.*", SearchOption.AllDirectories).ToDictionary(_ => _, _ => Path.Combine("out", GetRelativePath(outPath, _)));

            var logPath = Path.GetFullPath("Logs");
            var logFiles = Directory.GetFiles(logPath, "*.*", SearchOption.AllDirectories).ToDictionary(_ => _, _ => Path.Combine("logs", GetRelativePath(logPath, _)));

            return Enumerable.Concat(outFiles, logFiles).ToDictionary(_ => _.Key, _ => _.Value);
        }

        private static void ExecuteSettlement()
        {
            using (var api = new ComConcertApi())
            {
                var config = RuntimeConfiguration.Instance;

                api.Connect(config.Port);

                Log.Info("Executing auto settlement...");
                var result = api.EndOfDayReport(120000, out var response);
                if (result != COMConcertLibrary.ConcertErrMsg.None)
                {
                    Log.Info($"Error executing settlement: {result}");
                }
                else
                {
                    Log.Info("Auto settlement executed.");
                    PersistReport(response.nonConcertData);
                }
            }
        }

        private static void PersistReport(string buffer)
        {
            try
            {
                var bufferDocument = XDocument.Parse(buffer);
                var receipt = bufferDocument.XPathSelectElement("//RECEIPT");
                var root = new XElement("EndOfDay");
                if (receipt != null)
                {
                    root.Add(
                        receipt.Elements().Select(_ =>
                        {
                            var tag = new XElement("tag", _.Value);
                            if (_.Attribute("ID") != null)
                            {
                                tag.Add(new XAttribute("id", _.Attribute("ID").Value));
                            }

                            if (_.Attribute("ID_NAME") != null)
                            {
                                tag.Add(new XAttribute("name", _.Attribute("ID_NAME").Value));
                            }
                            return tag;
                        }));
                }

                var config = AppConfiguration.Instance;
                var outputDirectory = Path.GetFullPath(config.OutPath);
                var outputPath = Path.Combine(outputDirectory, $"{DateTime.Now:yyyyMMddHHmmss}_settlement.xml");

                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                Log.Info($"Persist Report path: {outputPath}");
                //Write the new ticket
                new XDocument(root).Save(outputPath);
            }
            catch (Exception ex)
            {
                Log.Info("PersistTicket error.");
                Log.Error(ex);
            }
        }

        private static string ZipFiles(IDictionary<string, string> files)
        {
            string targetPath = Path.Combine("compressed", $"{DateTime.Now:yyyyMMddHHmmss}.zip");

            Log.Info($"Zipping file to: {targetPath}");

            if (!Directory.Exists(Path.GetDirectoryName(targetPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
            }

            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }

            using (var zipFile = ZipFile.Open(targetPath, ZipArchiveMode.Create))
            {
                foreach (var fileEntry in files)
                {
                    zipFile.CreateEntryFromFile(fileEntry.Key, fileEntry.Value);
                }
            }

            return targetPath;
        }

        /// <summary>
        /// Creates a relative path from one file or folder to another.
        /// </summary>
        /// <param name="fromPath">Contains the directory that defines the start of the relative path.</param>
        /// <param name="toPath">Contains the path that defines the endpoint of the relative path.</param>
        /// <returns>The relative path from the start directory to the end path.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="fromPath"/> or <paramref name="toPath"/> is <c>null</c>.</exception>
        /// <exception cref="UriFormatException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static string GetRelativePath(string fromPath, string toPath)
        {
            Func<string, string> appendDirectorySeparatorChar = (path) =>
            {
                // Append a slash only if the path is a directory and does not have a slash.
                if (!Path.HasExtension(path) &&
                    !path.EndsWith(Path.DirectorySeparatorChar.ToString()))
                {
                    return path + Path.DirectorySeparatorChar;
                }

                return path;
            };

            var fromUri = new Uri(appendDirectorySeparatorChar(fromPath));
            var toUri = new Uri(appendDirectorySeparatorChar(toPath));

            if (fromUri.Scheme != toUri.Scheme)
            {
                return toPath;
            }

            var relativeUri = fromUri.MakeRelativeUri(toUri);
            var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            if (string.Equals(toUri.Scheme, Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase))
            {
                relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }

            return relativePath;
        }
    }
}
