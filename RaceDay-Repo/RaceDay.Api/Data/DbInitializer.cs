using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Data;

public static class DbInitializer
{
    public const string SeedPassword = "Password123!";

    public static void Initialize(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordService>();

        if (db.Database.IsRelational() && db.Database.GetMigrations().Any())
        {
            db.Database.Migrate();
        }
        else
        {
            db.Database.EnsureCreated();
        }

        if (db.Users.Any())
        {
            return;
        }

        // Users
        var thabo = new User { FullName = "Thabo Mokoena", Email = "thabo.mokoena@raceday.co.za", PasswordHash = hasher.Hash(SeedPassword), Role = "Organiser", ContactNumber = "0821234567" };
        var lindiwe = new User { FullName = "Lindiwe Nkosi", Email = "lindiwe.nkosi@raceday.co.za", PasswordHash = hasher.Hash(SeedPassword), Role = "Organiser", ContactNumber = "0837654321" };
        var sipho = new User { FullName = "Sipho Dlamini", Email = "sipho.dlamini@gmail.com", PasswordHash = hasher.Hash(SeedPassword), Role = "Participant", ContactNumber = "0731112222" };
        var amahle = new User { FullName = "Amahle Zulu", Email = "amahle.zulu@gmail.com", PasswordHash = hasher.Hash(SeedPassword), Role = "Participant", ContactNumber = "0743334444" };

        // Events: 3
        var marathon = new Event { Organiser = thabo, EventName = "Johannesburg City Marathon", EventDate = new DateTime(2026, 9, 12), Location = "Sandton, Johannesburg", Description = "Annual city marathon through the Johannesburg CBD and Sandton.", DistanceKM = 42.2m, EventType = "Run", Status = "Open" };
        var funRun = new Event { Organiser = thabo, EventName = "Soweto Fun Run", EventDate = new DateTime(2026, 10, 3), Location = "Soweto, Johannesburg", Description = "Community fun run supporting local youth sports programmes.", DistanceKM = 10m, EventType = "Run", Status = "Open" };
        var trail = new Event { Organiser = lindiwe, EventName = "Pretoria Trail Challenge", EventDate = new DateTime(2026, 11, 15), Location = "Groenkloof Nature Reserve, Pretoria", Description = "Off-road trail race across two difficulty categories.", DistanceKM = 18m, EventType = "Run", Status = "Planned" };

        // Categories
        var c1 = new Category { Event = marathon, CategoryName = "42km Full Marathon", DistanceKM = 42.2m, MaxParticipants = 500, EntryFee = 350m };
        var c2 = new Category { Event = marathon, CategoryName = "21km Half Marathon", DistanceKM = 21.1m, MaxParticipants = 800, EntryFee = 250m };
        var c3 = new Category { Event = funRun, CategoryName = "5km Fun Run", DistanceKM = 5m, MaxParticipants = 1000, EntryFee = 100m };
        var c4 = new Category { Event = funRun, CategoryName = "10km Fun Run", DistanceKM = 10m, MaxParticipants = 600, EntryFee = 150m };
        var c5 = new Category { Event = trail, CategoryName = "Beginner Trail (8km)", DistanceKM = 8m, MaxParticipants = 200, EntryFee = 180m };
        var c6 = new Category { Event = trail, CategoryName = "Advanced Trail (18km)", DistanceKM = 18m, MaxParticipants = 150, EntryFee = 280m };

        // Enrolments
        var en1 = new Enrolment { Participant = sipho, Category = c2, BibNumber = "B1001", Status = "Confirmed" };
        var en2 = new Enrolment { Participant = sipho, Category = c4, BibNumber = "B1002", Status = "Confirmed" };
        var en3 = new Enrolment { Participant = amahle, Category = c1, BibNumber = "B1003", Status = "Confirmed" };
        var en4 = new Enrolment { Participant = amahle, Category = c5, BibNumber = "B1004", Status = "Pending" };

        // Results
        var r1 = new Result { Enrolment = en1, FinishTime = new TimeSpan(1, 52, 34), Position = 12, Status = "Finished" };
        var r2 = new Result { Enrolment = en2, FinishTime = new TimeSpan(0, 48, 10), Position = 5, Status = "Finished" };
        var r3 = new Result { Enrolment = en3, Status = "Not Started" };
        var r4 = new Result { Enrolment = en4, Status = "Not Started" };

        // Payments
        var p1 = new Payment { Enrolment = en1, Amount = 250m, PaymentMethod = "Card", PaymentStatus = "Success" };
        var p2 = new Payment { Enrolment = en2, Amount = 150m, PaymentMethod = "EFT", PaymentStatus = "Success" };
        var p3 = new Payment { Enrolment = en3, Amount = 350m, PaymentMethod = "Card", PaymentStatus = "Success" };
        var p4 = new Payment { Enrolment = en4, Amount = 180m, PaymentMethod = "Card", PaymentStatus = "Pending" };

        db.Users.AddRange(thabo, lindiwe, sipho, amahle);
        db.Events.AddRange(marathon, funRun, trail);
        db.Categories.AddRange(c1, c2, c3, c4, c5, c6);
        db.Enrolments.AddRange(en1, en2, en3, en4);
        db.Results.AddRange(r1, r2, r3, r4);
        db.Payments.AddRange(p1, p2, p3, p4);
        db.SaveChanges();
    }
}
