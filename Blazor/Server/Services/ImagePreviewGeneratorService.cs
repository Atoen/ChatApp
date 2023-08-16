using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using SixLabors.ImageSharp.Formats.Pbm;
using SixLabors.ImageSharp.Formats.Png;
using tusdotnet.Interfaces;
using tusdotnet.Models;
using tusdotnet.Stores;
using tusdotnet.Stores.FileIdProviders;

namespace Blazor.Server.Services;

public class ImagePreviewGeneratorService
{
    private readonly TusDiskStore _tusStore;

    public static int MaxWidth { get; set; } = 700;
    public static int MaxHeight { get; set; } = 500;

    public ImagePreviewGeneratorService(TusDiskStoreHelper diskStoreHelper)
    {
        _tusStore = new TusDiskStore(diskStoreHelper.Path);
    }

    public async Task<string> CreateImagePreviewAsync(ITusFile imageFile, CancellationToken cancellationToken)
    {
        var imageData = await imageFile.GetContentAsync(cancellationToken);
        var metadata = await imageFile.GetMetadataAsync(cancellationToken);
        using var image = await Image.LoadAsync(imageData, cancellationToken);
        
        image.Mutate(x => x.Resize(MaxWidth, 0, KnownResamplers.NearestNeighbor));

        await using var stream = new MemoryStream();
        await image.SaveAsync(stream, new PngEncoder(), cancellationToken);
        stream.Seek(0, SeekOrigin.Begin);

        var meta = CreateMetadata(metadata);
        var id = await _tusStore.CreateFileAsync(stream.Length, meta, cancellationToken);
        
        await _tusStore.SetUploadLengthAsync(id, stream.Length, cancellationToken);
        await _tusStore.AppendDataAsync(id, stream, cancellationToken);

        return id;
    }

    private string CreateMetadata(Dictionary<string, Metadata> metadata)
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