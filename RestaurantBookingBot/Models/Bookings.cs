using System.Collections.Generic;
using Newtonsoft.Json;

namespace RestaurantBookingBot.Models
{
    public class Bookings
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "date")]
        public string Date { get; set; }

        [JsonProperty(PropertyName = "time")]
        public string Time { get; set; }

        [JsonProperty(PropertyName = "spaces_remaining")]
        public int SpacesRemaining { get; set; }

        [JsonProperty(PropertyName = "existing_bookings")]
        public IList<Booking> ExistingBookings { get; set; }
    }

    public class Booking
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "seats")]
        public int Seats { get; set; }
    }
}
