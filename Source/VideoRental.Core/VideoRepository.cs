using HtmlAgilityPack;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

namespace VideoRental.Core
{
    public class VideoRepository : IVideoRepository
    {
        private readonly BlurayRentalHttpClient _httpClient;
        
        public VideoRepository(BlurayRentalHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public IEnumerable<Video> GetAll(int category, int pageIndex, int sortBy, int maxResults)
        {
            var document = new HtmlDocument();
            document.Load(_httpClient.GetVideosStreamAsync(category, pageIndex, sortBy, maxResults).Result);

            // Get the table by class name
            var rows = document.DocumentNode.Descendants("table")
                .LastOrDefault(n => n.Attributes.Any(a => a.Name == "class" && a.Value != null && a.Value.EndsWith("productDisplay")))
                .Descendants("tr");

            var enumerator = rows.GetEnumerator();

            // Every item has 6 rows
            while (enumerator.MoveNext())
            {
                // Row #1
                // Prety much everything we need is in the anchor tag
                var anchor = enumerator.Current.Descendants("a").First();
                var id = Path.GetFileNameWithoutExtension(anchor.GetAttributeValue("href", default(string)));
                var title = CreateTitleLine(anchor.GetAttributeValue("title", default(string)));

                // Row #2
                // "new" icon indicates movie is new
                enumerator.MoveNext();
                var isNew = enumerator.Current.Descendants("img")
                    .FirstOrDefault(n => n.Attributes.Any(a => a.Name == "src" && a.Value != null && a.Value.EndsWith("Icon_New.gif"))) != null;

                // Skip the rest of the rows
                for (var i = 0; i < 5; i++)
                    enumerator.MoveNext();

                yield return new Video(id, title.Title(), title.Format(), isNew, title.Preorder()); ;
            }
        }

        private ITitleLine CreateTitleLine(string line)
        {
            var titleLine = TitleLine.Create(line);

            if (titleLine != null)
                return titleLine;

            // todo: log when this happens?

            return new FallbackTitleLine(line);
        }

        private interface ITitleLine
        {
            string Title();
            string Format();
            Preorder Preorder();
        }

        private class TitleLine : ITitleLine
        {
            private static readonly Regex _regex;

            private readonly Match _match;

            static TitleLine()
            {
                var options = RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase;

                _regex = new Regex(@"^(\(Pre-order - ships (?<ship>\d{2}/\d{2}/\d{2})\)\s)?(?<title>.*?)(\s(?<format>(4K UHD|4K|3D|PS4|Xbox One)))?(\s\d{2}/\d{2})?\sBlu-ray \(Rental\)$", options);


                //Avengers: Endgame 4K UHD 07/19 Blu-ray (Rental)
                //Avengers: Endgame 07/19 Blu-ray (Rental)
                //Avengers: Endgame 3D Blu-ray (Rental)     // not free

                //(Pre-order - ships 10/15/19) Stuber 4K UHD 09/19 Blu-ray (Rental)
                //(Pre-order - ships 10/15/19) Stuber 09/19 Blu-ray (Rental)

                // Sometimes "UHD" isnt present
                //Gremlins 4K 06/19 Blu-ray (Rental)
            }

            private TitleLine(Match match)
            {
                if (!match.Success)
                    throw new ArgumentException("Match must be successful.");

                _match = match;
            }

            public static TitleLine Create(string line)
            {
                var match = _regex.Match(line);

                if (match.Success)
                    return new TitleLine(match);

                return null;
            }


            public string Title()
            {
                return _match.Groups["title"].Value;
            }

            public string Format()
            {
                if (_match.Groups["format"].Success)
                {
                    switch (_match.Groups["format"].Value)
                    {
                        case var format when format.StartsWith("4K", StringComparison.OrdinalIgnoreCase):
                            return "4K";

                        case var format when format.Equals("3D", StringComparison.OrdinalIgnoreCase):
                            return "3D";

                        case var format when format.Equals("PS4", StringComparison.OrdinalIgnoreCase):
                            return "PS4";

                        case var format when format.Equals("Xbox One", StringComparison.OrdinalIgnoreCase):
                            return "Xbox One";
                    }
                }

                return "Blu-ray";
            }

            public Preorder Preorder()
            {
                if (_match.Groups["ship"].Success)
                    return new Preorder(DateTime.Parse(_match.Groups["ship"].Value));

                return null;
            }
        }

        private class FallbackTitleLine : ITitleLine
        {
            private readonly string _line;

            public FallbackTitleLine(string line)
            {
                _line = line;
            }

            public string Title()
            {
                return _line;
            }

            public string Format()
            {
                return "Blu-ray";
            }

            public Preorder Preorder()
            {
                return null;
            }
        }
    }
}
