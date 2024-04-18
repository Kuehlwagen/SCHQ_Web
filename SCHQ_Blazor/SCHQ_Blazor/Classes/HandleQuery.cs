using SCHQ_Protos;
using System.Globalization;
using System.Net;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Web;

namespace SCHQ_Blazor.Classes;
public static partial class HandleQuery {

  #region Regex
  private static readonly Regex RgxIdCmHandleEnlistedFluency = RgxIdCmHandleEnlistedFluencyMethod();
  private static readonly Regex RgxLocation = RgxLocationMethod();
  private static readonly Regex RgxAvatar = RgxAvatarMethod();
  private static readonly Regex RgxDisplayTitle = RgxDisplayTitleMethod();
  private static readonly Regex RgxMainOrganization = RgxMainOrganizationMethod();
  private static readonly Regex RgxOrganizationStars = RgxOrganizationStarsMethod();
  private static readonly Regex RgxOrganization = RgxOrganizationMethod();

  [GeneratedRegex("<strong class=\"value\">(.+)</strong>", RegexOptions.Multiline | RegexOptions.Compiled)]
  private static partial Regex RgxIdCmHandleEnlistedFluencyMethod();
  [GeneratedRegex("<span class=\"label\">Location</span>\\s+<strong class=\"value\">(.+)</strong>\\s+</p>\\s+<p class=\"entry\">", RegexOptions.Compiled | RegexOptions.Singleline)]
  private static partial Regex RgxLocationMethod();
  [GeneratedRegex("<div class=\"thumb\">\\s+<img src=\"(.+)\" \\/>", RegexOptions.Multiline | RegexOptions.Compiled)]
  private static partial Regex RgxAvatarMethod();
  [GeneratedRegex("<span class=\"icon\">\\s+<img src=\"(.+)\"\\/>\\s+<\\/span>\\s+<span class=\"value\">(.+)<", RegexOptions.Multiline | RegexOptions.Compiled)]
  private static partial Regex RgxDisplayTitleMethod();
  [GeneratedRegex("<a href=\"\\/orgs\\/(.+)\"><img src=\"(.+)\" \\/><\\/a>\\s+<span class=\"members\">(\\d+) members<\\/span>[\\W\\w]+class=\"value\">(.+)<\\/a>[\\W\\w]+Organization rank<\\/span>\\s+<strong class=\"value\">(.+)<\\/strong>[\\W\\w]+Prim. Activity<\\/span>\\s+<strong class=\"value\">(.+)<\\/strong>[\\W\\w]+Sec. Activity<\\/span>\\s+<strong class=\"value\">(.+)<\\/strong>[\\W\\w]+Commitment<\\/span>\\s+<strong class=\"value\">(.+)<\\/strong>", RegexOptions.Multiline | RegexOptions.Compiled)]
  private static partial Regex RgxMainOrganizationMethod();
  [GeneratedRegex("<span class=\"active\">", RegexOptions.Multiline | RegexOptions.Compiled)]
  private static partial Regex RgxOrganizationStarsMethod();
  [GeneratedRegex("<a href=\"\\/orgs\\/(.+)\"><img src=\"(.+)\" \\/><\\/a>\\s+<span class=\"members\">(\\d+) members<\\/span>[\\W\\w]+class=\"value\\s[\\w\\d]*\">(.+)<\\/a>[\\W\\w]+Organization rank<\\/span>\\s+<strong class=\"value\\s[\\w\\d]*\">(.+)<\\/strong>", RegexOptions.Multiline | RegexOptions.Compiled)]
  private static partial Regex RgxOrganizationMethod();
  #endregion

  private static CancellationTokenSource? CancelToken;

