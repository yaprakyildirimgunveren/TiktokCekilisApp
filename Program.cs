using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TiktokCekilisApp
{
    internal class Program
    {
        private class CommentItem
        {
            public string user { get; set; }
            public string text { get; set; }
        }

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            string videoUrl;
            while (true)
            {
                Console.Write("Video linki: ");
                videoUrl = (Console.ReadLine() ?? string.Empty).Trim().Trim('"');

                if (string.IsNullOrWhiteSpace(videoUrl))
                {
                    Console.WriteLine("Video linki bos olamaz.");
                    continue;
                }

                if (Uri.TryCreate(videoUrl, UriKind.Absolute, out var uri) &&
                    (uri.Host.Contains("tiktok.com", StringComparison.OrdinalIgnoreCase)))
                {
                    break;
                }

                Console.WriteLine("Gecersiz link. Ornek: https://www.tiktok.com/@kullanici/video/123");
            }

            Console.Write("Scroll sayisi (varsayilan 5): ");
            var scrollInput = Console.ReadLine();
            var scrollCount = 5;
            if (!string.IsNullOrWhiteSpace(scrollInput) && int.TryParse(scrollInput, out var parsedScroll) && parsedScroll > 0)
            {
                scrollCount = parsedScroll;
            }

            Console.Write("Ayni kisi tek hak olsun mu? (E/H): ");
            var uniqueByUserInput = (Console.ReadLine() ?? string.Empty).Trim();
            var uniqueByUser = uniqueByUserInput.Equals("E", StringComparison.OrdinalIgnoreCase);

            Console.Write("Maksimum yorum sayisi (bos = sinirsiz): ");
            var maxCountInput = (Console.ReadLine() ?? string.Empty).Trim();
            int? maxComments = null;
            if (int.TryParse(maxCountInput, out var parsedMax) && parsedMax > 0)
            {
                maxComments = parsedMax;
            }

            Console.WriteLine("Tarayici baslatiliyor...");

            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = false
            });

            var page = await browser.NewPageAsync();
            await page.GotoAsync(videoUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            await page.WaitForTimeoutAsync(3000);

            // Kisa yol ipucu popupini kapat (varsa)
            try
            {
                await page.Locator("button:has-text(\"×\")").First.ClickAsync(new LocatorClickOptions { Timeout = 2000 });
            }
            catch { }

            // Yorumlar sekmesini aktive et (varsa)
            try
            {
                await page.Locator("text=Yorumlar").First.ClickAsync(new LocatorClickOptions { Timeout = 2000 });
            }
            catch { }

            Console.WriteLine("Gerekirse tarayicida oturum ac. Devam etmek icin Enter...");
            Console.ReadLine();

            var allComments = new List<CommentItem>();
            var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var noNewRounds = 0;
            for (int i = 0; i < scrollCount; i++)
            {
                var batch = await page.EvaluateAsync<CommentItem[]>(@"
() => {
  const nodes = Array.from(document.querySelectorAll(
    '[data-e2e=""comment-level-1""], [data-e2e=""comment-item""], [data-e2e=""comment-list""] [data-e2e=""comment-item""], [data-e2e=""comment-list""] div'
  ));
  return nodes.map(n => {
    const textEl =
      n.querySelector('[data-e2e=""comment-content""]') ||
      n.querySelector('span[data-e2e=""comment-text""]') ||
      n.querySelector('p') ||
      n;
    const userEl =
      n.querySelector('a[href*=""@""]') ||
      n.querySelector('a[data-e2e*=""comment""]') ||
      n.querySelector('a');
    const userText = userEl ? userEl.textContent.trim() : '';
    return {
      text: textEl ? textEl.innerText.trim() : '',
      user: userText.replace(/^@/, '')
    };
  });
}
");

                foreach (var item in batch)
                {
                    if (string.IsNullOrWhiteSpace(item?.text))
                    {
                        continue;
                    }

                    var key = $"{item.user}|{item.text}";
                    if (seenKeys.Add(key))
                    {
                        allComments.Add(item);
                    }
                }

                if (batch.Length == 0 || allComments.Count == 0)
                {
                    noNewRounds++;
                }
                else
                {
                    noNewRounds = 0;
                }

                if (maxComments.HasValue && allComments.Count >= maxComments.Value)
                {
                    Console.WriteLine("Maksimum yorum sayisina ulasildi.");
                    break;
                }

                if (noNewRounds >= 3)
                {
                    Console.WriteLine("Yeni yorum gelmedi, durduruluyor.");
                    break;
                }

                // Yorum panelini scroll et (yoksa sayfayi scroll et)
                await page.EvaluateAsync(@"() => {
  const panel =
    document.querySelector('[data-e2e=""comment-list""]') ||
    document.querySelector('div[role=""dialog""] [data-e2e=""comment-list""]') ||
    document.querySelector('div[aria-label*=""Yorum""]');
  if (panel) {
    panel.scrollBy(0, 1200);
  } else {
    window.scrollBy(0, 1200);
  }
}");
                await page.WaitForTimeoutAsync(1500);

                Console.WriteLine($"Scroll {i + 1}/{scrollCount} - Toplanan yorum: {allComments.Count}");
            }

            var validComments = allComments
                .Where(c => c.text.Contains("@"))
                .ToList();

            Console.WriteLine();
            Console.WriteLine($"Toplam yorum: {allComments.Count}");
            Console.WriteLine($"Etiketli yorum: {validComments.Count}");

            if (uniqueByUser)
            {
                var users = validComments
                    .Where(c => !string.IsNullOrWhiteSpace(c.user))
                    .Select(c => c.user.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                Console.WriteLine($"Tek hak katilimci sayisi: {users.Count}");

                Console.Write("Kazanan secilsin mi? (E/H): ");
                var pickInput = (Console.ReadLine() ?? string.Empty).Trim();
                if (pickInput.Equals("E", StringComparison.OrdinalIgnoreCase) && users.Count > 0)
                {
                    var rng = new Random();
                    var winner = users[rng.Next(users.Count)];
                    Console.WriteLine($"Kazanan: @{winner}");
                }
            }
            else
            {
                Console.WriteLine("Katilimlar:");
                foreach (var comment in validComments)
                {
                    var userLabel = string.IsNullOrWhiteSpace(comment.user) ? "unknown" : comment.user;
                    Console.WriteLine($"@{userLabel}: {comment.text}");
                }

                Console.Write("Kazanan secilsin mi? (E/H): ");
                var pickInput = (Console.ReadLine() ?? string.Empty).Trim();
                if (pickInput.Equals("E", StringComparison.OrdinalIgnoreCase) && validComments.Count > 0)
                {
                    var rng = new Random();
                    var winner = validComments[rng.Next(validComments.Count)];
                    var label = string.IsNullOrWhiteSpace(winner.user) ? "unknown" : winner.user;
                    Console.WriteLine($"Kazanan: @{label} - {winner.text}");
                }
            }

            Console.WriteLine();
            Console.WriteLine("Bitirdi. Cikmak icin Enter...");
            Console.ReadLine();
        }
    }
}
