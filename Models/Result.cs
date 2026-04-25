using System;
using System.Collections.Generic;

namespace ConsoleStudentRegistration.Models;

public partial class Result
{
    public int ResultId { get; set; }

    public int StudentId { get; set; }

    public int CourseId { get; set; }

    public double Score { get; set; }

    public string Grade { get; set; } = null!;

    public double GradePoint { get; set; }

    public string Semester { get; set; } = null!;

    public string AcademicYear { get; set; } = null!;

    public DateTime DateRecorded { get; set; }

    public virtual Course Course { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;
}
