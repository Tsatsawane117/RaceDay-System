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
GO
 
-- Events: created and owned by an Organiser
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
GO
 
-- Categories: each Event can have multiple race categories (e.g. 5km, 10km)
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
GO
 
-- Enrolments: links a Participant (User) to a Category
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
GO
 
-- Results: one result per Enrolment
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
GO
 
-- Payments: an Enrolment can have multiple payment attempts/records
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