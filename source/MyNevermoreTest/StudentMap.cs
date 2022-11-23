using Nevermore.Mapping;

namespace MyNevermoreTest;

class StudentMap : DocumentMap<Student>
{
    public StudentMap()
    {
        Column(m => m.Email).MaxLength(50);
        Column(m => m.Age);
    }
}