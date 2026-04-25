-- ============================================================
-- Student Registration System — database-first schema
-- Aligns with EF Core models (DepartmentId, not DeptId).
-- Idempotent: safe to re-run (skips existing objects / conflicts).
-- ============================================================

-- Create DB once (connect to postgres):
-- CREATE DATABASE studentregistrationdb;

-- ============================================================
-- TABLES
-- ============================================================

CREATE TABLE IF NOT EXISTS "Schools" (
    "SchoolId"      SERIAL PRIMARY KEY,
    "SchoolName"    VARCHAR(100) NOT NULL,
    "Description"   VARCHAR(500) NOT NULL DEFAULT '',
    "IsActive"      BOOLEAN      NOT NULL DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS "Departments" (
    "DepartmentId"  SERIAL PRIMARY KEY,
    "DeptName"      VARCHAR(100) NOT NULL,
    "Description"   VARCHAR(500) NOT NULL DEFAULT '',
    "IsActive"      BOOLEAN      NOT NULL DEFAULT TRUE,
    "SchoolId"      INTEGER      NOT NULL,

    CONSTRAINT "FK_Departments_Schools" FOREIGN KEY ("SchoolId")
        REFERENCES "Schools"("SchoolId") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "Students" (
    "StudentId"       SERIAL PRIMARY KEY,
    "FirstName"       VARCHAR(50)  NOT NULL,
    "LastName"        VARCHAR(50)  NOT NULL,
    "Email"           VARCHAR(100) NOT NULL,
    "Phone"           VARCHAR(15)  NOT NULL DEFAULT '',
    "DateOfBirth"     TIMESTAMP    NOT NULL,
    "EnrollmentDate"  TIMESTAMP    NOT NULL DEFAULT NOW(),
    "IsActive"        BOOLEAN      NOT NULL DEFAULT TRUE,
    "DepartmentId"    INTEGER      NOT NULL,

    CONSTRAINT "FK_Students_Departments" FOREIGN KEY ("DepartmentId")
        REFERENCES "Departments"("DepartmentId") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "Courses" (
    "CourseId"      SERIAL PRIMARY KEY,
    "CourseCode"    VARCHAR(10)  NOT NULL,
    "CourseName"    VARCHAR(100) NOT NULL,
    "Description"   VARCHAR(500) NOT NULL DEFAULT '',
    "CreditHours"   INTEGER      NOT NULL DEFAULT 3,
    "MaxCapacity"   INTEGER      NOT NULL DEFAULT 40,
    "IsActive"      BOOLEAN      NOT NULL DEFAULT TRUE,
    "DepartmentId"  INTEGER      NOT NULL,

    CONSTRAINT "FK_Courses_Departments" FOREIGN KEY ("DepartmentId")
        REFERENCES "Departments"("DepartmentId") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "Enrollments" (
    "EnrollmentId"  SERIAL PRIMARY KEY,
    "StudentId"     INTEGER   NOT NULL,
    "CourseId"      INTEGER   NOT NULL,
    "EnrolledDate"  TIMESTAMP NOT NULL DEFAULT NOW(),
    "Status"        INTEGER   NOT NULL DEFAULT 0,

    CONSTRAINT "FK_Enrollments_Students" FOREIGN KEY ("StudentId")
        REFERENCES "Students"("StudentId") ON DELETE CASCADE,

    CONSTRAINT "FK_Enrollments_Courses" FOREIGN KEY ("CourseId")
        REFERENCES "Courses"("CourseId") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "Results" (
    "ResultId"      SERIAL PRIMARY KEY,
    "StudentId"     INTEGER      NOT NULL,
    "CourseId"      INTEGER      NOT NULL,
    "Score"         DOUBLE PRECISION NOT NULL DEFAULT 0,
    "Grade"         VARCHAR(2)   NOT NULL DEFAULT '',
    "GradePoint"    DOUBLE PRECISION NOT NULL DEFAULT 0,
    "Semester"      VARCHAR(20)  NOT NULL DEFAULT '',
    "AcademicYear"  VARCHAR(10)  NOT NULL DEFAULT '',
    "DateRecorded"  TIMESTAMP    NOT NULL DEFAULT NOW(),

    CONSTRAINT "FK_Results_Students" FOREIGN KEY ("StudentId")
        REFERENCES "Students"("StudentId") ON DELETE CASCADE,

    CONSTRAINT "FK_Results_Courses" FOREIGN KEY ("CourseId")
        REFERENCES "Courses"("CourseId") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "AuditLogs" (
    "AuditLogId"   BIGSERIAL PRIMARY KEY,
    "OccurredAt"   TIMESTAMP    NOT NULL DEFAULT NOW(),
    "Actor"        VARCHAR(100) NOT NULL DEFAULT '',
    "Action"       VARCHAR(50)  NOT NULL,
    "EntityType"   VARCHAR(100) NOT NULL,
    "EntityId"     VARCHAR(50)  NOT NULL DEFAULT '',
    "Details"      VARCHAR(2000) NOT NULL DEFAULT ''
);

-- ============================================================
-- UNIQUE INDEXES
-- ============================================================

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Schools_SchoolName"
    ON "Schools" ("SchoolName");

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Departments_DeptName_SchoolId"
    ON "Departments" ("DeptName", "SchoolId");

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Students_Email"
    ON "Students" ("Email");

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Courses_CourseCode"
    ON "Courses" ("CourseCode");

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Enrollments_StudentId_CourseId"
    ON "Enrollments" ("StudentId", "CourseId");

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Results_StudentId_CourseId_Semester_AcademicYear"
    ON "Results" ("StudentId", "CourseId", "Semester", "AcademicYear");

CREATE INDEX IF NOT EXISTS "IX_AuditLogs_OccurredAt"
    ON "AuditLogs" ("OccurredAt" DESC);

-- ============================================================
-- SEED DATA
-- ============================================================

INSERT INTO "Schools" ("SchoolName", "Description")
VALUES
    ('School of Computing', 'Computer Science, Software Engineering, and Information Technology'),
    ('School of Sciences', 'Mathematics, Physics, Chemistry, and Biology'),
    ('School of Arts & Humanities',    'Languages, Literature, Philosophy, and Social Sciences')
ON CONFLICT ("SchoolName") DO NOTHING;

INSERT INTO "Departments" ("DeptName", "Description", "SchoolId")
VALUES
    ('Computer Science',  'Programming, algorithms, and software development',        1),
    ('Information Technology',  'Networks, systems administration, and cybersecurity',    1),
    ('Mathematics',             'Pure and applied mathematics',                             2),
    ('Physics',                 'Mechanics, thermodynamics, and modern physics',          2),
    ('English',                 'English language and literature',                          3)
ON CONFLICT ("DeptName", "SchoolId") DO NOTHING;

INSERT INTO "Courses" ("CourseCode", "CourseName", "Description", "CreditHours", "MaxCapacity", "DepartmentId")
VALUES
    ('CS101',  'Introduction to Computer Science', 'Fundamentals of computing and programming',          3, 40, 1),
    ('CS201',  'Data Structures & Algorithms',     'Arrays, linked lists, trees, sorting and searching', 4, 35, 1),
    ('MTH101', 'Calculus I',                       'Limits, derivatives, and integrals',                 3, 50, 3),
    ('ENG101', 'English Composition',              'Academic writing and critical thinking',              3, 30, 5),
    ('PHY101', 'Physics I',                        'Mechanics, thermodynamics, and waves',               4, 40, 4),
    ('CS301',  'Database Systems',                 'Relational databases, SQL, and normalization',        3, 35, 1),
    ('CS401',  'Software Engineering',             'Software development lifecycle and methodologies',    3, 30, 1),
    ('MTH201', 'Linear Algebra',                   'Vectors, matrices, and linear transformations',       3, 40, 3)
ON CONFLICT ("CourseCode") DO NOTHING;

INSERT INTO "Students"
("FirstName", "LastName", "Email", "Phone", "DateOfBirth", "DepartmentId")
VALUES
    ('John',   'Doe',     'john.doe@example.com',     '08030000001', '2000-05-15', 1),
    ('Jane',   'Smith',   'jane.smith@example.com',   '08030000002', '1999-08-22', 1),
    ('Michael','Brown',   'michael.brown@example.com','08030000003', '2001-01-10', 2),
    ('Emily',  'Davis',   'emily.davis@example.com',  '08030000004', '2000-11-30', 3),
    ('Daniel', 'Wilson',  'daniel.wilson@example.com','08030000005', '1998-07-19', 4),
    ('Sophia', 'Taylor',  'sophia.taylor@example.com','08030000006', '2001-03-25', 5)
ON CONFLICT ("Email") DO NOTHING;

INSERT INTO "Enrollments"
("StudentId", "CourseId", "Status")
VALUES
    (1, 1, 1),
    (1, 2, 1),
    (1, 6, 1),

    (2, 1, 1),
    (2, 2, 1),

    (3, 1, 1),

    (4, 3, 1),
    (4, 8, 1),

    (5, 5, 1),

    (6, 4, 1)
ON CONFLICT ("StudentId", "CourseId") DO NOTHING;

INSERT INTO "Results"
("StudentId", "CourseId", "Score", "Grade", "GradePoint", "Semester", "AcademicYear")
VALUES
    (1, 1, 85, 'A', 4.0, 'First', '2024/2025'),
    (1, 2, 78, 'B', 3.0, 'First', '2024/2025'),
    (1, 6, 88, 'A', 4.0, 'Second', '2024/2025'),

    (2, 1, 65, 'C', 2.0, 'First', '2024/2025'),
    (2, 2, 72, 'B', 3.0, 'First', '2024/2025'),

    (4, 3, 90, 'A', 4.0, 'First', '2024/2025'),
    (4, 8, 83, 'A', 4.0, 'Second', '2024/2025'),

    (5, 5, 70, 'B', 3.0, 'First', '2024/2025'),

    (6, 4, 75, 'B', 3.0, 'First', '2024/2025')
ON CONFLICT ("StudentId", "CourseId", "Semester", "AcademicYear") DO NOTHING;