using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace FileShare.Storing
{
    public class FileSharingHandler
    {
        public async Task HandleClientAsync(StreamReader reader, StreamWriter writer, NetworkStream stream, Func<string, Task<string?>> requestSavePathAsync)
        {
            await writer.WriteLineAsync("READY_FOR_FILE");

            string fileName = await reader.ReadLineAsync();
            Debug.WriteLine($"Received file name: {fileName}");
            string fileSizeLine = await reader.ReadLineAsync();
            Debug.WriteLine($"Received file size: {fileSizeLine}");

            if (!long.TryParse(fileSizeLine, out long fileSize))
            {
                await writer.WriteLineAsync("INVALID_FILE_SIZE");
                return;
            }

            Debug.WriteLine($"File name: {fileName}, File size: {fileSize} bytes");

            string? savePath = await requestSavePathAsync(fileName);

            if (string.IsNullOrEmpty(savePath))
            {
                await writer.WriteLineAsync("USER_CANCELLED");
                return;
            }

            await writer.WriteLineAsync("READY_TO_RECEIVE_FILE");

            using var bufferedStream = new BufferedStream(stream, 65536);
            using var fs = new FileStream(savePath, FileMode.Create, FileAccess.Write, System.IO.FileShare.None, 65536, true);

            byte[] buffer = new byte[65536];
            long remaining = fileSize;

            while (remaining > 0)
            {
                int bytesToRead = (int)Math.Min(buffer.Length, remaining);
                int bytesRead = await bufferedStream.ReadAsync(buffer.AsMemory(0, bytesToRead));
                if (bytesRead == 0) break;

                await fs.WriteAsync(buffer.AsMemory(0, bytesRead));
                remaining -= bytesRead;

                Debug.WriteLine($"Bytes read: {bytesRead}, Remaining: {remaining}");
            }
            await fs.FlushAsync();
            await stream.FlushAsync();

            Debug.WriteLine($"File {fileName} received and saved to {savePath}");

            await writer.WriteLineAsync("FILE_RECEIVED");
            await writer.FlushAsync();

            await Task.Delay(1000);
        }
    }
}
