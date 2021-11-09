// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using RestaurantBookingBot.Models;

namespace RestaurantBookingBot.Dialogs
{
    public class BookingDialog : ComponentDialog
    {
        public BookingDialog()
            : base(nameof(BookingDialog))
        {
            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
            {
                DateStepAsync,
                AvailableSlotsStepAsync,
                NameStepAsync,
                SeatsStepAsync,
                ConfirmStepAsync,
                SummaryStepAsync,
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new NumberPrompt<int>(nameof(NumberPrompt<int>), SeatsPromptValidatorAsync));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private static async Task<DialogTurnResult> DateStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Hi, welcome to the restaurant booking bot."), cancellationToken);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("What date would you like to dine?") }, cancellationToken);
        }

        private static async Task<DialogTurnResult> AvailableSlotsStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["date"] = (string)stepContext.Result;
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://restaurantslots.azurewebsites.net/api/");

                using (HttpResponseMessage response = await client.GetAsync($"GetRestaurantSlots?date={stepContext.Values["date"]}"))
                {
                    var responseContent = response.Content.ReadAsStringAsync().Result;
                    stepContext.Values["available_bookings"] = responseContent;
                    IList<Bookings> availableBookings = JsonConvert.DeserializeObject<IList<Bookings>>(responseContent);
                    IList<string> timeSlotOptions =
                        availableBookings.Where(y => y.SpacesRemaining > 0).Select(x => x.Time).ToList();

                    if (timeSlotOptions.Count == 0)
                    {
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Oh no! We are fully booked for {stepContext.Values["date"]}. Please check another date... We would love to share our food with you soon!"), cancellationToken);
                        return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                    }

                    return await stepContext.PromptAsync(nameof(ChoicePrompt),
                        new PromptOptions
                        {
                            Prompt = MessageFactory.Text("Please select your preferred dining time:"),
                            Choices = ChoiceFactory.ToChoices(timeSlotOptions),
                        }, cancellationToken);
                }
            }
        }

        private static async Task<DialogTurnResult> NameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["slot"] = ((FoundChoice)stepContext.Result).Value;

            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter your name.") }, cancellationToken);
        }

        private async Task<DialogTurnResult> SeatsStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["name"] = (string)stepContext.Result;

            // We can send messages to the user at any point in the WaterfallStep.
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Thanks {stepContext.Result}."), cancellationToken);

            var promptOptions = new PromptOptions
            {
                Prompt = MessageFactory.Text("How many seats do you require?"),
                RetryPrompt = MessageFactory.Text("The value entered must be from 1 to 10."),
            };

            return await stepContext.PromptAsync(nameof(NumberPrompt<int>), promptOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            string name = (string)stepContext.Values["name"];
            string seats = stepContext.Result.ToString();
            string date = (string)stepContext.Values["date"];
            string time = (string)stepContext.Values["slot"];
            stepContext.Values["seats"] = seats;

            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"{name}, you have requested a table for {seats} at {time} on {date}."), cancellationToken);
            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = MessageFactory.Text("Is this OK?") }, cancellationToken);
        }

        private async Task<DialogTurnResult> SummaryStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
            {
                IList<Bookings> bookings = JsonConvert.DeserializeObject<IList<Bookings>>((string)stepContext.Values["available_bookings"]);
                BookingRequest request = new BookingRequest()
                {
                    Date = (string)stepContext.Values["date"],
                    Name = (string)stepContext.Values["name"],
                    Seats = int.Parse((string)stepContext.Values["seats"]),
                    Id = bookings.First(x => x.Time == (string)stepContext.Values["slot"]).Id
                };

                String requestJson = JsonConvert.SerializeObject(request);

                using (HttpClient client = new HttpClient())
                {
                    client.BaseAddress = new Uri("https://restaurantslots.azurewebsites.net/api/");

                    using (HttpResponseMessage response = await client.PostAsync($"BookRestaurantSlot", new StringContent(requestJson)))
                    {
                        var responseContent = response.Content.ReadAsStringAsync().Result;
                        if (bool.Parse(responseContent))
                        {
                            // Success
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Yay! Your booking was successful... We look forward to seeing you soon!"), cancellationToken);
                        }
                        else
                        {
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Unfortunately, we had problems making your booking. Please try again soon!"), cancellationToken);
                        }
                    }
                }
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Thank you for your inquiry. Please be in touch to make future bookings."), cancellationToken);
            }

            // WaterfallStep always finishes with the end of the Waterfall or with another dialog; here it is the end.
            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }

        private static Task<bool> SeatsPromptValidatorAsync(PromptValidatorContext<int> promptContext, CancellationToken cancellationToken)
        {
            // This condition is our validation rule. You can also change the value at this point.
            return Task.FromResult(promptContext.Recognized.Succeeded && promptContext.Recognized.Value > 0 && promptContext.Recognized.Value <= 10);
        }
    }
}
