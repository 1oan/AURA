// Batched validator tests for all CRUD commands (Campus, Dormitory, Room, Faculty, AllocationPeriod, User).
// Each validator is a small rule set; one test class per validator keeps failures easy to localize
// without exploding the file count. All use FluentValidation.TestHelper's TestValidate extensions.

using Aura.Application.AllocationPeriods.Commands.CreateAllocationPeriod;
using Aura.Application.AllocationPeriods.Commands.UpdateAllocationPeriod;
using Aura.Application.Auth.Commands.Login;
using Aura.Application.Campuses.Commands.CreateCampus;
using Aura.Application.Campuses.Commands.UpdateCampus;
using Aura.Application.Dormitories.Commands.CreateDormitory;
using Aura.Application.Dormitories.Commands.UpdateDormitory;
using Aura.Application.DormPreferences.Commands.SubmitPreferences;
using Aura.Application.Faculties.Commands.CreateFaculty;
using Aura.Application.Faculties.Commands.UpdateFaculty;
using Aura.Application.FacultyRoomAllocations.Commands.AssignRooms;
using Aura.Application.FacultyRoomAllocations.Commands.RemoveRoomAssignments;
using Aura.Application.Rooms.Commands.BulkCreateRooms;
using Aura.Application.Rooms.Commands.CreateRoom;
using Aura.Application.Rooms.Commands.UpdateRoom;
using Aura.Application.StudentRecords.Commands.Participate;
using Aura.Application.StudentRecords.Commands.UploadCsv;
using Aura.Application.Users.Commands.ChangePassword;
using Aura.Application.Users.Commands.PromoteUser;
using Aura.Application.Users.Commands.SetMatriculationCode;
using Aura.Application.Users.Commands.UpdateProfile;
using Aura.Domain.Enums;
using FluentValidation.TestHelper;

namespace Aura.Tests.Application;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _v = new();

    [Fact]
    public void Valid_PassesAllRules()
    {
        var r = _v.TestValidate(new LoginCommand("user@uaic.ro", "pass"));
        r.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyEmail_Fails() =>
        _v.TestValidate(new LoginCommand("", "pass"))
            .ShouldHaveValidationErrorFor(x => x.Email);

    [Fact]
    public void MalformedEmail_Fails() =>
        _v.TestValidate(new LoginCommand("not-an-email", "pass"))
            .ShouldHaveValidationErrorFor(x => x.Email);

    [Fact]
    public void EmptyPassword_Fails() =>
        _v.TestValidate(new LoginCommand("user@uaic.ro", ""))
            .ShouldHaveValidationErrorFor(x => x.Password);
}

public class CreateCampusCommandValidatorTests
{
    private readonly CreateCampusCommandValidator _v = new();

    [Fact]
    public void Valid_Passes() =>
        _v.TestValidate(new CreateCampusCommand("Codrescu", "Str. Codrescu")).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void NullAddress_Passes() =>
        _v.TestValidate(new CreateCampusCommand("Codrescu", null)).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void EmptyName_Fails() =>
        _v.TestValidate(new CreateCampusCommand("", null))
            .ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void NameTooLong_Fails() =>
        _v.TestValidate(new CreateCampusCommand(new string('a', 201), null))
            .ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void AddressTooLong_Fails() =>
        _v.TestValidate(new CreateCampusCommand("Codrescu", new string('a', 501)))
            .ShouldHaveValidationErrorFor(x => x.Address);
}

public class UpdateCampusCommandValidatorTests
{
    private readonly UpdateCampusCommandValidator _v = new();

    [Fact]
    public void Valid_Passes() =>
        _v.TestValidate(new UpdateCampusCommand(Guid.NewGuid(), "Codrescu", null)).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void EmptyId_Fails() =>
        _v.TestValidate(new UpdateCampusCommand(Guid.Empty, "Codrescu", null))
            .ShouldHaveValidationErrorFor(x => x.Id);

    [Fact]
    public void EmptyName_Fails() =>
        _v.TestValidate(new UpdateCampusCommand(Guid.NewGuid(), "", null))
            .ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void AddressTooLong_Fails() =>
        _v.TestValidate(new UpdateCampusCommand(Guid.NewGuid(), "Codrescu", new string('a', 501)))
            .ShouldHaveValidationErrorFor(x => x.Address);
}

public class CreateDormitoryCommandValidatorTests
{
    private readonly CreateDormitoryCommandValidator _v = new();

