using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using ShellProgressBar;

namespace spider
{
    class Program
    {
        private static HttpClient client = new HttpClient();
        private static long totalFileSize = 0;
        static async Task Main(string[] args)
        {
            bool carry = true;
            while (carry)
            {
                Console.Write("Please enter your link (or enter 'E/e' to exit): ");
                string url = Console.ReadLine();

                if (url.ToUpper() == "E") 
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("*******\nThanks for using and have a nice day :D\nWriter: ZtyanCrany\n*******\n");
                    Thread.Sleep(2500);
                    return;
                }
                else
                {
                    DateTime startTime = DateTime.Now;
                    await DownloadSettings(url);
                    DateTime endTime = DateTime.Now;

                    Console.WriteLine($"All Downloaded.\nTotal time: {Math.Round((endTime - startTime).TotalSeconds, 2)}s.");
                    Console.WriteLine($"Download number: {index}, Total size: {Math.Round((totalFileSize / (1024.0 * 1024.0)), 2)}MB.\n");
                    Console.ResetColor();
                    index = 0;
                }
            }
        }

        static async Task DownloadSettings(string url)
        {
            string htmlContent;
            string pagePath = "//*[@id=\"page\"]/";

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/html"));

            htmlContent = await client.GetStringAsync(url);
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(htmlContent);

            string galleryPath = pagePath + "div/div";
            string authorPath = pagePath + "header/div[1]/div[2]/a";
            string topicPath = pagePath + "header/div[2]/h1";

            HtmlNodeCollection divNodes = document.DocumentNode.SelectNodes(galleryPath);
            HtmlNode authorNode = document.DocumentNode.SelectSingleNode(authorPath);
            HtmlNode topicNode = document.DocumentNode.SelectSingleNode(topicPath);
            string authorText = authorNode.InnerText.Trim();
            string topicText = topicNode.InnerText.Trim();
            Console.ForegroundColor=ConsoleColor.Cyan;
            Console.WriteLine($"Author: {authorText}");
            Console.WriteLine($"Topic: {topicText}");
            Console.ResetColor();

            if (topicText.Contains("/"))
            {
                topicText = topicText.Replace("/", "_");
            }
            string createFilePath = $@"{Directory.GetCurrentDirectory()}/Download/【{authorText}】{topicText}";
            Directory.CreateDirectory(createFilePath);

            var progressBarOptions = new ProgressBarOptions
            {
                ProgressCharacter = '▇',
                ProgressBarOnBottom = true
            };

            using (var progressBar = new ProgressBar(divNodes.Count, "Downloading...", progressBarOptions))
            {
                var tasks = new List<Task>();

                foreach (HtmlNode divNode in divNodes)
                {
                    HtmlNodeCollection hrefNodes = divNode.SelectNodes(".//a[@href]");
                    if (hrefNodes != null)
                    {
                        foreach (HtmlNode hrefNode in hrefNodes)
                        {
                            string imageUrl = hrefNode.GetAttributeValue("href", "");
                            Console.ResetColor();
                            Console.WriteLine(imageUrl);

                            tasks.Add(DownloadGallery(imageUrl, createFilePath, progressBar));
                        }
                    }
                }
                await Task.WhenAll(tasks);
                progressBar.Message = "Download complete!";
            }
        }

        static int index = 0;
        static async Task DownloadGallery(string imageUrl, string createFilePath, ProgressBar progressBar)
        {
            try
            {
                string fileName = Path.GetFileName(imageUrl).Split(new char[] { '.' })[^1];
                string filePath = $"{createFilePath}/Art-{++index}" + '.' + fileName;
                Thread.Sleep(10);

                using (var response = await client.GetAsync(imageUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        using (var fileStream = File.Create(filePath))
                        {
                            await stream.CopyToAsync(fileStream);
                        }
                    }
                }

                //Calculate downloads
                FileInfo fileInfo = new FileInfo(filePath);
                if (fileInfo.Exists)
                {
                    long fileSizeInB = fileInfo.Length;
                    Interlocked.Add(ref totalFileSize, fileSizeInB);
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Downloading... Art-{index}");
                progressBar.Tick();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Download Error! " + ex.Message);
            }
        }
    }
}