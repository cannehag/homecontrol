using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Site.Models.Gallery.Raw
{
    public class LightroomLink
    {
        [JsonProperty("href")]
        public string Href { get; set; }
    }

    public class LightroomRef
    {
        [JsonProperty("id")]
        public string Id { get; set; }
    }

    public class LightroomCatalog
    {
        [JsonProperty("id")]
        public string Id { get; set; }
    }

    public class LightroomAlbumsResponse
    {
        [JsonProperty("resources")]
        public List<LightroomAlbum> Resources { get; set; } = new();

        [JsonProperty("links")]
        public Dictionary<string, LightroomLink> Links { get; set; } = new();
    }

    public class LightroomAlbum
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        // "collection" (regular album), "collection_set" (folder), "smart" (rule-based)
        [JsonProperty("subtype")]
        public string Subtype { get; set; }

        [JsonProperty("links")]
        public Dictionary<string, LightroomLink> Links { get; set; } = new();

        [JsonProperty("payload")]
        public LightroomAlbumPayload Payload { get; set; } = new();
    }

    public class LightroomAlbumPayload
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("parent")]
        public LightroomRef Parent { get; set; }

        [JsonProperty("cover")]
        public LightroomRef Cover { get; set; }
    }

    public class LightroomAlbumAssetsResponse
    {
        [JsonProperty("resources")]
        public List<LightroomAlbumAsset> Resources { get; set; } = new();

        [JsonProperty("links")]
        public Dictionary<string, LightroomLink> Links { get; set; } = new();
    }

    public class LightroomAlbumAsset
    {
        [JsonProperty("asset")]
        public LightroomAsset Asset { get; set; }
    }

    public class LightroomAsset
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        // "image", "video", "stack", ... - only "image" is relevant here
        [JsonProperty("subtype")]
        public string Subtype { get; set; }

        [JsonProperty("links")]
        public Dictionary<string, LightroomLink> Links { get; set; } = new();

        // The xmp/develop/location shape varies a lot per asset (optional blocks,
        // rational-number arrays, dynamic keyword maps) - parsed on demand via
        // JToken.SelectToken rather than strict POCOs.
        [JsonProperty("payload")]
        public JObject Payload { get; set; }
    }
}