    [Fact]
    public void Valid_Passes() =>
        _v.TestValidate(new CreateDormitoryCommand("C1", Guid.NewGuid())).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void EmptyName_Fails() =>
        _v.TestValidate(new CreateDormitoryCommand("", Guid.NewGuid()))
            .ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void NameTooLong_Fails() =>
        _v.TestValidate(new CreateDormitoryCommand(new string('a', 201), Guid.NewGuid()))
            .ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void EmptyCampusId_Fails() =>
        _v.TestValidate(new CreateDormitoryCommand("C1", Guid.Empty))
            .ShouldHaveValidationErrorFor(x => x.CampusId);
}

public class UpdateDormitoryCommandValidatorTests
{
    private readonly UpdateDormitoryCommandValidator _v = new();

    [Fact]
    public void Valid_Passes() =>
        _v.TestValidate(new UpdateDormitoryCommand(Guid.NewGuid(), "C1")).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void EmptyId_Fails() =>
        _v.TestValidate(new UpdateDormitoryCommand(Guid.Empty, "C1"))
            .ShouldHaveValidationErrorFor(x => x.Id);

    [Fact]
    public void EmptyName_Fails() =>
        _v.TestValidate(new UpdateDormitoryCommand(Guid.NewGuid(), ""))
            .ShouldHaveValidationErrorFor(x => x.Name);
}

public class CreateFacultyCommandValidatorTests
{
    private readonly CreateFacultyCommandValidator _v = new();

    [Fact]
    public void Valid_Passes() =>
        _v.TestValidate(new CreateFacultyCommand("Informatica", "INF")).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void EmptyName_Fails() =>
        _v.TestValidate(new CreateFacultyCommand("", "INF"))
            .ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void NameTooLong_Fails() =>
        _v.TestValidate(new CreateFacultyCommand(new string('a', 201), "INF"))
            .ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void EmptyAbbreviation_Fails() =>
        _v.TestValidate(new CreateFacultyCommand("Informatica", ""))
            .ShouldHaveValidationErrorFor(x => x.Abbreviation);

    [Fact]
    public void AbbreviationTooLong_Fails() =>
        _v.TestValidate(new CreateFacultyCommand("Informatica", new string('A', 21)))
            .ShouldHaveValidationErrorFor(x => x.Abbreviation);
}

public class UpdateFacultyCommandValidatorTests
{
    private readonly UpdateFacultyCommandValidator _v = new();

    [Fact]
    public void Valid_Passes() =>
        _v.TestValidate(new UpdateFacultyCommand(Guid.NewGuid(), "Informatica", "INF"))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void EmptyId_Fails() =>
        _v.TestValidate(new UpdateFacultyCommand(Guid.Empty, "Informatica", "INF"))
            .ShouldHaveValidationErrorFor(x => x.Id);

    [Fact]
    public void EmptyName_Fails() =>
        _v.TestValidate(new UpdateFacultyCommand(Guid.NewGuid(), "", "INF"))
            .ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void EmptyAbbreviation_Fails() =>
        _v.TestValidate(new UpdateFacultyCommand(Guid.NewGuid(), "Informatica", ""))
            .ShouldHaveValidationErrorFor(x => x.Abbreviation);
}

public class CreateAllocationPeriodCommandValidatorTests
{
    private readonly CreateAllocationPeriodCommandValidator _v = new();
    private static readonly DateTime Start = new(2026, 9, 1);
    private static readonly DateTime End = new(2027, 7, 1);

    private static readonly DateTime Round1 = new(2026, 9, 15);

    [Fact]
    public void Valid_Passes() =>
        _v.TestValidate(new CreateAllocationPeriodCommand("2026-2027", Start, End, Round1, 3))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void EmptyName_Fails() =>
        _v.TestValidate(new CreateAllocationPeriodCommand("", Start, End, Round1, 3))
            .ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void NameTooLong_Fails() =>
        _v.TestValidate(new CreateAllocationPeriodCommand(new string('a', 201), Start, End, Round1, 3))
            .ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void EndDateBeforeStart_Fails() =>
        _v.TestValidate(new CreateAllocationPeriodCommand("2026-2027", End, Start, Round1, 3))
            .ShouldHaveValidationErrorFor(x => x.EndDate);
}

public class UpdateAllocationPeriodCommandValidatorTests
{
    private readonly UpdateAllocationPeriodCommandValidator _v = new();
    private static readonly DateTime Start = new(2026, 9, 1);
    private static readonly DateTime End = new(2027, 7, 1);

