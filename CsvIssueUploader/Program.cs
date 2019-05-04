using CommandLine;
using CsvHelper;
using Octokit;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CsvIssueUploader
{
    class Options
    {
        [Option('f',"fileName",Required = true)]
        public string FileName { get; set; }
        [Option('u', "userName", Required = true)]
        public string UserName { get; set; }
        [Option('p', "password", Required = true)]
        public string Password { get; set; }
        [Option('o', "openSource", Required = true)]
        public string OpenSource { get; set; }
        [Option('c', "closeSource", Required = true)]
        public string CloseSource { get; set; }
        
    }
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(options =>
            {
                using (var reader = new StreamReader(options.FileName))
                using (var csv = new CsvReader(reader))
                {
                    var client = new GitHubClient(new ProductHeaderValue("CsvIssueUploader"));
                    var basicAuth = new Credentials(options.UserName, options.Password);
                    client.Credentials = basicAuth;
                    csv.Configuration.PrepareHeaderForMatch = (string header, int index) => header.Replace(" ", "");
                    var records = csv.GetRecords<CsvIssue>();
                    foreach (var record in records)
                    {
                        var issue = new NewIssue(record.FeatureName)
                        {
                            Body = $"{record.Description}\n{record.Why}"
                        };
                        var labels = record.Category.Split(",").Select(x => x.Trim());
                        foreach (var label in labels)
                            issue.Labels.Add(label);
                        var repository = IsOpenSource(record.OpenSource) ? options.OpenSource : options.CloseSource;
                        client.Issue.Create(options.UserName, repository, issue).Wait();
                        Console.WriteLine(record.FeatureName);
                    }
                }
            });
        }

        private static bool IsOpenSource(string openSource)
        {
            return openSource == "y" || openSource == "Y";
        }
    }
}