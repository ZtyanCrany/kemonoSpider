using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
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
        static int artNum = 0;
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
                    Console.WriteLine($"Download number: {fileNum}, Total size: {Math.Round((totalFileSize / (1024.0 * 1024.0)), 2)}MB.\n");
                    Console.ResetColor();
                    index = 0; totalFileSize = 0; fileNum = 0;
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

            if (topicText.Contains("/") || topicText.Contains("|"))
            {
                topicText = topicText.Replace("/", "_");
                topicText = topicText.Replace("|", "-");
            }
            string createFilePath = $@"{Directory.GetCurrentDirectory()}/Download/【{authorText}】{topicText}";
            Directory.CreateDirectory(createFilePath);

            var progressBarOptions = new ProgressBarOptions
            {
                ProgressCharacter = '▅',
                BackgroundColor = ConsoleColor.DarkGray,
                ProgressBarOnBottom = true,
                DisableBottomPercentage = false,
                CollapseWhenFinished = false,
            };
            var childProgressBarOptions = new ProgressBarOptions
            {
                ProgressCharacter = '▂',
                BackgroundColor = ConsoleColor.DarkGray,
                ProgressBarOnBottom = false,
                DisableBottomPercentage = false,

            };

            int downloadNum = 0;
            
            foreach (HtmlNode divNode in divNodes)
            {
                HtmlNodeCollection hrefNodes = divNode.SelectNodes(".//a[@href]");
                if (hrefNodes != null)
                {
                    foreach (HtmlNode hrefNode in hrefNodes)
                    {
                        downloadNum++;
                    }
                }
            }

            using (var progressBar = new ProgressBar(downloadNum, "Downloading...", progressBarOptions))
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
                            using (var childProgressBar = progressBar.Spawn(1,$"Art-{++artNum} is coming~",childProgressBarOptions))
                            {
                                tasks.Add(DownloadGallery(imageUrl, createFilePath, progressBar, childProgressBar, artNum));
                            }
                        }
                    }
                }
                await Task.WhenAll(tasks);
                progressBar.Message = "Download complete!";
            }
        }

        static int index = 0;
        static int fileNum = 0;
        static async Task DownloadGallery(string imageUrl, string createFilePath, ProgressBar progressBar, ChildProgressBar childProgressBar, int Artnum)
        {
            try
            {
                //设置保存路径与文件名
                string fileName = Path.GetFileName(imageUrl).Split(new char[] { '.' })[^1];
                string filePath = $"{createFilePath}/Art-{++index}" + '.' + fileName;
                Thread.Sleep(30);

                //获取图片大小前置
                HttpWebRequest hwrequest = (HttpWebRequest)WebRequest.Create(imageUrl);
                hwrequest.Method = "HEAD";
                HttpWebResponse hwresponse = (HttpWebResponse)hwrequest.GetResponse();

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

                //状态更新
                if (hwresponse.StatusCode == HttpStatusCode.OK)
                {
                    childProgressBar.Message = $"Art-{Artnum} is ready! Image size: {Math.Round((hwresponse.ContentLength / (1024.0 * 1024.0)), 2)}MB.";
                }          

                //Calculate downloads
                FileInfo fileInfo = new FileInfo(filePath);
                fileNum = Directory.GetFiles(fileInfo.DirectoryName).Length;
                if (fileInfo.Exists)
                {
                    long fileSizeInB = fileInfo.Length;
                    Interlocked.Add(ref totalFileSize, fileSizeInB);
                }

                Console.ForegroundColor = ConsoleColor.Green;
                progressBar.Tick();
                childProgressBar.Tick();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Download Error! " + ex.Message);
            }
        }
    }
}