    [Fact]
    public void Valid_Passes() =>
        _v.TestValidate(new UpdateAllocationPeriodCommand(Guid.NewGuid(), "2026-2027", Start, End))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void EmptyId_Fails() =>
        _v.TestValidate(new UpdateAllocationPeriodCommand(Guid.Empty, "2026-2027", Start, End))
            .ShouldHaveValidationErrorFor(x => x.Id);

    [Fact]
    public void EmptyName_Fails() =>
        _v.TestValidate(new UpdateAllocationPeriodCommand(Guid.NewGuid(), "", Start, End))
            .ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void EndDateBeforeStart_Fails() =>
        _v.TestValidate(new UpdateAllocationPeriodCommand(Guid.NewGuid(), "2026-2027", End, Start))
            .ShouldHaveValidationErrorFor(x => x.EndDate);
}

public class CreateRoomCommandValidatorTests
{
    private readonly CreateRoomCommandValidator _v = new();

    [Fact]
    public void Valid_Passes() =>
        _v.TestValidate(new CreateRoomCommand("101", Guid.NewGuid(), 1, 3, "Male"))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void EmptyNumber_Fails() =>
        _v.TestValidate(new CreateRoomCommand("", Guid.NewGuid(), 1, 3, "Male"))
            .ShouldHaveValidationErrorFor(x => x.Number);

    [Fact]
    public void NumberTooLong_Fails() =>
        _v.TestValidate(new CreateRoomCommand(new string('1', 21), Guid.NewGuid(), 1, 3, "Male"))
            .ShouldHaveValidationErrorFor(x => x.Number);

    [Fact]
    public void EmptyDormitoryId_Fails() =>
        _v.TestValidate(new CreateRoomCommand("101", Guid.Empty, 1, 3, "Male"))
            .ShouldHaveValidationErrorFor(x => x.DormitoryId);

    [Fact]
    public void NegativeFloor_Fails() =>
        _v.TestValidate(new CreateRoomCommand("101", Guid.NewGuid(), -1, 3, "Male"))
            .ShouldHaveValidationErrorFor(x => x.Floor);

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void CapacityOutOfRange_Fails(int cap) =>
        _v.TestValidate(new CreateRoomCommand("101", Guid.NewGuid(), 1, cap, "Male"))
            .ShouldHaveValidationErrorFor(x => x.Capacity);

    [Fact]
    public void EmptyGender_Fails() =>
        _v.TestValidate(new CreateRoomCommand("101", Guid.NewGuid(), 1, 3, ""))
            .ShouldHaveValidationErrorFor(x => x.Gender);
}

public class UpdateRoomCommandValidatorTests
{
    private readonly UpdateRoomCommandValidator _v = new();

    [Fact]
    public void Valid_Passes() =>
        _v.TestValidate(new UpdateRoomCommand(Guid.NewGuid(), "101", 1, 3, "Male"))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void EmptyId_Fails() =>
        _v.TestValidate(new UpdateRoomCommand(Guid.Empty, "101", 1, 3, "Male"))
            .ShouldHaveValidationErrorFor(x => x.Id);

    [Fact]
    public void EmptyNumber_Fails() =>
        _v.TestValidate(new UpdateRoomCommand(Guid.NewGuid(), "", 1, 3, "Male"))
            .ShouldHaveValidationErrorFor(x => x.Number);

    [Fact]
    public void NegativeFloor_Fails() =>
        _v.TestValidate(new UpdateRoomCommand(Guid.NewGuid(), "101", -1, 3, "Male"))
            .ShouldHaveValidationErrorFor(x => x.Floor);

    [Fact]
    public void CapacityOutOfRange_Fails() =>
        _v.TestValidate(new UpdateRoomCommand(Guid.NewGuid(), "101", 1, 11, "Male"))
            .ShouldHaveValidationErrorFor(x => x.Capacity);
}

public class BulkCreateRoomsCommandValidatorTests
{
    private readonly BulkCreateRoomsCommandValidator _v = new();

    [Fact]
    public void Valid_Passes()
    {
        var cmd = new BulkCreateRoomsCommand(Guid.NewGuid(),
            [new FloorConfiguration(1, 10, 3, "Male")]);
        _v.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyDormitoryId_Fails()
    {
        var cmd = new BulkCreateRoomsCommand(Guid.Empty,
            [new FloorConfiguration(1, 10, 3, "Male")]);
        _v.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.DormitoryId);
    }

