namespace StudentManagement.Models
{
    public class Student
    {
        public int ID { get; set; }
        public string StudentNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Mark { get; set; }
    }
}