using Newtonsoft.Json;

namespace RestaurantBookingBot.Models
{
    public class BookingRequest
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "seats")]
        public int Seats { get; set; }

        [JsonProperty(PropertyName = "date")]
        public string Date { get; set; }
    }
}
