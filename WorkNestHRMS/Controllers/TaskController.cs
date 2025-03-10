using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WorkNestHRMS.Models;
using static WorkNestHRMS.Models.TaskRequests;
using System.Text.Json;

[ApiController]
[Route("api/workplaces/{workplaceId}/tasks")]
public class TasksController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public TasksController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateTask(int workplaceId, [FromBody] TaskRequests.CreateTaskRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var workplace = await _dbContext.Workplaces.Include(w => w.UserWorkplaces).FirstOrDefaultAsync(w => w.Id == workplaceId);

        if (workplace == null || userId == null)
        {
            return NotFound("Nie znaleziono miejsca pracy.");
        }

        var userWorkplace = workplace.UserWorkplaces.FirstOrDefault(uw => uw.UserId.ToString() == userId);
        if (userWorkplace == null || (userWorkplace.Role != "manager" && workplace.OwnerId.ToString() != userId))
        {
            return Forbid("Tylko właściciel i manager mogą tworzyć zadania.");
        }

        var task = new WorkNestHRMS.Models.Task
        {
            Content = request.Content,
            DueDate = DateTime.SpecifyKind(request.DueDate, DateTimeKind.Utc), // Konwersja na UTC
            Status = "Do wykonania",
            Priority = request.Priority,
            AssignedUserId = request.AssignedUserId,
            AssignedWorkGroupId = request.AssignedWorkGroupId,
            CreatedByUserId = int.Parse(userId),
            WorkplaceId = workplaceId,
            Attachments = new List<string>() // jak nie działa to wrócić do const stringa (edit1 : działa!!!!!!!!!!!!!!!!)
        };

        _dbContext.Tasks.Add(task);
        await _dbContext.SaveChangesAsync();

        return Ok(task);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetTasks(int workplaceId)
    {
        var tasks = await _dbContext.Tasks
            .Where(t => t.WorkplaceId == workplaceId)
            .Include(t => t.AssignedUser)
                .ThenInclude(u => u.Employee)
            .Include(t => t.AssignedWorkGroup)
            .Include(t => t.Workplace)
            .ToListAsync();

        var response = tasks.Select(task => new
        {
            task.Id,
            task.Content,
            task.DueDate,
            task.Status,
            task.Priority,
            Attachments = task.Attachments,
            WorkplaceName = task.Workplace?.Name ?? "Nieznane",
            AssignedUser = task.AssignedUser != null
                ? new
                {
                    Id = task.AssignedUser.Id,
                    FirstName = task.AssignedUser.Employee.FirstName,
                    LastName = task.AssignedUser.Employee.LastName
                }
                : null,
            AssignedWorkGroup = task.AssignedWorkGroup != null
                ? new
                {
                    Id = task.AssignedWorkGroup.Id,
                    Name = task.AssignedWorkGroup.Name
                }
                : null
        });

        return Ok(response);
    }

    [Authorize]
    [HttpDelete("{taskId}")]
    public async Task<IActionResult> DeleteTask(int workplaceId, int taskId)
    {
        var task = await _dbContext.Tasks.FindAsync(taskId);

        if (task == null)
        {
            return NotFound("Nie znaleziono zadania.");
        }

        _dbContext.Tasks.Remove(task);
        await _dbContext.SaveChangesAsync();

        return Ok("Zadanie zostało usunięte.");
    }

    [Authorize]
    [HttpPut("{taskId}/status")]
    public async Task<IActionResult> UpdateTaskStatus(int workplaceId, int taskId, [FromBody] UpdateTaskStatusRequest request)
    {
        var task = await _dbContext.Tasks
            .Include(t => t.Workplace) // tego nei było i daltego nei działało (nie łapało workpalceId)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null)
        {
            return NotFound("Nie znaleziono zadania.");
        }

        task.Status = request.Status;
        await _dbContext.SaveChangesAsync();

        return Ok(task);
    }

    [Authorize]
    [HttpPost("{taskId}/upload")]
    public async Task<IActionResult> UploadTaskFiles(int workplaceId, int taskId, [FromForm] List<IFormFile> files)
    {
        var task = await _dbContext.Tasks.FindAsync(taskId);

        if (task == null)
        {
            return NotFound("Nie znaleziono zadania.");
        }

        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "shared", "uploads");

        if (!Directory.Exists(basePath))
        {
            Directory.CreateDirectory(basePath);
        }

        foreach (var file in files)
        {
            var originalFileName = $"{task.Id}_{file.FileName}";
            var filePath = Path.Combine(basePath, originalFileName);

            // żeby plik.txt -> plik(1).txt
            var fileCounter = 1;
            while (System.IO.File.Exists(filePath))
            {
                var fileNameWithoutExt = Path.GetFileNameWithoutExtension(file.FileName);
                var fileExtension = Path.GetExtension(file.FileName);
                var newFileName = $"{task.Id}_{fileNameWithoutExt}({fileCounter}){fileExtension}";
                filePath = Path.Combine(basePath, newFileName);
                fileCounter++;  // upewnic sie
            }

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var relativePath = $"/uploads/{Path.GetFileName(filePath)}";
            task.Attachments.Add(relativePath);
        }

        await _dbContext.SaveChangesAsync();

        return Ok(task);
    }

    [Authorize]
    [HttpGet("{taskId}/files/{fileName}")]
    public async Task<IActionResult> GetTaskFile(int workplaceId, int taskId, string fileName)
    {
        try
        {
            var task = await _dbContext.Tasks.FindAsync(taskId);

            if (task == null)
            {
                return NotFound("Nie znaleziono zadania.");
            }

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "shared", "uploads", fileName);
                                                                                                        
            Console.WriteLine($"Ścieżka do pobrania pliku: {filePath}");

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("Nie znaleziono pliku.");
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            
            Console.WriteLine($"Pobrano plik: {filePath}");     // TEMP

            return File(fileBytes, "application/octet-stream", fileName);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Wystąpił błąd podczas pobierania pliku: {ex.Message}");
            return StatusCode(500, "Wystąpił błąd podczas pobierania pliku.");
        }
    }

    [Authorize]
    [HttpPut("{taskId}/refresh-attachments")]
    public async Task<IActionResult> RefreshAttachments(int workplaceId, int taskId)
    {
        var task = await _dbContext.Tasks.FindAsync(taskId);

        if (task == null)
        {
            return NotFound("Nie znaleziono zadania.");
        }

        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "shared", "uploads");
        var existingAttachments = new List<string>();

        foreach (var attachment in task.Attachments)
        {
            var filePath = Path.Combine(basePath, attachment.Split('/').Last());
            if (System.IO.File.Exists(filePath))
            {
                existingAttachments.Add(attachment);
            }
        }

        task.Attachments = existingAttachments;
        _dbContext.Tasks.Update(task);
        await _dbContext.SaveChangesAsync();

        return Ok(task); 
    }

    [Authorize]
    [HttpGet("{taskId}/attachments/{fileName}")]
    public async Task<IActionResult> DownloadAttachment(int workplaceId, int taskId, string fileName)
    {
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "shared", "uploads");

        // Łączenie z nazwą pliku
        var attachmentPath = Path.Combine(basePath, fileName);


        if (!System.IO.File.Exists(attachmentPath))
        {
            Console.WriteLine($"Attachment not found: {attachmentPath}");
            return NotFound("Nie znaleziono załącznika.");
        }

        var memory = new MemoryStream();
        try
        {
            using (var stream = new FileStream(attachmentPath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading file: {ex.Message}");
            return StatusCode(500, "Błąd podczas pobierania załącznika.");
        }
        memory.Position = 0;

        return File(memory, GetContentType(attachmentPath), fileName);
    }

    private string GetContentType(string path)
    {
        var types = GetMimeTypes();
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return types.GetValueOrDefault(ext, "application/octet-stream");
    }

    private Dictionary<string, string> GetMimeTypes()
    {
        return new Dictionary<string, string>
    {
        {".txt", "text/plain"},
        {".pdf", "application/pdf"},
        {".doc", "application/vnd.ms-word"},
        {".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"},
        {".xls", "application/vnd.ms-excel"},
        {".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"},
        {".png", "image/png"},
        {".jpg", "image/jpeg"},
        {".jpeg", "image/jpeg"},
        {".gif", "image/gif"},
        {".csv", "text/csv"},
        {".xcf", "image/x-xcf"} 
    };
    }

    [Authorize]
    [HttpDelete("{taskId}/attachments/{fileName}")]
    public async Task<IActionResult> DeleteAttachment(int workplaceId, int taskId, string fileName)
    {
        try
        {
            var decodedFileName = Uri.UnescapeDataString(fileName);
            Console.WriteLine($"Decoded FileName: {decodedFileName}");

            var task = await _dbContext.Tasks.FindAsync(taskId);

            if (task == null)
            {
                Console.WriteLine($"Nie znaleziono zadania: taskId={taskId}, workplaceId={workplaceId}");
                return NotFound("Nie znaleziono zadania.");
            }

            var attachments = task.Attachments ?? new List<string>();   

            var attachment = attachments.FirstOrDefault(a => a.EndsWith(decodedFileName, StringComparison.OrdinalIgnoreCase));
            if (attachment == null)
            {
                Console.WriteLine($"Nie znaleziono załącznika w bazie danych: {decodedFileName}");
                return NotFound("Nie znaleziono załącznika.");
            }

            var basePath = Path.Combine(Directory.GetCurrentDirectory(), "shared", "uploads");
            var attachmentPath = Path.Combine(basePath, decodedFileName);
            Console.WriteLine($"Ścieżka pliku: {attachmentPath}");

            if (!System.IO.File.Exists(attachmentPath))
            {
                Console.WriteLine($"Plik nie istnieje: {attachmentPath}");
                return NotFound("Nie znaleziono pliku na dysku.");
            }

            System.IO.File.Delete(attachmentPath);
            Console.WriteLine($"Plik usunięty: {attachmentPath}");

            attachments.Remove(attachment);
            task.Attachments = attachments; 
            _dbContext.Tasks.Update(task);
            await _dbContext.SaveChangesAsync();

            Console.WriteLine($"Załącznik usunięty z bazy danych: {attachment}");
            return Ok(new { message = "Załącznik został usunięty.", attachments });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas usuwania załącznika: {ex.Message}");
            return StatusCode(500, "Wystąpił błąd podczas usuwania załącznika.");
        }
    }

}
