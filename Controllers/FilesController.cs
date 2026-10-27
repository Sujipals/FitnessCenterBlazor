using Microsoft.AspNetCore.Mvc;

namespace FitnessCenterBlazor.Controllers
{
    [ApiController]
    [Route("api/files")]
    public class FilesController : ControllerBase
    {
        // Folder where uploaded files will be stored
        private readonly string _uploadFolder;

        // Allowed file extensions
        private readonly string[] _allowedExtensions =
        {
            ".txt",
            ".pdf",
            ".docx",
            ".csv"
        };

        public FilesController(IWebHostEnvironment environment)
        {
            // wwwroot/uploads
            _uploadFolder = Path.Combine(
                environment.WebRootPath,
                "uploads");

            // Create folder if it does not exist
            if (!Directory.Exists(_uploadFolder))
            {
                Directory.CreateDirectory(_uploadFolder);
            }
        }

        // =====================================================
        // 1. GET ALL FILES       

        [HttpGet]
        public IActionResult GetFiles()
        {
            // Read files from the upload folder
            // using LINQ as required by the assignment
            var files = Directory
                .GetFiles(_uploadFolder)
                .Select(file => new
                {
                    FileName = Path.GetFileName(file),
                    Size = new FileInfo(file).Length,
                    LastModified = System.IO.File.GetLastWriteTime(file)
                })
                .ToList();

            return Ok(files);
        }

        // =====================================================
        // 2. UPLOAD FILE        

        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            // Check whether a file was selected
            if (file == null || file.Length == 0)//validation
            {
                return BadRequest("No file was selected.");
            }

            // Get file extension
            var extension = Path.GetExtension(file.FileName);

            // Check allowed extensions , validation
            if (!_allowedExtensions.Contains(extension,
                StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest(
                    "Only .txt, .pdf, .docx and .csv files are allowed.");
            }

            // Get only the file name // This prevents path manipulation
            var safeFileName = Path.GetFileName(file.FileName);

            // Create complete file path
            var filePath = Path.Combine(_uploadFolder, safeFileName);

            // Save the file asynchronously
            using var stream = new FileStream(filePath, FileMode.Create);

            await file.CopyToAsync(stream);
            return Ok(new
            {
                message = "File uploaded successfully.",
                fileName = safeFileName
            });
        }

        // =====================================================
        // 3. DOWNLOAD FILE
        

        [HttpGet("download/{fileName}")]
        public async Task<IActionResult> Download(string fileName)
        {
            // Remove any possible path information.
            // This helps prevent path traversal attacks.
            var safeFileName = Path.GetFileName(fileName);

            // Create the complete path to the file.
            var filePath = Path.Combine(_uploadFolder, safeFileName);

            // Check whether the file exists.
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("File not found.");
            }

            // Detect the file content type.
            var contentType = GetContentType(safeFileName);

            // Open the file asynchronously.
            var stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true);

            // Return the file to the client.
            return File(
                stream,
                contentType,
                safeFileName);
        }

        // =====================================================
        // 4. DELETE FILE
        

        [HttpDelete("{fileName}")]
        public IActionResult Delete(string fileName)
        {
            // Remove possible path information
            var safeFileName = Path.GetFileName(fileName);
            var filePath = Path.Combine(_uploadFolder,  safeFileName);

            // Check whether file exists
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("File not found.");
            }

            // Delete the file
            System.IO.File.Delete(filePath);
            return Ok(new
            {
                message = "File deleted successfully.",
                fileName = safeFileName
            });
        }

        // =====================================================
        // CONTENT TYPE        

        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();

            return extension switch
            {
                ".txt" => "text/plain",
                ".pdf" => "application/pdf",
                ".docx" =>
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".csv" => "text/csv",
                _ => "application/octet-stream"
            };
        }
    }
}