using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using TusDotNetClient;
using WpfClient.Models;
using WpfClient.Views.UserControls;

namespace WpfClient.Services;

public class FileTransferService
{
    private readonly Func<string> _tokenProvider;

    private double _lastUploadProgress;
    private const double UploadProgressChangedStep = 0.01;

    public event Action<bool>? UploadingChanged;
    public event Action<double>? UploadProgressChanged;

    public FileTransferService(Func<string> tokenProvider)
    {
        _tokenProvider = tokenProvider;
    }

    private readonly TusClient _tusClient = new();

    public async Task<Result> UploadFileAsync(string username, FileInfo fileInfo, CancellationToken token)
    {
        _lastUploadProgress = 0;
        _tusClient.AdditionalHeaders["Authorization"] = $"Bearer {_tokenProvider.Invoke()}";

        UploadingChanged?.Invoke(true);

        try
        {
            var url = await _tusClient.CreateAsync($"{App.EndPointUri}/tus", fileInfo,
                ("length", fileInfo.Length.ToString()), ("sender", username));

            var upload = _tusClient.UploadAsync(url, fileInfo, chunkSize: 10, cancellationToken: token);

            upload.Progressed += UploadProgressHandler;

            await upload;
        }
        catch (Exception e)
        {
            if (!token.IsCancellationRequested)
            {
                return Result.Fail(e.Message);
            }
        }
        finally
        {
            UploadingChanged?.Invoke(false);
        }

        return Result.Ok();
    }

    public Message CreateUploadMessage(FileInfo fileInfo)
    {
        var embed = new FileUploadEmbed
        {
            UploadedFileName = fileInfo.Name,
            UploadedFileSize = fileInfo.Length
        };

        var message = Message.SystemMessage(string.Empty);
        message.UiEmbed = embed;

        return message;
    }

    private void UploadProgressHandler(long transferred, long total)
    {
        var uploadProgress = (double) transferred / total;

        if (uploadProgress - _lastUploadProgress > UploadProgressChangedStep)
        {
            UploadProgressChanged?.Invoke(uploadProgress);
            _lastUploadProgress = uploadProgress;
        }
    }
}