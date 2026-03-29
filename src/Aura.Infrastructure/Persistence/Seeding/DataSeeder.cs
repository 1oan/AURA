namespace Aura.Infrastructure.Persistence.Seeding;

using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Microsoft.EntityFrameworkCore;

public class DataSeeder(AuraDbContext context)
{
    public async Task SeedAsync()
    {
        await SeedSuperAdminAsync();

        if (await context.Campuses.AnyAsync())
            return;

        var (campuses, dormitories) = BuildCampusesAndDormitories();
        context.Campuses.AddRange(campuses);
        context.Dormitories.AddRange(dormitories);

        var allRooms = new List<Room>();
        foreach (var dorm in dormitories)
        {
            // Premium dorms get smaller capacity; standard get 3
            bool isPremium = dorm.Name is "Gaudeamus" or "Akademos";
            int capacity = isPremium ? 2 : 3;
            var rooms = GenerateRooms(dorm.Id, floorsCount: 5, roomsPerFloor: 10, capacity, maleFemaleFloorSplit: 3);
            allRooms.AddRange(rooms);
        }
        context.Rooms.AddRange(allRooms);

        var faculties = BuildFaculties();
        context.Faculties.AddRange(faculties);

        var period = AllocationPeriod.Create(
            "2026-2027",
            new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 7, 1, 0, 0, 0, DateTimeKind.Utc));
        context.AllocationPeriods.Add(period);

        var allocations = DistributeRoomsRoundRobin(faculties, allRooms, period.Id);
        context.FacultyRoomAllocations.AddRange(allocations);

        await context.SaveChangesAsync();
    }

    private async Task SeedSuperAdminAsync()
    {
        if (await context.Users.AnyAsync(u => u.Role == UserRole.SuperAdmin))
            return;

        var admin = User.Create("admin@uaic.ro", "Admin", "admin", BCrypt.Net.BCrypt.HashPassword("Admin123!"));
        admin.SetRole(UserRole.SuperAdmin);
        context.Users.Add(admin);
        await context.SaveChangesAsync();
    }

    private static (List<Campus> campuses, List<Dormitory> dormitories) BuildCampusesAndDormitories()
    {
        var campusData = new (string Name, string Address, string[] DormNames)[]
        {
            ("Târgușor-Copou",   "Strada Târgușor, Iași",        ["C1", "C2", "C3"]),
            ("Codrescu",         "Strada Codrescu, Iași",         ["C10", "C11", "C12", "C13"]),
            ("Titu Maiorescu",   "Strada Titu Maiorescu, Iași",   ["C6", "C7", "C8"]),
            ("Gaudeamus",        "Bulevardul Carol I, Iași",      ["Gaudeamus"]),
            ("Akademos",         "Strada Universității, Iași",    ["Akademos"]),
            ("C5",               "Strada Păcurari, Iași",         ["C5"]),
            ("C4",               "Strada Copou, Iași",            ["C4"]),
            ("Buna Vestire",     "Strada Buna Vestire, Iași",     ["Buna Vestire"]),
        };

        var campuses = new List<Campus>();
        var dormitories = new List<Dormitory>();

        foreach (var (name, address, dormNames) in campusData)
        {
            var campus = Campus.Create(name, address);
            campuses.Add(campus);

            foreach (var dormName in dormNames)
                dormitories.Add(Dormitory.Create(dormName, campus.Id));
        }

        return (campuses, dormitories);
    }

    private static List<Room> GenerateRooms(
        Guid dormitoryId,
        int floorsCount,
        int roomsPerFloor,
        int capacity,
        int maleFemaleFloorSplit)
    {
        var rooms = new List<Room>();
        for (int floor = 0; floor < floorsCount; floor++)
        {
            var gender = floor < maleFemaleFloorSplit ? Gender.Male : Gender.Female;
            for (int seq = 1; seq <= roomsPerFloor; seq++)
            {
                var number = floor == 0 ? seq.ToString() : (floor * 100 + seq).ToString();
                rooms.Add(Room.Create(number, dormitoryId, floor, capacity, gender));
            }
        }
        return rooms;
    }

    private static List<Faculty> BuildFaculties() =>
    [
        Faculty.Create("Informatica", "INF"),
        Faculty.Create("Drept", "DRE"),
        Faculty.Create("Litere", "LIT"),
        Faculty.Create("Economie", "ECO"),
        Faculty.Create("Biologie", "BIO"),
    ];

    // Round-robin distribution ensures each faculty gets rooms spread across all dorms
    private static List<FacultyRoomAllocation> DistributeRoomsRoundRobin(
        List<Faculty> faculties,
        List<Room> rooms,
        Guid periodId)
    {
        var allocations = new List<FacultyRoomAllocation>(rooms.Count);
        for (int i = 0; i < rooms.Count; i++)
        {
            var faculty = faculties[i % faculties.Count];
            allocations.Add(FacultyRoomAllocation.Create(faculty.Id, rooms[i].Id, periodId));
        }
        return allocations;
    }
}
