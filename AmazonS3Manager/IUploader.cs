using System.Threading.Tasks;

namespace AmazonS3Manager
{
	public interface IUploader
	{
		Task<bool> UploadAsync (string filePath);
	}
}
