using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using HtmlAgilityPack;
using System.IO;
using ShellProgressBar;

namespace spider
{
    class Program
    {

        static void Main(string[] args)
        {
            bool carry = true;
            while (carry)
            {
                Console.Write("Please enter your link(or enter 'E/e' to exit): ");
                string url = Console.ReadLine();
                /*string url = "https://kemono.party/fanbox/user/70050825/post/4256029";*/

                if (url.ToUpper() =="E") { return; }
                else
                {
                    DateTime startTime = DateTime.Now;
                    downloadGallery(url);
                    DateTime endTime = DateTime.Now;

                    //TODO:异步多线程实现，现在效率极低！
                    //TODO:窗口化
                    //TODO:监控进度条

                    Console.WriteLine($"All Downloaded.\nTotal time: {Math.Round((endTime - startTime).TotalSeconds, 2)}s.");
                    Console.ResetColor();
                }
            }
        }

        static void downloadGallery(string url)
        {
            string htmlContent;
            string pagePath = "//*[@id=\"page\"]/";

            WebClient client = new WebClient();
            HtmlDocument document = new HtmlDocument();
            client.Encoding = Encoding.UTF8; //解决乱码
            htmlContent = client.DownloadString(url);
            document.LoadHtml(htmlContent);

            string galleryPath = pagePath + "div/div";
            string authorPath = pagePath + "header/div[1]/div[2]/a";
            string topicPath = pagePath + "header/div[2]/h1";

            HtmlNodeCollection divNodes = document.DocumentNode.SelectNodes(galleryPath);
            HtmlNode authorNode = document.DocumentNode.SelectSingleNode(authorPath);
            HtmlNode topicNode = document.DocumentNode.SelectSingleNode(topicPath);
            string authorText = authorNode.InnerText.Trim();
            string topicText = topicNode.InnerText.Trim();
            Console.WriteLine($"Author:{authorText}");
            Console.WriteLine($"Topic:{topicText}");

            //create file
            if (topicText.Contains("/"))
            {
                topicText = topicText.Replace("/", "_");
            }
            string createFilePath = $@"{Directory.GetCurrentDirectory()}/Download/【{authorText}】{topicText}";
            Directory.CreateDirectory(createFilePath);
            /*Console.WriteLine(createFilePath);*/

            foreach (HtmlNode divNode in divNodes)
            {
                HtmlNodeCollection hrefNodes = divNode.SelectNodes(".//a[@href]");
                if (hrefNodes != null)
                {
                    int index = 1;
                    foreach (HtmlNode hrefNode in hrefNodes)
                    {
                        string imageUrl = hrefNode.GetAttributeValue("href", "");
                        Console.ResetColor();
                        Console.WriteLine(imageUrl);
                        try
                        {
                            string fileName = Path.GetFileName(imageUrl).Split(new char[] { '.' })[^1];
                            string filePath = $"{createFilePath}/Art-{index}" + '.' + fileName;

                            //进度条显示
                            

                            client.DownloadFile(imageUrl, filePath);

                            FileInfo fileInfo = new FileInfo(filePath);
                            double fileSizeInMB = 1.0;
                            if (fileInfo.Exists)
                            {
                                long fileSizeInB = fileInfo.Length;
                                fileSizeInMB = fileSizeInB / 1024.0 / 1024.0;
                            }

                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"Art-{index++}:Download already!");
                            Console.WriteLine($"ImageSize: {Math.Round(fileSizeInMB, 2)}MB");
                        }
                        catch (Exception ex)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Download Error! " + ex.Message);
                        }
                    }
                }
            }
        }
    }
}