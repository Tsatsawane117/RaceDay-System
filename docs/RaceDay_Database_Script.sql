
IF OBJECT_ID('dbo.Payments', 'U') IS NOT NULL DROP TABLE dbo.Payments;
IF OBJECT_ID('dbo.Results', 'U') IS NOT NULL DROP TABLE dbo.Results;
IF OBJECT_ID('dbo.Enrolments', 'U') IS NOT NULL DROP TABLE dbo.Enrolments;
IF OBJECT_ID('dbo.Categories', 'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID('dbo.Events', 'U') IS NOT NULL DROP TABLE dbo.Events;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;

CREATE TABLE dbo.Users (
    UserID          INT IDENTITY(1,1) PRIMARY KEY,
    FullName        NVARCHAR(100)   NOT NULL,
    Email           NVARCHAR(150)   NOT NULL UNIQUE,
    PasswordHash    NVARCHAR(255)   NOT NULL,
    Role            VARCHAR(20)     NOT NULL DEFAULT 'Participant'
                        CHECK (Role IN ('Organiser', 'Participant')),
    ContactNumber   VARCHAR(20)     NULL,
    CreatedAt       DATETIME        NOT NULL DEFAULT GETDATE()
);


-- Events
CREATE TABLE dbo.Events (
    EventID         INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserID     INT             NOT NULL,
    EventName       NVARCHAR(150)   NOT NULL,
    EventDate       DATE            NOT NULL,
    Location        NVARCHAR(150)   NOT NULL,
    Description     NVARCHAR(500)   NULL,
    Status          VARCHAR(20)     NOT NULL DEFAULT 'Planned'
                        CHECK (Status IN ('Planned', 'Open', 'Closed', 'Completed', 'Cancelled')),
    CONSTRAINT FK_Events_Organiser FOREIGN KEY (OrganiserID)
        REFERENCES dbo.Users(UserID)
);


-- Categories
CREATE TABLE dbo.Categories (
    CategoryID      INT IDENTITY(1,1) PRIMARY KEY,
    EventID         INT             NOT NULL,
    CategoryName    NVARCHAR(100)   NOT NULL,
    DistanceKM      DECIMAL(5,2)    NOT NULL,
    MaxParticipants INT             NOT NULL DEFAULT 100,
    EntryFee        DECIMAL(8,2)    NOT NULL DEFAULT 0.00,
    CONSTRAINT FK_Categories_Events FOREIGN KEY (EventID)
        REFERENCES dbo.Events(EventID)
);


-- Enrolments
CREATE TABLE dbo.Enrolments (
    EnrolmentID     INT IDENTITY(1,1) PRIMARY KEY,
    ParticipantID   INT             NOT NULL,
    CategoryID      INT             NOT NULL,
    EnrolmentDate   DATETIME        NOT NULL DEFAULT GETDATE(),
    BibNumber       VARCHAR(10)     NULL,
    Status          VARCHAR(20)     NOT NULL DEFAULT 'Pending'
                        CHECK (Status IN ('Pending', 'Confirmed', 'Cancelled')),
    CONSTRAINT FK_Enrolments_Users FOREIGN KEY (ParticipantID)
        REFERENCES dbo.Users(UserID),
    CONSTRAINT FK_Enrolments_Categories FOREIGN KEY (CategoryID)
        REFERENCES dbo.Categories(CategoryID),
    CONSTRAINT UQ_Enrolment UNIQUE (ParticipantID, CategoryID)
);


-- Results
CREATE TABLE dbo.Results (
    ResultID        INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentID     INT             NOT NULL UNIQUE,
    FinishTime      TIME            NULL,
    Position        INT             NULL,
    Status          VARCHAR(20)     NOT NULL DEFAULT 'Not Started'
                        CHECK (Status IN ('Not Started', 'Finished', 'DNF', 'DSQ')),
    CONSTRAINT FK_Results_Enrolments FOREIGN KEY (EnrolmentID)
        REFERENCES dbo.Enrolments(EnrolmentID)
);


-- Payments
CREATE TABLE dbo.Payments (
    PaymentID       INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentID     INT             NOT NULL,
    Amount          DECIMAL(8,2)    NOT NULL,
    PaymentDate     DATETIME        NOT NULL DEFAULT GETDATE(),
    PaymentMethod   VARCHAR(30)     NOT NULL DEFAULT 'Card'
                        CHECK (PaymentMethod IN ('Card', 'EFT', 'Cash')),
    PaymentStatus   VARCHAR(20)     NOT NULL DEFAULT 'Pending'
                        CHECK (PaymentStatus IN ('Pending', 'Success', 'Failed', 'Refunded')),
    CONSTRAINT FK_Payments_Enrolments FOREIGN KEY (EnrolmentID)
        REFERENCES dbo.Enrolments(EnrolmentID)
);



-- Users
INSERT INTO dbo.Users (FullName, Email, PasswordHash, Role, ContactNumber) VALUES
('Thabo Mokoena', 'thabo.mokoena@raceday.co.za', 'hashed_pw_1', 'Organiser', '0821234567'),
('Lindiwe Nkosi', 'lindiwe.nkosi@raceday.co.za', 'hashed_pw_2', 'Organiser', '0837654321'),
('Sipho Dlamini', 'sipho.dlamini@gmail.com', 'hashed_pw_3', 'Participant', '0731112222'),
('Amahle Zulu', 'amahle.zulu@gmail.com', 'hashed_pw_4', 'Participant', '0743334444');


-- Events
INSERT INTO dbo.Events (OrganiserID, EventName, EventDate, Location, Description, Status) VALUES
(1, 'Johannesburg City Marathon', '2026-09-12', 'Sandton, Johannesburg', 'Annual city marathon through the Johannesburg CBD and Sandton.', 'Open'),
(1, 'Soweto Fun Run', '2026-10-03', 'Soweto, Johannesburg', 'Community fun run supporting local youth sports programmes.', 'Open'),
(2, 'Pretoria Trail Challenge', '2026-11-15', 'Groenkloof Nature Reserve, Pretoria', 'Off-road trail race across three difficulty categories.', 'Planned');


-- Categories
INSERT INTO dbo.Categories (EventID, CategoryName, DistanceKM, MaxParticipants, EntryFee) VALUES
(1, '42km Full Marathon', 42.20, 500, 350.00),
(1, '21km Half Marathon', 21.10, 800, 250.00),
(2, '5km Fun Run', 5.00, 1000, 100.00),
(2, '10km Fun Run', 10.00, 600, 150.00),
(3, 'Beginner Trail (8km)', 8.00, 200, 180.00),
(3, 'Advanced Trail (18km)', 18.00, 150, 280.00);


-- Enrolments
INSERT INTO dbo.Enrolments (ParticipantID, CategoryID, BibNumber, Status) VALUES
(3, 2, 'B1001', 'Confirmed'),
(3, 4, 'B1002', 'Confirmed'),
(4, 1, 'B1003', 'Confirmed'),
(4, 5, 'B1004', 'Pending');


-- Results
INSERT INTO dbo.Results (EnrolmentID, FinishTime, Position, Status) VALUES
(1, '01:52:34', 12, 'Finished'),
(2, '00:48:10', 5, 'Finished'),
(3, NULL, NULL, 'Not Started'),
(4, NULL, NULL, 'Not Started');


-- Payments
INSERT INTO dbo.Payments (EnrolmentID, Amount, PaymentMethod, PaymentStatus) VALUES
(1, 250.00, 'Card', 'Success'),
(2, 150.00, 'EFT', 'Success'),
(3, 350.00, 'Card', 'Success'),
(4, 180.00, 'Card', 'Pending');



