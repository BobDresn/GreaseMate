using System.IO;

namespace GreaseMate.Services;

public sealed class VehiclePhotoService
{
    private static readonly string PhotoDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "GreaseMate", "VehiclePhotos");

    public string Import(string sourcePath)
    {
        Directory.CreateDirectory(PhotoDirectory);
        var extension = Path.GetExtension(sourcePath).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{extension}";
        File.Copy(sourcePath, Path.Combine(PhotoDirectory, fileName), overwrite: false);
        return fileName;
    }

    public void Delete(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return;
        var fullPath = Path.Combine(PhotoDirectory, Path.GetFileName(fileName));
        if (File.Exists(fullPath)) File.Delete(fullPath);
    }
}
