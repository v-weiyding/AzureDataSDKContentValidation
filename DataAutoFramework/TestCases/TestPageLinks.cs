﻿using NUnit.Framework;
using DataAutoFramework.Helper;
using NUnit.Framework.Legacy;
using System.Text.Json;
using Microsoft.Playwright;

namespace DataAutoFramework.TestCases
{
    public class TestPageLinks
    {
        public static List<string> TestLinks { get; set; }

        public static Dictionary<string, string> SpecialLinks { get; set; }

        static TestPageLinks()
        {
            //TestLinks = new List<string>
            //{
            //    "https://learn.microsoft.com/en-us/python/api/overview/azure/app-configuration?view=azure-python",
            //    "https://learn.microsoft.com/en-us/python/api/overview/azure/appconfiguration-readme?view=azure-python",
            //    "https://learn.microsoft.com/en-us/python/api/azure-appconfiguration/azure.appconfiguration?view=azure-python",
            //    "https://learn.microsoft.com/en-us/python/api/azure-appconfiguration/azure.appconfiguration.aio?view=azure-python",
            //    "https://learn.microsoft.com/en-us/python/api/azure-appconfiguration/azure.appconfiguration.aio.azureappconfigurationclient?view=azure-python",
            //    "https://learn.microsoft.com/en-us/python/api/azure-appconfiguration/azure.appconfiguration.azureappconfigurationclient?view=azure-python"
            //};

            TestLinks = JsonSerializer.Deserialize<List<string>>(File.ReadAllText("appsettings.json")) ?? new List<string>();

            SpecialLinks = new Dictionary<string, string>();

            SpecialLinks.Add("Read in English", "https://learn.microsoft.com/en-us/python/api/overview/azure/app-configuration?view=azure-python");
            SpecialLinks.Add("our contributor guide", "https://github.com/Azure/azure-sdk-for-python/blob/main/CONTRIBUTING.md");
            // SpecialLinks.Add("English (United States)", "/en-us/locale?target=https%3A%2F%2Flearn.microsoft.com%2Fen-us%2Fpython%2Fapi%2Foverview%2Fazure%2Fapp-configuration%3Fview%3Dazure-python");
            SpecialLinks.Add("Privacy", "https://go.microsoft.com/fwlink/?LinkId=521839");
        }

        [Test]
        [TestCaseSource(nameof(TestLinks))]
        public async Task TestCrossLinks(string testLink)
        {
            var playwright = await Playwright.CreateAsync();
            var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            var page = await browser.NewPageAsync();
            await page.GotoAsync(testLink);

            var hrefs = page.Locator("#main-column").Locator("a");
            var failCount = 0;
            var failMsg = "";

            for(var index = 0; index < await hrefs.CountAsync(); index++)
            {
                var href = hrefs.Nth(index);
                var attri = href.GetAttributeAsync("href").Result;
                var text = href.InnerTextAsync().Result;

                if (String.IsNullOrEmpty(text.Trim()) || text.Trim() == "English (United States)")
                {
                    continue;
                }

                if (SpecialLinks.ContainsKey(text.Trim()) && SpecialLinks[text.Trim()] == attri)
                {
                    continue;
                }

                var subContent = text.ToLower().Replace("-", " ").Replace("@", " ").Split(" ");
                var flag = false;

                foreach (string s in subContent)
                {
                    if (attri?.ToLower().Replace(".", "").Contains(s) ?? false)
                    {
                        flag = true;
                        break;
                    }
                    else
                    {
                        continue;
                    }
                }

                if (!flag)
                {
                    failCount++;
                    failMsg = failMsg + text.Trim() + ": " + attri + "\n";
                }
            }

            ClassicAssert.Zero(failCount, failMsg);
        }

        [Test]
        [TestCaseSource(nameof(TestLinks))]
        public async Task TestBrokenLinks(string testLink)
        {
            string baseUri = "https://learn.microsoft.com/";

            using var playwright = await Playwright.CreateAsync();
            var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            var page = await browser.NewPageAsync();

            await page.GotoAsync(testLink);

            var links = await page.Locator("a").AllAsync();

            var errorList = new List<string>();

            foreach (var link in links)
            {
                var href = await link.GetAttributeAsync("href");
                if (!string.IsNullOrEmpty(href) && !href.StartsWith("mailto"))
                {
                    if (href.StartsWith("#"))
                    {
                        href = testLink + href;
                    }
                    else if (!href.StartsWith("#") && !href.StartsWith("http") && !href.StartsWith("https"))
                    {
                        href = baseUri + href;
                    }
                    if (!await ValidationHelper.CheckIfPageExist(href))
                    {
                        errorList.Add(href);
                    }
                }
            }

            await browser.CloseAsync();

            ClassicAssert.Zero(errorList.Count, testLink + " has error link at " + string.Join(",", errorList));
        }
    }
}
