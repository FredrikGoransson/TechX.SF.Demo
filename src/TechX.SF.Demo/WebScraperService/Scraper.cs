using System;
using System.Collections.Generic;
using System.Fabric;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace WebScraperService
{
	public class Scraper
	{
		private StatelessServiceContext Context { get; }

		public Scraper(StatelessServiceContext context)
		{
			Context = context;
		}

		internal static string GetShortHash(string input)
		{
			var md5 = System.Security.Cryptography.MD5.Create();
			var inputBytes = System.Text.Encoding.ASCII.GetBytes(input);

			var hash = md5.ComputeHash(inputBytes);
			var sb = new StringBuilder();
			foreach (var t in hash)
			{
				sb.Append(t.ToString("X2"));
			}
			return sb.ToString();
		}

		public async Task<IEnumerable<Speaker>> ScanSpeakers()
		{
			var speakers = new List<Speaker>();
			try
			{
				var httpClient = new HttpClient();
				var page = await httpClient.GetStringAsync("http://www.techx.se/talare/");

				var htmlDocument = new HtmlAgilityPack.HtmlDocument();
				htmlDocument.LoadHtml(page);

				var speakersDivs = htmlDocument.DocumentNode.SelectNodes("//div/div[2]/div");
				foreach (var speakerDiv in speakersDivs)
				{
					var nameDiv = speakerDiv?.SelectSingleNode("h5");
					var bioDiv = speakerDiv?.SelectSingleNode("p");
					if (nameDiv != null)
					{
						var speaker = new Speaker
						{
							Name = nameDiv.InnerText,
							Bio = bioDiv.InnerText
						};
						speakers.Add(speaker);
					}
				}
			}
			catch (Exception ex)
			{
				ServiceEventSource.Current.ServiceMessage(this.Context, $"Failed to scrape speakers: {ex.Message}");
			}
			return speakers;
		}

		public async Task<IEnumerable<Session>> ScanSessions()
		{
			// //*[@id=\"wrap\"]/div/table/tbody/tr/td

			var pageUris = new string[]
			{
				"http://www.techx.se/agenda/",
				"http://www.techx.se/agenda-14-feb/",
				"http://www.techx.se/agenda-15-feb/",
				"http://www.techx.se/agenda-16-feb/",
				"http://www.techx.se/agenda17-februari/"
			};
			var days = new int[] { 13, 14, 15, 16, 17 };
			var pages = pageUris.Select((pageUri, i) => new { PageUri = pageUri, Date = new DateTime(2017, 02, days[i]) });

			var sessions = new List<Session>();
			foreach (var page in pages)
			{
				try
				{
					var httpClient = new HttpClient();
					var pageDocument = await httpClient.GetStringAsync(page.PageUri);

					var htmlDocument = new HtmlAgilityPack.HtmlDocument();
					htmlDocument.LoadHtml(pageDocument);

					var sessionsDivs = htmlDocument.DocumentNode.SelectNodes("//*[@id=\"wrap\"]/div/table/tbody/tr/td");
					foreach (var sessionDiv in sessionsDivs)
					{
						if (!(sessionDiv.ChildNodes.Where(n => (n.NodeType == HtmlNodeType.Element))?.Select(n => n.InnerText)).Any())
						{
							continue;
						}

						var contentLines = sessionDiv.ChildNodes.Select(n => n.InnerText.Replace("\n", "")).Where(t => (t != "\n") && !string.IsNullOrWhiteSpace(t)).ToArray();

						if (contentLines.Length >= 5)
						{
							var sessionName = contentLines[0].Trim();
							var nameAndCompany = contentLines[contentLines.Length - 3].Replace("Talare: ", "").Trim();
							var levelString = contentLines[contentLines.Length - 2].Replace("Level: ", "").Trim() ?? "100";
							var level = 100;
							int.TryParse(levelString, NumberStyles.Integer, CultureInfo.InvariantCulture, out level);
							var target = contentLines[contentLines.Length - 1].Replace("Målgrupp: ", "").Trim().Split(',').Select(t => t.Trim()).ToArray();
							var content = contentLines.Where((cl, i) => (i > 0) && (i < contentLines.Length - 2)).Aggregate("", (aggregate, cl) => aggregate + "\r\n" + cl);
							if (!string.IsNullOrWhiteSpace(sessionName) && !string.IsNullOrWhiteSpace(nameAndCompany) && !string.IsNullOrWhiteSpace(content))
							{
								var session = new Session()
								{
									Title = sessionName,
									Speakers = nameAndCompany,
									Content = content,
									Date = page.Date,
									Level = level,
									Target = target,
								};
								sessions.Add(session);
							}
						}

					}
				}
				catch (Exception ex)
				{
					ServiceEventSource.Current.ServiceMessage(this.Context, $"Failed to scrape agenda: {ex.Message}");
				}
			}
			return sessions;

		}

		public void AddSessionsToSpeakers(IEnumerable<Speaker> speakers, IEnumerable<Session> sessions)
		{
			foreach (var session in sessions)
			{
				var sessionSpeakers = speakers.Where(speaker => session.Speakers.ToLowerInvariant().Contains(speaker.Name.ToLowerInvariant())).ToArray();
				if (sessionSpeakers.Length == 0)
				{
					ServiceEventSource.Current.ServiceMessage(this.Context, $"Failed to find speaker(s) {session.Speakers} for sessions");
					continue;
				}

				foreach (var speaker in sessionSpeakers)
				{
					speaker.Sessions.Add(session);
				}
			}
		}

		public class Session
		{
			public string[] Target { get; set; }
			public int Level { get; set; }
			public string Title { get; set; }
			public string Content { get; set; }
			public string Speakers { get; set; }
			public DateTime Date { get; set; }

			public string GetShortHash()
			{
				return Scraper.GetShortHash($"{Title}-{Content}-{Speakers}-{Date.ToShortDateString()}");
			}
		}

		public class Speaker
		{
			public Speaker()
			{
				Sessions = new List<Session>();
			}

			public string Name { get; set; }
			public string Bio { get; set; }

			public List<Session> Sessions { get; set; }

			public string GetShortHash()
			{
				return Scraper.GetShortHash($"{Name}-{Bio}");
			}
		}
	}
}