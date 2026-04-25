using System;
using System.Collections.Generic;

namespace ConsoleStudentRegistration.Models;

public class Department
{
    public int DepartmentId { get; set; }

    public string DeptName { get; set; } = null!;

    public string Description { get; set; } = null!;

    public bool IsActive { get; set; }

    public int SchoolId { get; set; }

    public virtual School School { get; set; } = null!;

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();

    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
}
