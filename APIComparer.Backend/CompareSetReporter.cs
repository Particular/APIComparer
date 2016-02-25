namespace APIComparer.Backend
{
    using System;
    using System.IO;
    using Reporting;

    public class CompareSetReporter
    {
        public void Report(PackageDescription description, DiffedCompareSet[] diffedCompareSets)
        {
            var resultPath = DetermineAndCreateResultPathIfNotExistant(description);

            using (var fileStream = File.OpenWrite(resultPath))
            {
                using (var into = new StreamWriter(fileStream))
                {
                    var formatter = new APIUpgradeToHtmlFormatter();
                    formatter.Render(into, description, diffedCompareSets);

                    @into.Flush();
                    @into.Close();
                    fileStream.Close();
                }
            }
            RemoveTemporaryWorkFiles(resultPath);
        }

        public void ReportFailure(PackageDescription packageDescription, Exception exception)
        {
            var resultPath = DetermineAndCreateResultPathIfNotExistant(packageDescription);
            File.WriteAllText(resultPath, $"{packageDescription.PackageId} comparison between {packageDescription.Versions.LeftVersion} and {packageDescription.Versions.RightVersion} has failed with the following error: {exception.Message}");
            RemoveTemporaryWorkFiles(resultPath);
        }

        static string DetermineAndCreateResultPathIfNotExistant(PackageDescription description)
        {
            var resultFile = $"{description.PackageId}-{description.Versions.LeftVersion}...{description.Versions.RightVersion}.html";

            var rootPath = Environment.GetEnvironmentVariable("HOME"); // TODO: use AzureEnvironment

            if (rootPath != null)
            {
                rootPath = Path.Combine(rootPath, @".\site\wwwroot");
            }
            else
            {
                rootPath = Environment.GetEnvironmentVariable("APICOMPARER_WWWROOT", EnvironmentVariableTarget.User);
            }

            if (string.IsNullOrEmpty(rootPath))
            {
                throw new Exception("No root path could be found. If in development please set the `APICOMPARER_WWWROOT` env variable to the root folder of the webproject");
            }

            var directoryPath = Path.Combine(rootPath, "Comparisons");

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            var resultPath = Path.Combine(directoryPath, resultFile);
            return resultPath;
        }

        static void RemoveTemporaryWorkFiles(string resultPath)
        {
            File.Delete(Path.ChangeExtension(resultPath, ".running.html"));
        }
    }
}