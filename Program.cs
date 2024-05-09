using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using ShellProgressBar;

namespace spider
{
    class Program
    {
        private static HttpClient client = new HttpClient();
        private static string downloadPath = string.Empty;
        private static long totalFileSize = 0;
        static async Task Main(string[] args)
        {
            while (true)
            {
                LoadDownloadPath();
                Console.Write("Please Enter Your Link or: \n" +
                    $"-> enter 'S/s' to change downloadpath.\n (Current: {downloadPath})\n" +
                    "-> enter 'E/e' to exit.\n");
                string url = Console.ReadLine();
                if (url.ToUpper() == "E")
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("*******\nThanks for using and have a nice day :D\n" +
                                      "Author: ZtyanCrany\n*******\n");
                    Console.ResetColor();
                    Thread.Sleep(2500);
                    return;
                }
                if (url.ToUpper() == "S")
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("*******\nPlease enter your download path: ");
                    string path = Console.ReadLine();
                    if (Directory.Exists(path))
                    {
                        downloadPath = path;
                        SaveDownloadPath();
                        Console.WriteLine($"Download path change to ({downloadPath}).\n*******");
                    }
                    else
                    {
                        Console.WriteLine("Path does not exist.\n*******");
                    }
                    Console.ResetColor();
                    continue;
                }
                else
                {
                    DateTime startTime = DateTime.Now;
                    await DownloadSettings(url);
                    DateTime endTime = DateTime.Now;
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"\nAuthor: {authorText}\n" +
                                      $"Topic: {topicText}\n");

                    Console.ResetColor();
                    Console.WriteLine($"Task Completed, {errorIndex} download failed.\n" +
                                      $"Total time: {Math.Round((endTime - startTime).TotalSeconds, 2)}s.\n" +
                                      $"Download number: {fileNum}, " +
                                      $"Total size: {Math.Round(totalFileSize / (1024.0 * 1024.0), 2)}MB.\n" +
                                      $"Download path: {downloadPath}\n");

