using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TaskManager.API.Data;
using TaskManager.API.Services;

namespace TaskManager.API.Controllers
{
    [ApiController]
    [Route("api/ai")]
    public class AIController : ControllerBase
    {
        private readonly GeminiService _geminiService;

        private readonly ApplicationDbContext _context;

        public AIController(GeminiService geminiService, ApplicationDbContext context)
        {
            _geminiService = geminiService;
            _context = context;
        }

        [HttpPost("generate-task")]
        public async Task<IActionResult> GenerateTask([FromBody] GenerateTaskDto dto)
        {
            var result = await _geminiService.GenerateTask(dto.Input);

            var parsed = JsonSerializer.Deserialize<object>(result);

            return Ok(parsed); // ✅ now proper JSON
        }


        [HttpGet("plan")]
        public async Task<IActionResult> GeneratePlan()
        {
            var tasks = await _context.Tasks
                .Select(t => $"{t.Title} - {t.Priority} - Due {t.DueDate}")
                .ToListAsync();

            var result = await _geminiService.GeneratePlan(tasks);

            var parsed = JsonSerializer.Deserialize<object>(result);

            return Ok(parsed);
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatDto dto)
        {
            var tasks = await _context.Tasks
                .Select(t => $"{t.Title} - {t.Priority} - Due {t.DueDate}")
                .ToListAsync();

            var result = await _geminiService.ChatWithTasks(dto.Message, tasks);

            return Ok(new { response = result });
        }
    }
}