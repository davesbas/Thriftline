using ThriftlineApi.Models.Enums;

namespace ThriftlineApi.DTOs.Uploads;

public class UploadImageResponse
{
    public string Url { get; set; } = string.Empty;
    public MediaType MediaType { get; set; } = MediaType.Image;
}