                    totalFileSize = 0; fileNum = 0; errorIndex = 0;
                }
            }
        }

        static void LoadDownloadPath()
        {
            string pathfile = "downloadpath.txt";
            if (File.Exists(pathfile))
            {
                downloadPath = File.ReadAllText(pathfile);
            }
            else
            {
                downloadPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            }
        }
        static void SaveDownloadPath()
        {
            string pathfile = "downloadpath.txt";
            File.WriteAllText(pathfile, downloadPath);
        }

        static string authorText = string.Empty;
        static string topicText = string.Empty;
        static async Task DownloadSettings(string url)
        {
            try
            {
                string pagePath = "//*[@id=\"page\"]/";

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11;
                HtmlWeb web = new HtmlWeb();
                HtmlDocument document = await web.LoadFromWebAsync(url);

                string galleryPath = $"{pagePath}div/div";
                string authorPath = $"{pagePath}header/div[1]/div[2]/a";
                string topicPath = $"{pagePath}header/div[2]/h1";

                HtmlNodeCollection divNodes = document.DocumentNode.SelectNodes(galleryPath);
                HtmlNode authorNode = document.DocumentNode.SelectSingleNode(authorPath);
                HtmlNode topicNode = document.DocumentNode.SelectSingleNode(topicPath);

                authorText = authorNode?.InnerText.Trim() ?? "Unknown Author";
                topicText = topicNode?.InnerText.Trim() ?? "Unknown Topic";
                authorText = Regex.Replace(authorText, "[<>:\"/\\|?*]", "");
                topicText = Regex.Replace(topicText, "[<>:\"/\\|?*]", "");

                string createFilePath = Path.Combine(downloadPath, $"【{authorText}】{topicText}");
                Directory.CreateDirectory(createFilePath);

                await DownloadGallerys(divNodes, createFilePath);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Web Error! " + ex.Message);
            }
        }
        
        static ProgressBarOptions progressBarOptions = new ProgressBarOptions
        {
            ProgressCharacter = '▅',
            BackgroundColor = ConsoleColor.DarkGray,
            ProgressBarOnBottom = true,
            DisableBottomPercentage = false,
            CollapseWhenFinished = false,
            
        };
        static ProgressBarOptions childProgressBarOptions = new ProgressBarOptions
        {
            ProgressCharacter = '▂',
            BackgroundColor = ConsoleColor.DarkGray,
            ProgressBarOnBottom = false,
            DisableBottomPercentage = false,
        };
        private static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(6);
        static async Task DownloadGallerys(HtmlNodeCollection divNodes, string createFilePath)
        {
            int downloadNum = divNodes.Sum(divNode => (divNode.SelectNodes("*/figure/a")?.Count) ?? 0);
            using (var progressBar = new ProgressBar(downloadNum, "Downloading...", progressBarOptions))
            {
                var tasks = divNodes.SelectMany(divNode => divNode.SelectNodes("*/figure/a") ?? Enumerable.Empty<HtmlNode>())
                    .Select(async (imgNode, index) =>
                    {
                        await semaphoreSlim.WaitAsync();
                        try
                        {
                            string imageUrl = imgNode.GetAttributeValue("href", "");
                            Console.ResetColor();
                            using (var childProgressBar = progressBar.Spawn(1, $"Art-{index + 1} is coming~", childProgressBarOptions))
                            {
                                await DownloadGallery(imageUrl, createFilePath, progressBar, childProgressBar, index + 1);
                            }
                        }
                        finally
                        {
                            semaphoreSlim.Release();
                        }
                    });

                await Task.WhenAll(tasks);
                progressBar.Message = "Download complete!";
            }
        }


        static int fileNum = 0;
        static int errorIndex = 0;
        static async Task DownloadGallery(string imageUrl, string createFilePath, ProgressBar progressBar, ChildProgressBar childProgressBar, int Artnum)
        {
            int tryCount = 16;
            while (tryCount > 0)
            {
                try
                {
                    string fileName = Path.GetFileName(imageUrl).Split('.').LastOrDefault();
                    string filePath = Path.Combine(createFilePath, $"Art-{Artnum}.{fileName}");
                    Thread.Sleep(200);

                    using (var hwresponse = await GetHeadResponse(imageUrl))
                    {
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

                        if (hwresponse.StatusCode == HttpStatusCode.OK)
                        {
                            double imageSizeInMB = 0;
                            if (hwresponse.Content.Headers.ContentLength.HasValue)
                            {
                                imageSizeInMB = Math.Round(hwresponse.Content.Headers.ContentLength.Value / (1024.0 * 1024.0), 2);
                            }
                            childProgressBar.Message = $"Art-{Artnum} is ready! Image size: {imageSizeInMB}MB.";
                        }

                        FileInfo fileInfo = new FileInfo(filePath);
                        if (fileInfo.DirectoryName != null)
                        {
                            fileNum = Directory.GetFiles(fileInfo.DirectoryName).Length;
                            if (fileInfo.Exists)
                            {
                                Interlocked.Add(ref totalFileSize, fileInfo.Length);
                            }
                        }
                        Console.ForegroundColor = ConsoleColor.Green;
                        progressBar.Tick();
                        childProgressBar.Tick();
                    }
                    break;
                }
                catch (Exception ex)
                {
                    tryCount--;
                    if (tryCount > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        childProgressBar.Message = $"Art-{Artnum} Download Error! Retrying...";
                    }
                    if (tryCount == 0)
                    {
                        errorIndex++;
                        Console.ForegroundColor = ConsoleColor.Red;
                        childProgressBar.Message = $"Art-{Artnum} Download Error! " + ex.Message;
                    }
                }
            }

        }
        static async Task<HttpResponseMessage> GetHeadResponse(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Head, url);
                var response = await client.SendAsync(request);
                return response;
            }
        }
    }
}