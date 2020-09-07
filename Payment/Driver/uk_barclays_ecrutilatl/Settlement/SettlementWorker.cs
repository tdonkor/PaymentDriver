using Acrelec.Library.Logger;
//using com.ingenico.cli.comconcert;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Acrelec.Mockingbird.Payment.Configuration;
using System.Text;
using ECRUtilATLLib;

namespace Acrelec.Mockingbird.Payment.Settlement
{
    public class SettlementWorker
    {
        //public static void OnSettlement(Action<string> fileSender)
        //{
        //    Log.Info("Auto settlement has been triggered...");

        //    try
        //    {
        //        ExecuteSettlement();

        //        Log.Info("Auto settlement executed!");

        //        Log.Info("Ziping files...");
        //        var files = GetFiles();
        //        var zipPath = ZipFiles(files);

        //        Log.Info("Sending files...");
        //        fileSender(zipPath);

        //        files.Add(zipPath, null);

        //        Log.Info("Removing sent files...");
        //        foreach (var file in files.Keys)
        //        {
        //            if (File.Exists(file))
        //            {
        //                File.Delete(file);
        //            }
        //        }

        //        Log.Info("Auto settlement executed succesfully!");
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error("Could not execute auto settlement");
        //        Log.Error(ex);
        //        throw ex;
        //    }
        //}

        private static IDictionary<string, string> GetFiles()
        {
            var outPath = Path.GetFullPath(AppConfiguration.Instance.OutPath);
            var outFiles = Directory.GetFiles(outPath, "*.*", SearchOption.AllDirectories).ToDictionary(_ => _, _ => Path.Combine("out", GetRelativePath(outPath, _)));

            var logPath = Path.GetFullPath("Logs");
            var logFiles = Directory.GetFiles(logPath, "*.*", SearchOption.AllDirectories).ToDictionary(_ => _, _ => Path.Combine("logs", GetRelativePath(logPath, _)));

            return Enumerable.Concat(outFiles, logFiles).ToDictionary(_ => _.Key, _ => _.Value);
        }


        //private static void ExecuteSettlement()
        //{
        //    using (var api = new ECRUtilATLApi())
        //    {
        //        var config = RuntimeConfiguration.Instance;

        //        api.Connect(config.IpAddress);

        //        Log.Info("Executing auto settlement...");
        //        var result = api.EndOfDayReport();
        //        if (result == null)
        //        {
        //            Log.Info($"Error executing settlement: {result}");
        //        }
        //        else
        //        {
        //            Log.Info("Auto settlement executed.");
        //           PersistReport(result);
        //        }
        //    }
        //}

        private static void PersistReport(SettlementClass report)
        {
            try
            {
                StringBuilder reportContent = new StringBuilder();
                Log.Info($"Persist Report");

                //get the reponse details for the ticket
                reportContent.Append($"End of Day\n");
                reportContent.Append($"================\n\n");



                reportContent.Append($"{report.AcquirerMerchantIDOut}\n");
                reportContent.Append($"{report.AcquirerNameOut}\n");
                reportContent.Append($"{report.LastMessageNumberOut}\n");
                reportContent.Append($"{report.MerchantAddress1Out}\n");
                reportContent.Append($"{report.MerchantAddress2Out}\n");
                reportContent.Append($"{report.NumCardSchemeOut}\n");
                reportContent.Append($"{report.QuantityCashOut}\n");
                reportContent.Append($"{report.QuantityClessCreditsOut}\n");
                reportContent.Append($"{report.QuantityClessDebitsOut}\n");
                reportContent.Append($"{report.QuantityCreditsOut}\n");
                reportContent.Append($"{report.QuantityDebitsOut}\n");
                reportContent.Append($"{report.SettlementResultOut}\n");
                reportContent.Append($"{report.TerminalIdentityOut}\n");
                reportContent.Append($"{report.ValueCashOut}\n");
                reportContent.Append($"{report.ValueClessCreditsOut}\n");
                reportContent.Append($"{report.ValueClessDebitsOut}\n");
                reportContent.Append($"{report.ValueCreditsOut}\n");
                reportContent.Append($"{report.ValueDebitsOut}\n");

                var config = AppConfiguration.Instance;
                var outputDirectory = Path.GetFullPath(config.OutPath);
                var outputPath = Path.Combine(outputDirectory, $"{DateTime.Now:yyyyMMddHHmmss}_settlement.txt");

                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                Log.Info($"Persist Report path: {outputPath}");
                //Write the new ticket
                File.WriteAllText(outputPath, reportContent.ToString());
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
