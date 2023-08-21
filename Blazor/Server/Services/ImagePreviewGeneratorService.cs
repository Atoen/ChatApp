using System.Diagnostics;
using SixLabors.ImageSharp.Formats.Png;
using tusdotnet.Interfaces;
using tusdotnet.Models;
using tusdotnet.Stores;

namespace Blazor.Server.Services;

public class ImagePreviewGeneratorService
{
    private readonly ILogger<ImagePreviewGeneratorService> _logger;
    private readonly TusDiskStore _tusStore;

    public static int MaxWidth { get; set; } = 700;
    public static int MaxHeight { get; set; } = 500;

    [Flags]
    public enum ResizingDimension
    {
        None,
        Width,
        Height,
        Both,
    }

    public ImagePreviewGeneratorService(TusDiskStoreHelper diskStoreHelper, ILogger<ImagePreviewGeneratorService> logger)
    {
        _logger = logger;
        _tusStore = new TusDiskStore(diskStoreHelper.Path);
    }

    public async Task<(string id, int width, int height)> CreateImagePreviewAsync(ITusFile imageFile, ResizingDimension resizingDimension, CancellationToken cancellationToken)
    {
        var start = Stopwatch.GetTimestamp();
        
        var imageData = await imageFile.GetContentAsync(cancellationToken);
        var metadata = await imageFile.GetMetadataAsync(cancellationToken);
        using var image = await Image.LoadAsync(imageData, cancellationToken);

        _logger.LogInformation("Original image size: {Size}", image.Size);

        var loaded = Stopwatch.GetTimestamp();
        _logger.LogInformation("Loaded image in {Time}", Stopwatch.GetElapsedTime(start, loaded));

        var (targetWidth, targetHeight) = resizingDimension switch
        {
            ResizingDimension.Width => (MaxWidth, 0),
            ResizingDimension.Height => (0, MaxHeight),
            ResizingDimension.Both => (MaxWidth, 0),
            _ => throw new ArgumentOutOfRangeException(nameof(resizingDimension), resizingDimension, null)
        };

        image.Mutate(x => x.Resize(targetWidth, targetHeight, KnownResamplers.NearestNeighbor));

        var resized = Stopwatch.GetTimestamp();
        _logger.LogInformation("Resized image in {Time}", Stopwatch.GetElapsedTime(loaded, resized));
        _logger.LogInformation("Resized image size: {Size}", image.Size);

        using var stream = new MemoryStream();
        await image.SaveAsync(stream, new PngEncoder(), cancellationToken);
        stream.Seek(0, SeekOrigin.Begin);

        var formattedMetadata = FormatMetadata(metadata);
        var id = await _tusStore.CreateFileAsync(stream.Length, formattedMetadata, cancellationToken);
        
        await _tusStore.SetUploadLengthAsync(id, stream.Length, cancellationToken);
        await _tusStore.AppendDataAsync(id, stream, cancellationToken);

        var saved = Stopwatch.GetTimestamp();
        _logger.LogInformation("Saved image preview in {Time}", Stopwatch.GetElapsedTime(resized, saved));
        _logger.LogInformation("Created preview in {Time}", Stopwatch.GetElapsedTime(start, saved));
        
        return (id, image.Width, image.Height);
    }

    private string FormatMetadata(Dictionary<string, Metadata> metadata)
    {
        var filename = metadata["filename"].GetBytes();
        var filetype = "image/png"u8;
        var filesize = metadata["filesize"].GetBytes();

        var name64 = Convert.ToBase64String(filename);
        var type64 = Convert.ToBase64String(filetype);
        var size64 = Convert.ToBase64String(filesize);

        return $"filename {name64},filetype {type64},filesize {size64}";
    }
}