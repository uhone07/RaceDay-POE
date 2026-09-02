/* =========================================================
   RaceDay Database Script
   Part 1 - Section C
   Target: SQL Server (run in SSMS)
   Matches: RaceDay_ERD.png
   ========================================================= */

IF DB_ID('RaceDayDB') IS NOT NULL
BEGIN
    ALTER DATABASE RaceDayDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE RaceDayDB;
END
GO

CREATE DATABASE RaceDayDB;
GO

USE RaceDayDB;
GO

/* =========================================================
   TABLE: Users
   ========================================================= */
CREATE TABLE Users (
    UserId          INT IDENTITY(1,1) PRIMARY KEY,
    Email           VARCHAR(150)    NOT NULL UNIQUE,
    PasswordHash    VARCHAR(255)    NOT NULL,
    Role            VARCHAR(20)     NOT NULL DEFAULT 'Participant',
    CreatedAt       DATETIME        NOT NULL DEFAULT GETDATE(),
    CONSTRAINT CK_Users_Role CHECK (Role IN ('Organiser', 'Participant'))
);
GO

/* =========================================================
   TABLE: UserProfiles
   ========================================================= */
CREATE TABLE UserProfiles (
    ProfileId           INT IDENTITY(1,1) PRIMARY KEY,
    UserId              INT             NOT NULL UNIQUE,
    FullName            VARCHAR(150)    NOT NULL,
    PhoneNumber         VARCHAR(20)     NULL,
    ProfilePictureUrl   VARCHAR(500)    NULL,
    CONSTRAINT FK_UserProfiles_Users FOREIGN KEY (UserId)
        REFERENCES Users(UserId) ON DELETE CASCADE
);
GO

/* =========================================================
   TABLE: Events
   ========================================================= */
CREATE TABLE Events (
    EventId         INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserId     INT             NOT NULL,
    Name            VARCHAR(150)    NOT NULL,
    Description     VARCHAR(1000)   NULL,
    EventDate       DATETIME        NOT NULL,
    Location        VARCHAR(200)    NOT NULL,
    DistanceKm      DECIMAL(6,2)    NOT NULL,
    EventType       VARCHAR(10)     NOT NULL,
    BannerImageUrl  VARCHAR(500)    NULL,
    CONSTRAINT FK_Events_Organiser FOREIGN KEY (OrganiserId)
        REFERENCES Users(UserId),
    CONSTRAINT CK_Events_Type CHECK (EventType IN ('Run', 'Walk', 'Cycle'))
);
GO

/* =========================================================
   TABLE: Categories
   ========================================================= */
CREATE TABLE Categories (
    CategoryId      INT IDENTITY(1,1) PRIMARY KEY,
    EventId         INT             NOT NULL,
    Name            VARCHAR(100)    NOT NULL,
    MinAge          INT             NULL,
    MaxAge          INT             NULL,
    DistanceKm      DECIMAL(6,2)    NULL,
    CONSTRAINT FK_Categories_Events FOREIGN KEY (EventId)
        REFERENCES Events(EventId) ON DELETE CASCADE
);
GO

/* =========================================================
   TABLE: Enrolments
   ========================================================= */
CREATE TABLE Enrolments (
    EnrolmentId     INT IDENTITY(1,1) PRIMARY KEY,
    ParticipantId   INT             NOT NULL,
    EventId         INT             NOT NULL,
    CategoryId      INT             NOT NULL,
    EnrolmentDate   DATETIME        NOT NULL DEFAULT GETDATE(),
    Status          VARCHAR(20)     NOT NULL DEFAULT 'Pending',
    CONSTRAINT FK_Enrolments_Participant FOREIGN KEY (ParticipantId)
        REFERENCES Users(UserId),
    CONSTRAINT FK_Enrolments_Events FOREIGN KEY (EventId)
        REFERENCES Events(EventId),
    CONSTRAINT FK_Enrolments_Categories FOREIGN KEY (CategoryId)
        REFERENCES Categories(CategoryId),
    CONSTRAINT CK_Enrolments_Status CHECK (Status IN ('Pending', 'Confirmed', 'Cancelled')),
    CONSTRAINT UQ_Enrolments_ParticipantEvent UNIQUE (ParticipantId, EventId)
);
GO

/* =========================================================
   TABLE: Results
   ========================================================= */
CREATE TABLE Results (
    ResultId        INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentId     INT             NOT NULL UNIQUE,
    FinishTime      TIME            NOT NULL,
    FinishPosition  INT             NOT NULL,
    TotalFinishers  INT             NOT NULL,
    CONSTRAINT FK_Results_Enrolments FOREIGN KEY (EnrolmentId)
        REFERENCES Enrolments(EnrolmentId) ON DELETE CASCADE
);
GO

/* =========================================================
   SEED DATA
   ========================================================= */

-- Users: 2 Organisers, 2 Participants
INSERT INTO Users (Email, PasswordHash, Role) VALUES
('organiser1@raceday.co.za', 'HASHED_PASSWORD_1', 'Organiser'),
('organiser2@raceday.co.za', 'HASHED_PASSWORD_2', 'Organiser'),
('participant1@raceday.co.za', 'HASHED_PASSWORD_3', 'Participant'),
('participant2@raceday.co.za', 'HASHED_PASSWORD_4', 'Participant');
GO

INSERT INTO UserProfiles (UserId, FullName, PhoneNumber) VALUES
(1, 'Thabo Nkosi', '0821234567'),
(2, 'Sarah van der Merwe', '0837654321'),
(3, 'Lindiwe Dlamini', '0716549873'),
(4, 'James Botha', '0845551234');
GO

-- Events: 3 events
INSERT INTO Events (OrganiserId, Name, Description, EventDate, Location, DistanceKm, EventType) VALUES
(1, 'Johannesburg City Run', 'Annual road running event through the Johannesburg CBD.', '2026-10-10', 'Johannesburg, Gauteng', 21.10, 'Run'),
(1, 'Soweto Charity Cycle', 'Charity cycling event supporting local schools.', '2026-11-15', 'Soweto, Gauteng', 42.00, 'Cycle'),
(2, 'Durban Beachfront Walk', 'Family-friendly walk along the Durban promenade.', '2026-09-20', 'Durban, KwaZulu-Natal', 5.00, 'Walk');
GO

-- Categories: at least one per event
INSERT INTO Categories (EventId, Name, MinAge, MaxAge, DistanceKm) VALUES
(1, 'Under 20', 15, 19, 21.10),
(1, 'Senior', 20, 59, 21.10),
(1, '10km Fun Run', NULL, NULL, 10.00),
(2, 'Open Category', NULL, NULL, 42.00),
(3, 'Family Walk', NULL, NULL, 5.00);
GO

-- Enrolments: sample enrolments
INSERT INTO Enrolments (ParticipantId, EventId, CategoryId, Status) VALUES
(3, 1, 2, 'Confirmed'),
(4, 1, 3, 'Pending'),
(3, 3, 5, 'Confirmed');
GO

-- Results: sample result for a completed enrolment
INSERT INTO Results (EnrolmentId, FinishTime, FinishPosition, TotalFinishers) VALUES
(1, '01:45:32', 47, 312);
GO
