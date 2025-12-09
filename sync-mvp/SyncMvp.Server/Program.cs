using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Add CORS services
builder.Services.AddCors();

var app = builder.Build();

// Simple WebDAV server - stores files in local directory
var storagePath = Path.Combine(Environment.CurrentDirectory, "webdav-storage");
Directory.CreateDirectory(storagePath);

// Enable CORS for local development
app.UseCors(policy => policy
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

// Simple WebDAV handler
app.MapMethods("/webdav/{**path}", new[] { "GET", "PUT", "DELETE", "MKCOL", "PROPFIND" }, async (HttpContext context, string path) =>
{
    var fullPath = Path.Combine(storagePath, path.Replace('/', Path.DirectorySeparatorChar));
    
    switch (context.Request.Method)
    {
        case "GET":
            if (File.Exists(fullPath))
            {
                var content = await File.ReadAllBytesAsync(fullPath);
                return Results.File(content, "application/json");
            }
            return Results.NotFound();
            
        case "PUT":
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await context.Request.Body.CopyToAsync(stream);
            }
            return Results.Ok();
            
        case "DELETE":
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
            return Results.Ok();
            
        case "MKCOL":
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }
            return Results.Ok();
            
        case "PROPFIND":
            // Simple directory listing
            var files = new List<object>();
            if (Directory.Exists(fullPath))
            {
                foreach (var file in Directory.GetFiles(fullPath))
                {
                    files.Add(new
                    {
                        name = Path.GetFileName(file),
                        path = file.Replace(storagePath, "").Replace(Path.DirectorySeparatorChar, '/')
                    });
                }
            }
            return Results.Json(files);
            
        default:
            return Results.BadRequest();
    }
});

app.MapGet("/", () => "WebDAV Server is running. Storage path: " + storagePath);

app.Run("http://localhost:8080");
