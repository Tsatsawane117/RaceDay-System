

IF COL_LENGTH('dbo.Events', 'DistanceKM') IS NULL
    ALTER TABLE dbo.Events ADD DistanceKM DECIMAL(5,2) NOT NULL
        CONSTRAINT DF_Events_DistanceKM DEFAULT 0;
GO

IF COL_LENGTH('dbo.Events', 'EventType') IS NULL
    ALTER TABLE dbo.Events ADD EventType VARCHAR(10) NOT NULL
        CONSTRAINT DF_Events_EventType DEFAULT 'Run'
        CONSTRAINT CK_Events_EventType CHECK (EventType IN ('Run', 'Walk', 'Cycle'));
GO

-- Back-fill sample distances for the three seeded events
UPDATE dbo.Events SET DistanceKM = 42.2 WHERE EventName = 'Johannesburg City Marathon';
UPDATE dbo.Events SET DistanceKM = 10   WHERE EventName = 'Soweto Fun Run';
UPDATE dbo.Events SET DistanceKM = 18   WHERE EventName = 'Pretoria Trail Challenge';
GO
