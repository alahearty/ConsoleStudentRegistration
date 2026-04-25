using System;
using System.Collections.Generic;

namespace ConsoleStudentRegistration.Models;

public partial class School
{
    public int SchoolId { get; set; }

    public string SchoolName { get; set; } = null!;

    public string Description { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual ICollection<Department> Departments { get; set; } = new List<Department>();
}