  public static async Task<HandleInfo> GetHandleInfo(string handle) {
    CancelToken = new();
    HandleInfo rtnVal = new() {
      Profile = new() {
        Handle = handle,
        Url = $"https://robertsspaceindustries.com/citizens/{handle}"
      }
    };

    if (!string.IsNullOrWhiteSpace(handle)) {
      rtnVal.HttpResponse = await GetRSISource(rtnVal.Profile.Url, CancelToken);
      if (rtnVal.HttpResponse.StatusCode == HttpStatusCode.OK && rtnVal.HttpResponse.Source != null) {

        // UEE Citizen Record, Community Monicker, Handle, Enlisted, Fluency
        MatchCollection mcIdCmHandleEnlistedFluency = RgxIdCmHandleEnlistedFluency.Matches(rtnVal.HttpResponse.Source);
        if (mcIdCmHandleEnlistedFluency.Count >= 5) {
          rtnVal.Profile.UeeCitizenRecord = mcIdCmHandleEnlistedFluency[0].Groups[1].Value;
          rtnVal.Profile.CommunityMonicker = CorrectText(mcIdCmHandleEnlistedFluency[1].Groups[1].Value);
          rtnVal.Profile.Handle = CorrectText(mcIdCmHandleEnlistedFluency[2].Groups[1].Value);
          if (DateTime.TryParseExact(mcIdCmHandleEnlistedFluency[3].Groups[1].Value, "MMM d, yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime enlisted)) {
            rtnVal.Profile.Enlisted = enlisted;
          }
          rtnVal.Profile.Fluency.AddRange(mcIdCmHandleEnlistedFluency[4].Groups[1].Value.Replace(" ", string.Empty).Split(","));
        }

        Match matchLocation = RgxLocation.Match(rtnVal.HttpResponse.Source);
        if (matchLocation?.Groups.Count > 1) {
          string[] countryRegion = matchLocation.Groups[1].Value.Split(",");
          rtnVal.Profile.Country = countryRegion[0].Trim();
          rtnVal.Profile.Region = countryRegion.Length > 1 ? countryRegion[1].Trim() : string.Empty;
        }

        // Avatar
        MatchCollection mcAvatar = RgxAvatar.Matches(rtnVal.HttpResponse.Source);
        if (mcAvatar.Count == 1) {
          rtnVal.Profile.AvatarUrl = CorrectUrl(mcAvatar[0].Groups[1].Value);
        }

        // Display Title
        MatchCollection mcDisplayTitle = RgxDisplayTitle.Matches(rtnVal.HttpResponse.Source);
        if (mcDisplayTitle.Count == 1 && mcDisplayTitle[0].Groups.Count == 3) {
          rtnVal.Profile.DisplayTitle = CorrectText(mcDisplayTitle[0].Groups[2].Value);
          rtnVal.Profile.DisplayTitleAvatarUrl = CorrectUrl(mcDisplayTitle[0].Groups[1].Value);
        }

        // Organizations
        rtnVal.Organizations = await GetOrganizationsInfo(handle);

      } else {
        rtnVal.HttpResponse.StatusCode = rtnVal.HttpResponse.StatusCode != null ? rtnVal.HttpResponse.StatusCode : HttpStatusCode.InternalServerError;
        rtnVal.HttpResponse.ErrorText = rtnVal.HttpResponse.ErrorText;
      }
    } else {
      rtnVal.HttpResponse = new() {
        StatusCode = HttpStatusCode.BadRequest,
        ErrorText = $"{HttpStatusCode.BadRequest}: No handle provided"
      };
    }

    rtnVal ??= new();

    return rtnVal;
  }

  private static async Task<OrganizationsInfo> GetOrganizationsInfo(string handle) {
    OrganizationsInfo reply = new();

    HttpInfo httpInfo = await GetRSISource($"https://robertsspaceindustries.com/citizens/{handle}/organizations", CancelToken);
    if (httpInfo.StatusCode.HasValue && httpInfo.StatusCode == HttpStatusCode.OK && httpInfo.Source != null) {

      string[] organizations = httpInfo.Source.Split("<div class=\"title\">");
      if (organizations.Length > 1) {

        foreach (string organization in organizations) {
          if (organization.StartsWith("Main organization")) {

            // Main Organization
            MatchCollection mcMainOrganization = RgxMainOrganization.Matches(organization);
            if (mcMainOrganization.Count == 1 && mcMainOrganization[0].Groups.Count == 9) {
              reply.MainOrganization = new() {
                Url = $"https://robertsspaceindustries.com/orgs/{mcMainOrganization[0].Groups[1].Value}",
                Sid = CorrectText(mcMainOrganization[0].Groups[1].Value),
                AvatarUrl = CorrectUrl(mcMainOrganization[0].Groups[2].Value),
                Members = Convert.ToInt32(mcMainOrganization[0].Groups[3].Value),
                Name = CorrectText(mcMainOrganization[0].Groups[4].Value),
                RankName = CorrectText(mcMainOrganization[0].Groups[5].Value),
                PrimaryActivity = CorrectText(mcMainOrganization[0].Groups[6].Value),
                SecondaryActivity = CorrectText(mcMainOrganization[0].Groups[7].Value),
                Commitment = CorrectText(mcMainOrganization[0].Groups[8].Value),
                Redacted = false
              };
              // Main Organization Rank Stars
              MatchCollection mcMainOrganizationRankStars = RgxOrganizationStars.Matches(organization);
              reply.MainOrganization.RankStars = mcMainOrganizationRankStars.Count;
            } else {
              reply.MainOrganization = new() {
                Redacted = true
              };
            }

          } else if (organization.StartsWith("Affiliation")) {

            // Affiliation
            MatchCollection mcOrganization = RgxOrganization.Matches(organization);
            reply.Affiliations ??= [];
            if (mcOrganization.Count > 0 && mcOrganization[0].Groups.Count == 6) {
              reply.Affiliations.Add(new OrganizationInfo() {
                Url = $"https://robertsspaceindustries.com/orgs/{mcOrganization[0].Groups[1].Value}",
                Sid = CorrectText(mcOrganization[0].Groups[1].Value),
                AvatarUrl = CorrectUrl(mcOrganization[0].Groups[2].Value),
                Members = Convert.ToInt32(mcOrganization[0].Groups[3].Value),
                Name = CorrectText(mcOrganization[0].Groups[4].Value),
                RankName = CorrectText(mcOrganization[0].Groups[5].Value.Replace("&", "&&")),
                Redacted = false
              });
              // Affiliation Rank Stars
              MatchCollection mcAffiliationRankStars = RgxOrganizationStars.Matches(organization);
              reply.Affiliations[^1].RankStars = mcAffiliationRankStars.Count;
            } else {
              reply.Affiliations.Add(new OrganizationInfo() {
                Redacted = true
              });
            }

          }
        }

      }

    }

    return reply;
  }

  private static string CorrectText(string text) {
    return HttpUtility.HtmlDecode(text);
  }

  private static async Task<HttpInfo> GetSource(string url, CancellationTokenSource? cancellationToken) {
    HttpInfo rtnVal = new();

    using HttpClient client = new() {
      Timeout = TimeSpan.FromSeconds(10)
    };
    try {
      rtnVal.Source = await client.GetStringAsync(url, cancellationToken?.Token ?? new()).ConfigureAwait(false);
      rtnVal.StatusCode = HttpStatusCode.OK;
    } catch (HttpRequestException ex) {
      rtnVal.Source = string.Empty;
      rtnVal.ErrorText = $"{ex.StatusCode}: {ex.Message}";
      rtnVal.StatusCode = ex.StatusCode;
    } catch (OperationCanceledException ex) {
      rtnVal.Source = string.Empty;
      rtnVal.ErrorText = ex.Message;
      rtnVal.StatusCode = HttpStatusCode.BadGateway;
    }

    return rtnVal;
  }

  private static async Task<HttpInfo> GetRSISource(string url, CancellationTokenSource? cancellationToken, bool isCommunityHub = false) {
    HttpInfo rtnVal = await GetSource(url, cancellationToken);

    if (rtnVal.StatusCode == HttpStatusCode.OK && rtnVal.Source != null) {
      if (!isCommunityHub) {
        int index = rtnVal.Source.IndexOf("<div class=\"page-wrapper\">");
        if (index >= 0) {
          rtnVal.Source = rtnVal.Source[index..];
        }
        index = rtnVal.Source.IndexOf("<script type=\"text/plain\" data-cookieconsent=\"statistics\">");
        if (index >= 0) {
          rtnVal.Source = rtnVal.Source[..index];
        }
      } else {
        int index = rtnVal.Source.IndexOf("{\"props\":");
        if (index >= 0) {
          rtnVal.Source = rtnVal.Source[index..];
        }
        index = rtnVal.Source.IndexOf("</script></body></html>");
        if (index >= 0) {
          rtnVal.Source = rtnVal.Source[..index];
        }
      }
    }

    return rtnVal;
  }

  private static string CorrectUrl(string url) {
    return url.StartsWith('/') ? $"https://robertsspaceindustries.com{url}" : url;
  }

}

#region Serialization
public class HandleInfo {

