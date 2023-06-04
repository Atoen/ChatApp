using System.Collections.Concurrent;
using OneOf;
using OneOf.Types;

namespace HttpServer.Services;

public class FileService
{
    private readonly ConcurrentDictionary<string, string> _fileMapping;
    private const string SaveDirectory = @"C:\Users\artur\Desktop\ServerFileCache";

    public FileService()
    {
        _fileMapping = new ConcurrentDictionary<string, string>();
    }

    public async Task<string> SaveFile(IFormFile file)
    {
        var originalName = Path.GetFileName(file.FileName);
        var uid = Guid.NewGuid().ToString(format: "N") + Path.GetExtension(file.FileName);

        _fileMapping.TryAdd(uid, originalName);

        var savePath = Path.Combine(SaveDirectory, uid);
        await using var fileStream = new FileStream(savePath, FileMode.Create);
        await file.CopyToAsync(fileStream);

        return uid;
    }

    public async Task<OneOf<(string, byte[]), NotFound>> GetFile(string id)
    {
        var filePath = Path.Combine(SaveDirectory, id);
        if (!File.Exists(filePath))
        {
            return new NotFound();
        }

        var content = await File.ReadAllBytesAsync(filePath);
        var name = _fileMapping.TryGetValue(id, out var originalName) ? originalName : id;

        return (name, content);
    }
}