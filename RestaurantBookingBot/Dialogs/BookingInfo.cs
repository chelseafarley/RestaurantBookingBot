// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Schema;

namespace RestaurantBookingBot.Dialogs
{
    /// <summary>
    /// This is our application state. Just a regular serializable .NET class.
    /// </summary>
    public class BookingInfo
    {
        public int Id { get; set; }

        public string Date { get; set; }

        public string Name { get; set; }

        public int Seats { get; set; }
    }
}
