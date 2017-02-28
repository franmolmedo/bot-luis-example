using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using Newtonsoft.Json.Linq;

namespace BotLuisExample.LuisDialog
{
    [Serializable]
    [LuisModel("d4aeb219-660f-444d-905e-8ff69c48e630", "c87e472a48a840f89eb172fe9e372f7c")]
    public class PictureLuisDialog: LuisDialog<object>
    {
        private const string EntityGeographyCity = "builtin.geography.city";

        [LuisIntent("")]
        [LuisIntent("None")]
        public async Task None(IDialogContext context, LuisResult result)
        {
            await context.PostAsync($"Sorry, I did not understand '{result.Query}'. Try again with something like: 'find pics from London'");
            context.Wait(MessageReceived);
        }

        [LuisIntent("Pictures")]
        public async Task Pictures(IDialogContext context,  LuisResult result)
        {
            var message =  context.MakeMessage();
            await context.PostAsync("Please wait: ...");
            EntityRecommendation cityEntity;

            if (result.TryFindEntity(EntityGeographyCity, out cityEntity))
            {
                var images = await CallCognitiveServices(cityEntity.Entity);
                if (images != null)
                {
                    var attachments = new List<Attachment>();
                    foreach (var image in images["value"])
                    {
                        attachments.Add(new Attachment
                        {
                            ContentUrl = image["contentUrl"].Value<string>(),
                            ContentType = $"image/{image["encodingFormat"].Value<string>()}",
                            Name = image["name"].Value<string>()
                        });
                    }
                    message.Attachments = attachments;

                }

                await context.PostAsync(message);

            }
            else
            {
                message.Text = "i couldn´t understand you. Try again";
                await context.PostAsync(message);
                
            }
            context.Wait(MessageReceived);
        }

        private async Task<JObject> CallCognitiveServices(string city)
        {
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(city);

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "d3496274bb9a4fe4b3506752ee2cd055");


            // Request parameters
            var uri = "https://api.cognitive.microsoft.com/bing/v5.0/images/search?q="+ queryString + "&count=5";

            // Request body
            var apiResponse = await client.GetAsync(uri);

            if (apiResponse.StatusCode == HttpStatusCode.OK)
            {
                return await apiResponse.Content.ReadAsAsync<JObject>();
            }
            return null;

        }

    }
}