using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Nexmo.Api.Request;
using Nexmo.Api.Voice;
using Nexmo.Api.Voice.EventWebhooks;
using Nexmo.Api.Voice.Nccos;

namespace VonageVoicemail.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VoiceController : ControllerBase
    {
        private readonly IConfiguration _config;

        public VoiceController(IConfiguration config)
        {
            _config = config;
        }

        [HttpGet]
        [Route("webhooks/answer")]
        public string Answer()
        {
            var host = Request.Host.ToString();
            //Uncomment the next line if using ngrok with --host-header option
            //host = Request.Headers["X-Original-Host"];
            var sitebase = $"{Request.Scheme}://{host}";

            var talkAction = new TalkAction
            {
                Text = "Hello, you have reached Steve's number," +
                " he is cannot come to the phone right now. " +
                "Please leave a message after the tone.",
                VoiceName = "Joey"
            };

            var recordAction = new RecordAction
            {
                EndOnSilence = "3",
                BeepStart = "true",
                EventUrl = new[] { $"{sitebase}/webhooks/recording" },
                EventMethod = "POST"
            };

            var ncco = new Ncco(talkAction, recordAction);
            return ncco.ToString();
        }

        [HttpPost]
        [Route("webhooks/recording")]
        public IActionResult Recording()
        {
            Record record;
            var appId = _config["APP_ID"];
            var privateKeyPath = _config["PRIVATE_KEY_PATH"];
            var credentials = Credentials.FromAppIdAndPrivateKeyPath(appId, privateKeyPath);
            var voiceClient = new VoiceClient(credentials);
            using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                record = JsonConvert.DeserializeObject<Record>(reader.ReadToEndAsync().Result);
                var recording = voiceClient.GetRecording(record.RecordingUrl);
                System.IO.File.WriteAllBytes("your_recording.mp3", recording.ResultStream);
            }

            Console.WriteLine($"Record event received on webhook - URL: {record?.RecordingUrl}");
            return StatusCode(204);
        }
    }
}
