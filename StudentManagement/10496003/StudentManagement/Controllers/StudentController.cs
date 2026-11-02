using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagement.Data;
using StudentManagement.Models;

namespace StudentManagement.Controllers
{
    public class StudentController : Controller
    {
        private readonly AppDbContext _context;

        public StudentController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var students = await _context.Students.ToListAsync();
            return View(students);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Student student)
        {
            _context.Add(student);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Export()
        {
            var students = await _context.Students.ToListAsync();

            var csv = new System.Text.StringBuilder();
            csv.AppendLine("ID,StudentNumber,Name,Mark");

            foreach (var s in students)
            {
                csv.AppendLine($"{s.ID},{s.StudentNumber},{s.Name},{s.Mark}");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", "students.csv");
        }
    }
}