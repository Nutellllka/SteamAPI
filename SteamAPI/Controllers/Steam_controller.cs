using app;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json;
using SteamAPI;
using System.Net.NetworkInformation;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace st.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class Steam_controller : ControllerBase
    { 
        [HttpGet("get_games_by_genre")]
        public async Task<ActionResult<List<GameInfo>>> GetGamesByGenre(string genre)
        {
            HttpClient httpClient = new HttpClient();
            var response = await httpClient.GetAsync($"https://steamspy.com/api.php?request=genre&genre={genre}");
            string result = await response.Content.ReadAsStringAsync();
            var games = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, GameInfo>>(result);
            List<GameInfo> gamesList = new List<GameInfo>();
            gamesList = games.Values.Take(100).ToList();
            return Ok(gamesList);
        }
        [HttpGet("get_top_100_games")]
        public async Task<ActionResult<List<GameInfo>>> GetTop100()
        {
            HttpClient httpClient = new HttpClient();
            var response = await httpClient.GetAsync($"https://steamspy.com/api.php?request=top100forever");
            string result = await response.Content.ReadAsStringAsync();
            var games = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, GameInfo>>(result);
            List<GameInfo> gamesList = new List<GameInfo>();
            gamesList = games.Values.Take(100).ToList();
            Console.Write(gamesList.Count);
            return Ok(gamesList);
        }
        [HttpGet("get_dlc_for_game")]
        public async Task<ActionResult<DLCList>> GetDLCForGame(string name)
        {
            HttpClient httpClient = new HttpClient();
            var response = await httpClient.GetAsync($"https://store.steampowered.com/api/storesearch/?term={name}&cc=us&l=english");
            string result = await response.Content.ReadAsStringAsync();
            DLCList dlcList = JsonConvert.DeserializeObject<DLCList>(result);
            return Ok(dlcList);
        }
        [HttpGet("get_link_for_game")]
        public async Task<ActionResult<string>> GetLinkForGame(string name)
        {
            HttpClient httpClient = new HttpClient();
            var response = await httpClient.GetAsync($"https://store.steampowered.com/api/storesearch/?term={name}&cc=us&l=english");
            string result = await response.Content.ReadAsStringAsync();
            DLCList dlcList = JsonConvert.DeserializeObject<DLCList>(result);
            return Ok($"https://store.steampowered.com/app/{dlcList.items[0].id}/{name}/");
        }
        [HttpPut("put_game_to_list")]
        public async Task<ActionResult> PutGameToList(long id, string game_name)
        {
            HttpClient httpClient = new HttpClient();
            ITelegramBotClient bot = new TelegramBotClient(constants.botId);
            var response = await httpClient.GetAsync($"https://store.steampowered.com/api/storesearch/?term={game_name}&cc=us&l=english");
            var result = await response.Content.ReadAsStringAsync();

            if (result != "{\"total\":0,\"items\":[]}")
            {
                var filter = Builders<BsonDocument>.Filter.Eq("user_id", id);
                var document = constants.collection.Find(filter).FirstOrDefault();

                var my_games = document["saved_games"].AsBsonArray;

                bool hasDuplicate = my_games.AsQueryable()
        .Any(a => a == game_name);


                if (hasDuplicate)
                {
                    await bot.SendTextMessageAsync(id, "The game is already in your list.");
                }
                else
                {
                    BsonValue bsonValue = BsonValue.Create(game_name);

                    my_games.Add(bsonValue);
                    var update = Builders<BsonDocument>.Update.Set("saved_games", my_games);
                    constants.collection.UpdateOne(filter, update);
                    await bot.SendTextMessageAsync(id, "The game was added to your list.");
                }
                return Ok();
            }
            else
            {
                await bot.SendTextMessageAsync(id, "This game doesn`t exist.");
                return BadRequest();
            }
        }
        [HttpPost("post_my_games_list")]
        public async Task<ActionResult> PostMyGamesList(long id)
        {
            HttpClient httpClient = new HttpClient();
            ITelegramBotClient bot = new TelegramBotClient(constants.botId);

            var filter = Builders<BsonDocument>.Filter.Eq("user_id", id);
            var document = constants.collection.Find(filter).FirstOrDefault();

            var games = document["saved_games"].AsBsonArray;
            if (games.Count == 0)
            {
                await bot.SendTextMessageAsync(id, "Your list is empty.");
            }
            else
            {
                await bot.SendTextMessageAsync(id, "Here is your list:");
                for (int i = 0; i < games.Count; i++)
                {
                    await bot.SendTextMessageAsync(id, $"{i + 1}. {games[i]}");
                }
            }
            return Ok();
        }
        [HttpDelete("delete_game_from_list")]
        public async Task<ActionResult> DeleteGameFromList(long id, int gameIndex)
        {
            gameIndex--;
            HttpClient httpClient = new HttpClient();
            ITelegramBotClient bot = new TelegramBotClient(constants.botId);

            var filter = Builders<BsonDocument>.Filter.Eq("user_id", id);
            var document = constants.collection.Find(filter).FirstOrDefault();
            var my_games = document["saved_games"].AsBsonArray;

            if (gameIndex >= 0 && gameIndex < my_games.Count)
            {
                string game = my_games[gameIndex].AsString;
                my_games.RemoveAt(gameIndex);
                var update = Builders<BsonDocument>.Update.Set("saved_games", my_games);
                constants.collection.UpdateOne(filter, update);
                await bot.SendTextMessageAsync(id, $"Game \"{game}\" has been removed from your list.");
                return Ok();
            }
            else
            {
                await bot.SendTextMessageAsync(id, "You entered the wrong game number.");
                return BadRequest();
            }
        }


        [HttpPut("bot_is_waiting_for_genre")]
        public ActionResult<string> BotIsWaitingForCityToRemove(long id, bool b)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("user_id", id);
            var update = Builders<BsonDocument>.Update.Set("bot_is_waiting_for_genre", b);
            var result = constants.collection.UpdateOne(filter, update);
            return Ok();
        }

        [HttpGet("bot_is_waiting_for_genre")]
        public ActionResult<bool> BotIsWaitingForCityToRemove(long id)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("user_id", id);
            var document = constants.collection.Find(filter).FirstOrDefault();
            if (document != null && document.TryGetValue("bot_is_waiting_for_genre", out BsonValue value))
            {
                return value.AsBoolean;
            }

            return NotFound();
        }
        [HttpPut("bot_is_waiting_for_name_of_game_to_find_dlc")]
        public ActionResult<string> BotISWaitingForNameOfGameToFindDLC(long id, bool b)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("user_id", id);
            var update = Builders<BsonDocument>.Update.Set("bot_is_waiting_for_name_of_game_to_find_dlc", b);
            var result = constants.collection.UpdateOne(filter, update);
            return Ok();
        }

        [HttpGet("bot_is_waiting_for_name_of_game_to_find_dlc")]
        public ActionResult<bool> BotISWaitingForNameOfGameToFindDLC(long id)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("user_id", id);
            var document = constants.collection.Find(filter).FirstOrDefault();
            if (document != null && document.TryGetValue("bot_is_waiting_for_name_of_game_to_find_dlc", out BsonValue value))
            {
                return value.AsBoolean;
            }

            return NotFound();
        }
        [HttpPut("bot_is_waiting_for_name_of_game_to_add_to_list")]
        public ActionResult<string> BotISWaitingForNameOfGameToAddToList(long id, bool b)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("user_id", id);
            var update = Builders<BsonDocument>.Update.Set("bot_is_waiting_for_name_of_game_to_add_to_list", b);
            var result = constants.collection.UpdateOne(filter, update);
            return Ok();
        }

        [HttpGet("bot_is_waiting_for_name_of_game_to_add_to_list")]
        public ActionResult<bool> BotISWaitingForNameOfGameToAddToList(long id)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("user_id", id);
            var document = constants.collection.Find(filter).FirstOrDefault();
            if (document != null && document.TryGetValue("bot_is_waiting_for_name_of_game_to_add_to_list", out BsonValue value))
            {
                return value.AsBoolean;
            }

            return NotFound();
        }
        [HttpPut("bot_is_waiting_for_name_of_game_to_remove_from_list")]
        public ActionResult<string> BotISWaitingForNameOfGameToRemoveFromList(long id, bool b)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("user_id", id);
            var update = Builders<BsonDocument>.Update.Set("bot_is_waiting_for_name_of_game_to_remove_from_list", b);
            var result = constants.collection.UpdateOne(filter, update);
            return Ok();
        }

        [HttpGet("bot_is_waiting_for_name_of_game_to_remove_from_list")]
        public ActionResult<bool> BotISWaitingForNameOfGameToRemoveFromList(long id)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("user_id", id);
            var document = constants.collection.Find(filter).FirstOrDefault();
            if (document != null && document.TryGetValue("bot_is_waiting_for_name_of_game_to_remove_from_list", out BsonValue value))
            {
                return value.AsBoolean;
            }

            return NotFound();
        }

        [HttpPut("bot_is_waiting_for_name_of_game_to_get_link")]
        public ActionResult<string> BotISWaitingForNameOfGameToGetLink(long id, bool b)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("user_id", id);
            var update = Builders<BsonDocument>.Update.Set("bot_is_waiting_for_name_of_game_to_get_link", b);
            var result = constants.collection.UpdateOne(filter, update);
            return Ok();
        }

        [HttpGet("bot_is_waiting_for_name_of_game_to_get_link")]
        public ActionResult<bool> BotISWaitingForNameOfGameToGetLink(long id)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("user_id", id);
            var document = constants.collection.Find(filter).FirstOrDefault();
            if (document != null && document.TryGetValue("bot_is_waiting_for_name_of_game_to_get_link", out BsonValue value))
            {
                return value.AsBoolean;
            }

            return NotFound();
        }

    }
}