namespace DersProgrami.Models
{
    public class OgretmenMaasVM
    {
        public int TeacherId { get; set; }
        public string TeacherName { get; set; } = "";
        public string Title { get; set; } = "";
        public decimal Coefficient { get; set; }
        public decimal BaseHourly { get; set; }
        public int TotalHours { get; set; }
        public decimal TotalSalary => Math.Round(BaseHourly * Coefficient * TotalHours, 2);
    }
}
