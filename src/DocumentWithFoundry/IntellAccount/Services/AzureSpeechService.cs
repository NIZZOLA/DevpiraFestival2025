/*
using Ciandt.Infra.Shared.Constants;
using Ciandt.Infra.Shared.Interfaces;
using Ciandt.Infra.Shared.Models.Options;
using FFMpegCore;
using FFMpegCore.Pipes;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NAudio.Lame;
using NAudio.Wave;
using System.Runtime.InteropServices;

namespace Ciandt.Infra.Shared.Services;
public class AzureSpeechService : IAzureSpeechService
{
    private SpeechConfig _speechConfig;
    private ILogger _logger;
    public AzureSpeechService(IOptions<AzureSpeechOptions> options, IConfiguration config, ILogger<AzureSpeechService> logger)
    {
        _logger = logger;
        AzureSpeechOptions azureSpeechOptions = SelectConfiguration(options, config);

        _speechConfig = SpeechConfig.FromSubscription(azureSpeechOptions.AzureKey, azureSpeechOptions.AzureLocation);
        _speechConfig.SpeechRecognitionLanguage = azureSpeechOptions.Language;
    }

    private AzureSpeechOptions SelectConfiguration(IOptions<AzureSpeechOptions> options, IConfiguration config)
    {
        var azureKey = config.GetSection("AzureSpeechAzureKey").Value;
        if (string.IsNullOrEmpty(azureKey))
            throw new Exception("Azure Speech Key is not provided in azure key vault");

        options.Value.AzureKey = azureKey;
        if (options.Value != null && (!string.IsNullOrWhiteSpace(options.Value.AzureKey) && !string.IsNullOrEmpty(options.Value.AzureLocation)))
            return options.Value;

        return new AzureSpeechOptions()
        {
            AzureKey = config.GetSection("AzureSpeechAzureKey").Value,
            AzureLocation = config.GetSection("AzureSpeech_AzureLocation").Value,
            Language = config.GetSection("AzureSpeech_Language").Value
        };
    }

    public async Task Talk(string phrase)
    {
        using var speechSynthesizer = new SpeechSynthesizer(_speechConfig);
        await speechSynthesizer.SpeakTextAsync(phrase);
    }

    public async Task TalkToWav(string phrase, string fileName)
    {
        string responseFilePath = fileName + ".wav";
        using (var audioResponseConfig = AudioConfig.FromWavFileOutput(responseFilePath))
        {
            using (var speechSynthesizer = new SpeechSynthesizer(_speechConfig, audioResponseConfig))
            {
                await speechSynthesizer.SpeakTextAsync(phrase);
            }
        }
    }

    public async Task TalkToMP3(string phrase, string fileName)
    {
        var wavWithPath = SharedConstants.GetFilenameWithTempPath($"{fileName}.wav");
        var mp3WithPath = SharedConstants.GetFilenameWithTempPath($"{fileName}.mp3");

        using (var speechSynthesizer = new SpeechSynthesizer(_speechConfig, null))
        {
            using (var result = await speechSynthesizer.SpeakTextAsync(phrase))
            {
                using (var stream = AudioDataStream.FromResult(result))
                {
                    stream.SaveToWaveFileAsync(wavWithPath).Wait();
                }
            }
        }
        await WavToMp3(wavWithPath, mp3WithPath);
    }

    public async Task<string> RecognizeFromWavUsingStream(string filename)
    {
        var reader = new BinaryReader(File.OpenRead(filename));
        using var audioConfigStream = AudioInputStream.CreatePushStream();
        using var audioConfig = AudioConfig.FromStreamInput(audioConfigStream);
        using var speechRecognizer = new SpeechRecognizer(_speechConfig, audioConfig);

        byte[] readBytes;
        do
        {
            readBytes = reader.ReadBytes(1024);
            audioConfigStream.Write(readBytes, readBytes.Length);
        } while (readBytes.Length > 0);

        var speechRecognitionResult = await speechRecognizer.RecognizeOnceAsync();
        Console.WriteLine($"RECOGNIZED: Text={speechRecognitionResult.Text}");
        return speechRecognitionResult.Text;
    }

    public async Task<string> TranslateMP3SpeechToText(string filename)
    {
        using Mp3FileReader mp3FileReader = new Mp3FileReader(filename);

        using WaveStream pcm = WaveFormatConversionStream.CreatePcmStream(mp3FileReader);
        var newFilename = ChangeExtension(filename, "wav");
        WaveFileWriter.CreateWaveFile(newFilename, pcm);
        return await RecognizeFromWavUsingStream(newFilename);
    }

    public async Task<string> TranslateOGGSpeechToText(string filename)
    {
        var outputWavPath = ChangeExtension(filename, "wav");

        using (var audioInputStream = File.OpenRead(filename))
        {
            using (var audioOutputStream = File.Open(outputWavPath, FileMode.Create))
            {
                // Usando FFMpegCore para converter - executando de forma síncrona
                FFMpegArguments
                    .FromPipeInput(new StreamPipeSource(audioInputStream))
                    .OutputToPipe(new StreamPipeSink(audioOutputStream), options =>
                        options.ForceFormat("wav"))
                    .ProcessSynchronously();

                audioOutputStream.Flush();
            }
        }

        return await TranslateWaveSpeechToText(outputWavPath);
    }

    private string ChangeExtension(string filename, string extension)
    {
        if (string.IsNullOrEmpty(filename))
            return string.Empty;

        string nameWithoutExtension = Path.GetFileNameWithoutExtension(filename);
        extension = extension.TrimStart('.');

        return $"{nameWithoutExtension}.{extension}";
    }

    public async Task<string> TranslateWaveSpeechToText(string fileName)
    {
        string response = "não tivemos uma resposta !";
        try
        {
            FileInfo fileInfo = new FileInfo(fileName);
            if (fileInfo.Exists)
            {

                using var audioConfig = AudioConfig.FromWavFileInput(fileInfo.FullName);
                using var speechRecognizer = new SpeechRecognizer(_speechConfig, audioConfig);

                var speechRecognitionResult = await speechRecognizer.RecognizeOnceAsync();

                Console.WriteLine("Speech recognition stopped.");
                Console.WriteLine(speechRecognitionResult.Text);
                response = speechRecognitionResult.Text;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        return response;
    }

    private void OutputSpeechRecognitionResult(SpeechRecognitionResult speechRecognitionResult)
    {
        switch (speechRecognitionResult.Reason)
        {
            case ResultReason.RecognizedSpeech:
                Console.WriteLine($"RECOGNIZED: Text={speechRecognitionResult.Text}");
                break;
            case ResultReason.NoMatch:
                Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                break;
            case ResultReason.Canceled:
                var cancellation = CancellationDetails.FromResult(speechRecognitionResult);
                Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                if (cancellation.Reason == CancellationReason.Error)
                {
                    Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                    Console.WriteLine($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
                    Console.WriteLine($"CANCELED: Did you set the speech resource key and region values?");
                }
                break;
        }
    }

    public void Mp3ToWav(string inputMp3Path, string outputWavPath)
    {
        using (var reader = new Mp3FileReader(inputMp3Path))
        {
            WaveFileWriter.CreateWaveFile(outputWavPath, reader);
        }
    }

    public async Task WavToMp3(string inputWavPath, string outputMp3Path, int bitRate = 128)
    {

        bool isAvailable = await WaitForFileAvailabilityAsync(inputWavPath);
        if (isAvailable)
        {

            try
            {
                using (var reader = new WaveFileReader(inputWavPath))
                using (var writer = new LameMP3FileWriter(outputMp3Path, reader.WaveFormat, bitRate))
                {
                    reader.CopyTo(writer);
                }
            }
            catch (Exception error)
            {
                _logger.LogError($"Falha na conversão de {inputWavPath} para {outputMp3Path} - {error.Message.First().ToString()}");
            }
        }
        else
        {
            _logger.LogWarning("Arquivo continua bloqueado após tempo de espera");
        }
    }

    public async Task<bool> WaitForFileAvailabilityAsync(string filePath, int maxWaitTimeInSeconds = 60, int retryIntervalInMs = 500)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            _logger.LogError("Caminho do arquivo não fornecido para verificação de disponibilidade");
            return false;
        }

        if (!File.Exists(filePath))
        {
            _logger.LogWarning($"Arquivo {filePath} não existe para verificação de disponibilidade");
            return false;
        }

        DateTime startTime = DateTime.Now;
        DateTime endTime = startTime.AddSeconds(maxWaitTimeInSeconds);
        int attemptCount = 0;
        bool isAvailable = false;

        _logger.LogInformation($"Verificando disponibilidade do arquivo: {filePath}");

        while (DateTime.Now < endTime)
        {
            attemptCount++;

            try
            {
                // Tenta abrir o arquivo para escrita exclusiva
                // Se o arquivo estiver em uso, isso gerará uma exceção
                using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    // Se conseguiu abrir, o arquivo não está em uso
                    isAvailable = true;
                    _logger.LogInformation($"Arquivo {Path.GetFileName(filePath)} está disponível após {attemptCount} tentativas");
                    break;
                }
            }
            catch (IOException ex) when (IsFileLocked(ex))
            {
                // O arquivo está em uso
                TimeSpan elapsed = DateTime.Now - startTime;
                TimeSpan remaining = endTime - DateTime.Now;

                _logger.LogWarning($"Tentativa {attemptCount}: Arquivo {Path.GetFileName(filePath)} " +
                                  $"ainda está em uso. Tempo decorrido: {elapsed.TotalSeconds:F1}s, " +
                                  $"Tempo restante: {remaining.TotalSeconds:F1}s");

                // Aguarda antes de tentar novamente
                await Task.Delay(retryIntervalInMs);
            }
            catch (Exception ex)
            {
                // Outra exceção ocorreu
                _logger.LogError($"Erro ao verificar disponibilidade do arquivo {Path.GetFileName(filePath)}: {ex.Message}");
                return false;
            }
        }

        if (!isAvailable)
        {
            _logger.LogError($"Tempo limite excedido aguardando a liberação do arquivo {Path.GetFileName(filePath)} " +
                             $"após {attemptCount} tentativas durante {maxWaitTimeInSeconds} segundos");
        }

        return isAvailable;
    }

    private bool IsFileLocked(IOException exception)
    {
        int errorCode = Marshal.GetHRForException(exception) & ((1 << 16) - 1);

        // 32 = ERROR_SHARING_VIOLATION
        // 33 = ERROR_LOCK_VIOLATION
        return errorCode == 32 || errorCode == 33;
    }
}
*/