    [Fact]
    public void EmptyFloorList_Fails()
    {
        var cmd = new BulkCreateRoomsCommand(Guid.NewGuid(), []);
        _v.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Floors);
    }

    [Fact]
    public void NegativeFloorNumber_Fails()
    {
        var cmd = new BulkCreateRoomsCommand(Guid.NewGuid(),
            [new FloorConfiguration(-1, 10, 3, "Male")]);
        _v.TestValidate(cmd).ShouldHaveValidationErrorFor("Floors[0].FloorNumber");
    }

    [Fact]
    public void RoomCountOutOfRange_Fails()
    {
        var cmd = new BulkCreateRoomsCommand(Guid.NewGuid(),
            [new FloorConfiguration(1, 0, 3, "Male")]);
        _v.TestValidate(cmd).ShouldHaveValidationErrorFor("Floors[0].RoomCount");
    }

    [Fact]
    public void CapacityOutOfRange_Fails()
    {
        var cmd = new BulkCreateRoomsCommand(Guid.NewGuid(),
            [new FloorConfiguration(1, 10, 11, "Male")]);
        _v.TestValidate(cmd).ShouldHaveValidationErrorFor("Floors[0].Capacity");
    }

    [Fact]
    public void EmptyGender_Fails()
    {
        var cmd = new BulkCreateRoomsCommand(Guid.NewGuid(),
            [new FloorConfiguration(1, 10, 3, "")]);
        _v.TestValidate(cmd).ShouldHaveValidationErrorFor("Floors[0].Gender");
    }
}

public class AssignRoomsCommandValidatorTests
{
    private readonly AssignRoomsCommandValidator _v = new();

    [Fact]
    public void Valid_Passes() =>
        _v.TestValidate(new AssignRoomsCommand(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()]))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void EmptyFacultyId_Fails() =>
        _v.TestValidate(new AssignRoomsCommand(Guid.Empty, Guid.NewGuid(), [Guid.NewGuid()]))
            .ShouldHaveValidationErrorFor(x => x.FacultyId);

    [Fact]
    public void EmptyAllocationPeriodId_Fails() =>
        _v.TestValidate(new AssignRoomsCommand(Guid.NewGuid(), Guid.Empty, [Guid.NewGuid()]))
            .ShouldHaveValidationErrorFor(x => x.AllocationPeriodId);

    [Fact]
    public void EmptyRoomIds_Fails() =>
        _v.TestValidate(new AssignRoomsCommand(Guid.NewGuid(), Guid.NewGuid(), []))
            .ShouldHaveValidationErrorFor(x => x.RoomIds);
}

public class RemoveRoomAssignmentsCommandValidatorTests
{
    private readonly RemoveRoomAssignmentsCommandValidator _v = new();

    [Fact]
    public void Valid_Passes() =>
        _v.TestValidate(new RemoveRoomAssignmentsCommand(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()]))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void EmptyFacultyId_Fails() =>
        _v.TestValidate(new RemoveRoomAssignmentsCommand(Guid.Empty, Guid.NewGuid(), [Guid.NewGuid()]))
            .ShouldHaveValidationErrorFor(x => x.FacultyId);

    [Fact]
    public void EmptyRoomIds_Fails() =>
        _v.TestValidate(new RemoveRoomAssignmentsCommand(Guid.NewGuid(), Guid.NewGuid(), []))
            .ShouldHaveValidationErrorFor(x => x.RoomIds);
}

public class UpdateProfileCommandValidatorTests
{
    private readonly UpdateProfileCommandValidator _v = new();

    [Fact]
    public void Valid_Passes() =>
        _v.TestValidate(new UpdateProfileCommand("Ioan", "Virlescu")).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void EmptyFirstName_Fails() =>
        _v.TestValidate(new UpdateProfileCommand("", "Virlescu"))
            .ShouldHaveValidationErrorFor(x => x.FirstName);

    [Fact]
    public void FirstNameTooLong_Fails() =>
        _v.TestValidate(new UpdateProfileCommand(new string('a', 101), "Virlescu"))
            .ShouldHaveValidationErrorFor(x => x.FirstName);

    [Fact]
    public void EmptyLastName_Fails() =>
        _v.TestValidate(new UpdateProfileCommand("Ioan", ""))
            .ShouldHaveValidationErrorFor(x => x.LastName);
}

public class ChangePasswordCommandValidatorTests
{
    private readonly ChangePasswordCommandValidator _v = new();

    [Fact]
    public void Valid_Passes() =>
        _v.TestValidate(new ChangePasswordCommand("old123456", "new123456"))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void EmptyCurrentPassword_Fails() =>
        _v.TestValidate(new ChangePasswordCommand("", "new123456"))
            .ShouldHaveValidationErrorFor(x => x.CurrentPassword);

