using System.Globalization;
using CsvHelper;
using Microsoft.AspNetCore.Mvc;
using Recomendation.Models;

namespace Recomendation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecommendationsController : ControllerBase
    {
        private readonly string _collaborativePath = "python/collaborative_recommendations.csv";
        private readonly string _contentPath = "python/content_filtering_results.csv";
        private readonly int _topN = 5;

        [HttpPost]
        public IActionResult Post([FromBody] RecommendationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ContentId))
                return BadRequest("contentId is required.");

            if (!TryParseContentId(request.ContentId, out long contentId))
                return BadRequest("Invalid contentId format.");

            var collaborative = GetCollaborativeRecommendations(contentId);
            var content = GetContentBasedRecommendations(contentId);

            return Ok(new
            {
                collaborative,
                content,
                azure = new List<string>() // Placeholder
            });
        }

        private bool TryParseContentId(string input, out long contentId)
        {
            input = input.Trim();
            return long.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out contentId) ||
                   decimal.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal dec) &&
                   long.TryParse(dec.ToString(CultureInfo.InvariantCulture), out contentId);
        }

        private List<long> GetCollaborativeRecommendations(long contentId)
        {
            var results = new List<long>();
            using var reader = new StreamReader(_collaborativePath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            var records = csv.GetRecords<CollaborativeRecord>();

            foreach (var record in records)
            {
                if (record.input_contentId == contentId)
                {
                    results.Add(record.recommended_contentId);
                    if (results.Count >= _topN)
                        break;
                }
            }
            return results;
        }

        private List<long> GetContentBasedRecommendations(long contentId)
        {
            var results = new List<(long id, double score)>();
            using var reader = new StreamReader(_contentPath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            csv.Read();
            csv.ReadHeader();
            var headers = csv.HeaderRecord.Skip(1).ToList(); // skip "contentId"

            while (csv.Read())
            {
                var rowContentIdStr = csv.GetField(0);
                if (long.TryParse(rowContentIdStr, out long rowContentId) && rowContentId == contentId)
                {
                    for (int i = 1; i < headers.Count + 1; i++)
                    {
                        string colId = headers[i - 1];
                        string cellValue = csv.GetField(i);
                        if (double.TryParse(cellValue, out double score))
                        {
                            if (long.TryParse(colId, out long colIdParsed))
                                results.Add((colIdParsed, score));
                        }
                    }
                    break; // Only one row will match
                }
            }

            return results.OrderByDescending(x => x.score)
                           .Take(_topN)
                           .Select(x => x.id)
                           .ToList();
        }
    }
}
