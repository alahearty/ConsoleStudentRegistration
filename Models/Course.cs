using System;
using System.Collections.Generic;

namespace ConsoleStudentRegistration.Models;

public partial class Course
{
    public int CourseId { get; set; }

    public string CourseCode { get; set; } = null!;

    public string CourseName { get; set; } = null!;

    public string Description { get; set; } = null!;

    public int CreditHours { get; set; }

    public int MaxCapacity { get; set; }

    public bool IsActive { get; set; }

    public int DepartmentId { get; set; }

    public virtual Department Department { get; set; } = null!;

    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    public virtual ICollection<Result> Results { get; set; } = new List<Result>();
}