  [JsonIgnore]
  public HttpInfo? HttpResponse { get; set; }
  public HandleProfileInfo? Profile { get; set; }
  public OrganizationsInfo? Organizations { get; set; }
  public string? Comment { get; set; }
  public RelationValue Relation { get; set; }

}

public class HttpInfo {

  public HttpStatusCode? StatusCode { get; set; }
  public string? ErrorText { get; set; }
  public string? Source { get; set; }

}

public class HandleProfileInfo {

  public string? UeeCitizenRecord { get; set; }
  public string? Handle { get; set; }
  public string? CommunityMonicker { get; set; }
  public DateTime Enlisted { get; set; }
  public string? Url { get; set; }
  public string? AvatarUrl { get; set; }
  public string? DisplayTitle { get; set; }
  public string? DisplayTitleAvatarUrl { get; set; }
  public string? Country { get; set; }
  public string? Region { get; set; }
  public List<string> Fluency { get; set; } = [];

}

public class OrganizationsInfo {

  public OrganizationInfo? MainOrganization { get; set; }
  public List<OrganizationInfo>? Affiliations { get; set; }

}

public class OrganizationInfo {

  public bool Redacted { get; set; }
  public string? RankName { get; set; }
  public int? RankStars { get; set; }
  public string? Sid { get; set; }
  public string? Name { get; set; }
  public string? Url { get; set; }
  public string? AvatarUrl { get; set; }
  public int? Members { get; set; }
  public string? PrimaryActivity { get; set; }
  public string? SecondaryActivity { get; set; }
  public string? Commitment { get; set; }

}
#endregion