    [Fact]
    public void EmptyNewPassword_Fails() =>
        _v.TestValidate(new ChangePasswordCommand("old123456", ""))
            .ShouldHaveValidationErrorFor(x => x.NewPassword);

    [Fact]
    public void NewPasswordTooShort_Fails() =>
        _v.TestValidate(new ChangePasswordCommand("old123456", "short"))
            .ShouldHaveValidationErrorFor(x => x.NewPassword);
}

public class PromoteUserCommandValidatorTests
{
    private readonly PromoteUserCommandValidator _v = new();

    [Fact]
    public void Valid_Passes() =>
        _v.TestValidate(new PromoteUserCommand(Guid.NewGuid(), UserRole.FacultyAdmin))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void EmptyUserId_Fails() =>
        _v.TestValidate(new PromoteUserCommand(Guid.Empty, UserRole.FacultyAdmin))
            .ShouldHaveValidationErrorFor(x => x.UserId);

    [Fact]
    public void InvalidRole_Fails() =>
        _v.TestValidate(new PromoteUserCommand(Guid.NewGuid(), (UserRole)999))
            .ShouldHaveValidationErrorFor(x => x.Role);
}

public class SetMatriculationCodeCommandValidatorTests
{
    private readonly SetMatriculationCodeCommandValidator _v = new();

    [Fact]
    public void Valid_Passes() =>
        _v.TestValidate(new SetMatriculationCodeCommand("CS2024001"))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Empty_Fails() =>
        _v.TestValidate(new SetMatriculationCodeCommand(""))
            .ShouldHaveValidationErrorFor(x => x.MatriculationCode);

    [Fact]
    public void TooLong_Fails() =>
        _v.TestValidate(new SetMatriculationCodeCommand(new string('A', 51)))
            .ShouldHaveValidationErrorFor(x => x.MatriculationCode);
}

public class SubmitPreferencesCommandValidatorTests
{
    private readonly SubmitPreferencesCommandValidator _v = new();

    [Fact]
    public void Valid_Passes() =>
        _v.TestValidate(new SubmitPreferencesCommand(Guid.NewGuid(), [Guid.NewGuid()]))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void EmptyPeriodId_Fails() =>
        _v.TestValidate(new SubmitPreferencesCommand(Guid.Empty, [Guid.NewGuid()]))
            .ShouldHaveValidationErrorFor(x => x.AllocationPeriodId);

    [Fact]
    public void EmptyDormitoryList_Fails() =>
        _v.TestValidate(new SubmitPreferencesCommand(Guid.NewGuid(), []))
            .ShouldHaveValidationErrorFor(x => x.DormitoryIds);

    [Fact]
    public void DuplicateDormitoryIds_Fails()
    {
        var id = Guid.NewGuid();
        _v.TestValidate(new SubmitPreferencesCommand(Guid.NewGuid(), [id, id]))
            .ShouldHaveValidationErrorFor(x => x.DormitoryIds);
    }
}

public class ParticipateCommandValidatorTests
{
    private readonly ParticipateCommandValidator _v = new();

    [Fact]
    public void Valid_PassesWithCode() =>
        _v.TestValidate(new ParticipateCommand(Guid.NewGuid(), "CS2024001"))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Valid_PassesWithNullCode() =>
        _v.TestValidate(new ParticipateCommand(Guid.NewGuid(), null))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void EmptyPeriodId_Fails() =>
        _v.TestValidate(new ParticipateCommand(Guid.Empty, "CS2024001"))
            .ShouldHaveValidationErrorFor(x => x.AllocationPeriodId);

    [Fact]
    public void CodeTooLong_Fails() =>
        _v.TestValidate(new ParticipateCommand(Guid.NewGuid(), new string('A', 51)))
            .ShouldHaveValidationErrorFor(x => x.MatriculationCode);
}

public class UploadCsvCommandValidatorTests
{
    private readonly UploadCsvCommandValidator _v = new();

    [Fact]
    public void Valid_Passes() =>
        _v.TestValidate(new UploadCsvCommand(Guid.NewGuid(), new MemoryStream()))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void EmptyPeriodId_Fails() =>
        _v.TestValidate(new UploadCsvCommand(Guid.Empty, new MemoryStream()))
            .ShouldHaveValidationErrorFor(x => x.AllocationPeriodId);

    [Fact]
    public void NullStream_Fails() =>
        _v.TestValidate(new UploadCsvCommand(Guid.NewGuid(), null!))
            .ShouldHaveValidationErrorFor(x => x.CsvStream);
}
