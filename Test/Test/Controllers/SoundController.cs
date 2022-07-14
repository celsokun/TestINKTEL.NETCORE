using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Recognition;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace Test.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SoundController : ControllerBase
    {
        private readonly ILogger<SoundController> _logger;

        public SoundController(ILogger<SoundController> logger)
        {
            _logger = logger;
        }
        [HttpPost]
        [Route("UploadAudioFile")]
        public async Task<IActionResult> UploadAudioFile(IFormFile file)
        {
            if (file.ContentType != "audio/mpeg")
            {
                return BadRequest("Wrong file type");
            }
            string uploads = Path.GetTempPath();
            string filePath = Path.Combine(uploads, file.FileName);
            string filePathWav = Path.Combine(uploads, file.FileName.Replace(".mp3", ".wav"));
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
            if (System.IO.File.Exists(filePathWav))
            {
                System.IO.File.Delete(filePathWav);
            }
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }
            SpeechRecognitionEngine sre = new SpeechRecognitionEngine();
            Grammar gr = new DictationGrammar();
            sre.LoadGrammar(gr);
            using (Mp3FileReader mp3 = new Mp3FileReader(filePath))
            {
                using (WaveStream pcm = WaveFormatConversionStream.CreatePcmStream(mp3))
                {
                    WaveFileWriter.CreateWaveFile(filePathWav, pcm);
                }
            }
            sre.SetInputToWaveFile(filePathWav);
            sre.BabbleTimeout = new TimeSpan(Int32.MaxValue);
            sre.InitialSilenceTimeout = new TimeSpan(Int32.MaxValue);
            sre.EndSilenceTimeout = new TimeSpan(100000000);
            sre.EndSilenceTimeoutAmbiguous = new TimeSpan(100000000);
            sre.LoadGrammar(new DictationGrammar());
            StringBuilder sb = new StringBuilder();
            while (true)
            {
                try
                {
                    var recText = sre.Recognize();
                    if (recText == null)
                    {
                        break;
                    }
                    sb.Append(recText.Text);
                }
                catch (Exception ex)
                {
                    break;
                }
            }
            return Ok(sb.ToString());
        }
    }
}
