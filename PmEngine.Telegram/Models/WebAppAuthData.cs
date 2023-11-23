using System.Text.Json.Serialization;

namespace PmEngine.Telegram.Models
{
    public class WebAppAuthData
    {
        public string query_id { get; set; }
        public User user { get; set; }
        public int auth_date { get; set; }
        public string hash { get; set; }
    }

    public class User
    {
        public long id { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? first_name { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? last_name { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? username { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? language_code { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? is_premium { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? allows_write_to_pm { get; set; }
    }
}