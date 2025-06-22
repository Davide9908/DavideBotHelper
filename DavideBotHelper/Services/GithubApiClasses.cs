using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DavideBotHelper.Services;


#pragma warning disable CS8618
#pragma warning disable CS8601
#pragma warning disable CS8603
    public class GithubReleases
    {
        [JsonPropertyName("assets")] public List<ReleaseAsset> Assets { get; set; }

        [JsonPropertyName("assets_url")] public Uri AssetsUrl { get; set; }

        /// <summary>
        /// A GitHub user.
        /// </summary>
        [JsonPropertyName("author")]
        public AuthorClass Author { get; set; }

        [JsonPropertyName("body")] public string Body { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("body_html")]
        public string BodyHtml { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("body_text")]
        public string BodyText { get; set; }

        [JsonPropertyName("created_at")] public DateTimeOffset CreatedAt { get; set; }

        /// <summary>
        /// The URL of the release discussion.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("discussion_url")]
        public Uri DiscussionUrl { get; set; }

        /// <summary>
        /// true to create a draft (unpublished) release, false to create a published one.
        /// </summary>
        [JsonPropertyName("draft")]
        public bool Draft { get; set; }

        [JsonPropertyName("html_url")] public Uri HtmlUrl { get; set; }

        [JsonPropertyName("id")] public long Id { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("mentions_count")]
        public long? MentionsCount { get; set; }

        [JsonPropertyName("name")] public string Name { get; set; }

        [JsonPropertyName("node_id")] public string NodeId { get; set; }

        /// <summary>
        /// Whether to identify the release as a prerelease or a full release.
        /// </summary>
        [JsonPropertyName("prerelease")]
        public bool Prerelease { get; set; }

        [JsonPropertyName("published_at")] public DateTimeOffset? PublishedAt { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("reactions")]
        public ReactionRollup Reactions { get; set; }

        /// <summary>
        /// The name of the tag.
        /// </summary>
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; }
        
        [JsonIgnore]
        public string TagNameNoV => TagName.Replace("v", string.Empty, StringComparison.InvariantCultureIgnoreCase);

        [JsonPropertyName("tarball_url")] public Uri TarballUrl { get; set; }

        /// <summary>
        /// Specifies the commitish value that determines where the Git tag is created from.
        /// </summary>
        [JsonPropertyName("target_commitish")]
        public string TargetCommitish { get; set; }

        [JsonPropertyName("upload_url")] public string UploadUrl { get; set; }

        [JsonPropertyName("url")] public Uri Url { get; set; }

        [JsonPropertyName("zipball_url")] public Uri ZipballUrl { get; set; }
    }

    /// <summary>
    /// Data related to a release.
    /// </summary>
    public class ReleaseAsset
    {
        [JsonPropertyName("browser_download_url")]
        public Uri BrowserDownloadUrl { get; set; }

        [JsonPropertyName("content_type")] public string ContentType { get; set; }

        [JsonPropertyName("created_at")] public DateTimeOffset CreatedAt { get; set; }

        [JsonPropertyName("digest")] public string Digest { get; set; }

        [JsonPropertyName("download_count")] public long DownloadCount { get; set; }

        [JsonPropertyName("id")] public long Id { get; set; }

        [JsonPropertyName("label")] public string Label { get; set; }

        /// <summary>
        /// The file name of the asset.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("node_id")] public string NodeId { get; set; }

        [JsonPropertyName("size")] public long Size { get; set; }

        /// <summary>
        /// State of the release asset.
        /// </summary>
        [JsonPropertyName("state")]
        public State State { get; set; }

        [JsonPropertyName("updated_at")] public DateTimeOffset UpdatedAt { get; set; }

        [JsonPropertyName("uploader")] public SimpleUser Uploader { get; set; }

        [JsonPropertyName("url")] public Uri Url { get; set; }
    }

    /// <summary>
    /// A GitHub user.
    /// </summary>
    public class SimpleUser
    {
        [JsonPropertyName("avatar_url")] public Uri AvatarUrl { get; set; }

        [JsonPropertyName("email")] public string Email { get; set; }

        [JsonPropertyName("events_url")] public string EventsUrl { get; set; }

        [JsonPropertyName("followers_url")] public Uri FollowersUrl { get; set; }

        [JsonPropertyName("following_url")] public string FollowingUrl { get; set; }

        [JsonPropertyName("gists_url")] public string GistsUrl { get; set; }

        [JsonPropertyName("gravatar_id")] public string GravatarId { get; set; }

        [JsonPropertyName("html_url")] public Uri HtmlUrl { get; set; }

        [JsonPropertyName("id")] public long Id { get; set; }

        [JsonPropertyName("login")] public string Login { get; set; }

        [JsonPropertyName("name")] public string Name { get; set; }

        [JsonPropertyName("node_id")] public string NodeId { get; set; }

        [JsonPropertyName("organizations_url")]
        public Uri OrganizationsUrl { get; set; }

        [JsonPropertyName("received_events_url")]
        public Uri ReceivedEventsUrl { get; set; }

        [JsonPropertyName("repos_url")] public Uri ReposUrl { get; set; }

        [JsonPropertyName("site_admin")] public bool SiteAdmin { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("starred_at")]
        public string StarredAt { get; set; }

        [JsonPropertyName("starred_url")] public string StarredUrl { get; set; }

        [JsonPropertyName("subscriptions_url")]
        public Uri SubscriptionsUrl { get; set; }

        [JsonPropertyName("type")] public string Type { get; set; }

        [JsonPropertyName("url")] public Uri Url { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("user_view_type")]
        public string UserViewType { get; set; }
    }

    /// <summary>
    /// A GitHub user.
    /// </summary>
    public class AuthorClass
    {
        [JsonPropertyName("avatar_url")] public Uri AvatarUrl { get; set; }

        [JsonPropertyName("email")] public string Email { get; set; }

        [JsonPropertyName("events_url")] public string EventsUrl { get; set; }

        [JsonPropertyName("followers_url")] public Uri FollowersUrl { get; set; }

        [JsonPropertyName("following_url")] public string FollowingUrl { get; set; }

        [JsonPropertyName("gists_url")] public string GistsUrl { get; set; }

        [JsonPropertyName("gravatar_id")] public string GravatarId { get; set; }

        [JsonPropertyName("html_url")] public Uri HtmlUrl { get; set; }

        [JsonPropertyName("id")] public long Id { get; set; }

        [JsonPropertyName("login")] public string Login { get; set; }

        [JsonPropertyName("name")] public string Name { get; set; }

        [JsonPropertyName("node_id")] public string NodeId { get; set; }

        [JsonPropertyName("organizations_url")]
        public Uri OrganizationsUrl { get; set; }

        [JsonPropertyName("received_events_url")]
        public Uri ReceivedEventsUrl { get; set; }

        [JsonPropertyName("repos_url")] public Uri ReposUrl { get; set; }

        [JsonPropertyName("site_admin")] public bool SiteAdmin { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("starred_at")]
        public string StarredAt { get; set; }

        [JsonPropertyName("starred_url")] public string StarredUrl { get; set; }

        [JsonPropertyName("subscriptions_url")]
        public Uri SubscriptionsUrl { get; set; }

        [JsonPropertyName("type")] public string Type { get; set; }

        [JsonPropertyName("url")] public Uri Url { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("user_view_type")]
        public string UserViewType { get; set; }
    }

    public class ReactionRollup
    {
        [JsonPropertyName("+1")] public long The1 { get; set; }

        [JsonPropertyName("-1")] public long ReactionRollup1 { get; set; }

        [JsonPropertyName("confused")] public long Confused { get; set; }

        [JsonPropertyName("eyes")] public long Eyes { get; set; }

        [JsonPropertyName("heart")] public long Heart { get; set; }

        [JsonPropertyName("hooray")] public long Hooray { get; set; }

        [JsonPropertyName("laugh")] public long Laugh { get; set; }

        [JsonPropertyName("rocket")] public long Rocket { get; set; }

        [JsonPropertyName("total_count")] public long TotalCount { get; set; }

        [JsonPropertyName("url")] public Uri Url { get; set; }
    }

    /// <summary>
    /// State of the release asset.
    /// </summary>
    public enum State
    {
        Open,
        Uploaded
    };

    // public partial class GithubReleases
    // {
    //     public static List<GithubReleases> FromJson(string json) => JsonSerializer.Deserialize<List<GithubReleases>>(json, Converter.Settings);
    // }
    //
    // public static class Serialize
    // {
    //     public static string ToJson(this List<GithubReleases> self) => JsonSerializer.Serialize(self, Converter.Settings);
    // }

    public static class Converter
    {
        public static readonly JsonSerializerOptions Settings = new(JsonSerializerDefaults.General)
        {
            Converters =
            {
                StateConverter.Singleton,
                new DateOnlyConverter(),
                new TimeOnlyConverter(),
                IsoDateTimeOffsetConverter.Singleton
            },
        };
    }

    internal class StateConverter : JsonConverter<State>
    {
        public override bool CanConvert(Type t) => t == typeof(State);

        public override State Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            switch (value)
            {
                case "open":
                    return State.Open;
                case "uploaded":
                    return State.Uploaded;
            }

            throw new Exception("Cannot unmarshal type State");
        }

        public override void Write(Utf8JsonWriter writer, State value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case State.Open:
                    JsonSerializer.Serialize(writer, "open", options);
                    return;
                case State.Uploaded:
                    JsonSerializer.Serialize(writer, "uploaded", options);
                    return;
            }

            throw new Exception("Cannot marshal type State");
        }

        public static readonly StateConverter Singleton = new StateConverter();
    }

    public class DateOnlyConverter : JsonConverter<DateOnly>
    {
        private readonly string _serializationFormat;

        public DateOnlyConverter() : this(null)
        {
        }

        public DateOnlyConverter(string? serializationFormat)
        {
            this._serializationFormat = serializationFormat ?? "yyyy-MM-dd";
        }

        public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            return DateOnly.Parse(value!);
        }

        public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString(_serializationFormat));
    }

    public class TimeOnlyConverter : JsonConverter<TimeOnly>
    {
        private readonly string _serializationFormat;

        public TimeOnlyConverter() : this(null)
        {
        }

        public TimeOnlyConverter(string? serializationFormat)
        {
            _serializationFormat = serializationFormat ?? "HH:mm:ss.fff";
        }

        public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            return TimeOnly.Parse(value!);
        }

        public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString(_serializationFormat));
    }

    internal class IsoDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
    {
        public override bool CanConvert(Type t) => t == typeof(DateTimeOffset);

        private const string DefaultDateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK";

        private DateTimeStyles _dateTimeStyles = DateTimeStyles.RoundtripKind;
        private string? _dateTimeFormat;
        private CultureInfo? _culture;

        public DateTimeStyles DateTimeStyles
        {
            get => _dateTimeStyles;
            set => _dateTimeStyles = value;
        }

        public string? DateTimeFormat
        {
            get => _dateTimeFormat ?? string.Empty;
            set => _dateTimeFormat = (string.IsNullOrEmpty(value)) ? null : value;
        }

        public CultureInfo Culture
        {
            get => _culture ?? CultureInfo.CurrentCulture;
            set => _culture = value;
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        {
            if ((_dateTimeStyles & DateTimeStyles.AdjustToUniversal) == DateTimeStyles.AdjustToUniversal
                || (_dateTimeStyles & DateTimeStyles.AssumeUniversal) == DateTimeStyles.AssumeUniversal)
            {
                value = value.ToUniversalTime();
            }

            var text = value.ToString(_dateTimeFormat ?? DefaultDateTimeFormat, Culture);

            writer.WriteStringValue(text);
        }

        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert,
            JsonSerializerOptions options)
        {
            string? dateText = reader.GetString();

            if (!string.IsNullOrEmpty(dateText))
            {
                if (!string.IsNullOrEmpty(_dateTimeFormat))
                {
                    return DateTimeOffset.ParseExact(dateText, _dateTimeFormat, Culture, _dateTimeStyles);
                }

                return DateTimeOffset.Parse(dateText, Culture, _dateTimeStyles);
            }

            return default;
        }


        public static readonly IsoDateTimeOffsetConverter Singleton = new IsoDateTimeOffsetConverter();
    }

#pragma warning restore CS8618
#pragma warning restore CS8601
#pragma warning restore CS8603
