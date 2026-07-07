-- ADLE PostgreSQL schema (idempotent).
-- Derived from the EF6 migration definitions in DatabaseMigration/ and
-- SimulationDB_Migrations/. Both EF6 contexts share this single database and
-- query the "public" schema (see HasDefaultSchema("public") in the DbContexts).
-- Executed at application startup by GUI_Simulation instead of DbMigrator,
-- because the EF6 migration snapshots were left in an inconsistent state by
-- the SQL Server -> PostgreSQL port and Npgsql EF6 cannot regenerate them here.

-- =========================================================================
-- DatabaseMigration context: Areas / AreaTypes / Items / Memories
-- =========================================================================

CREATE TABLE IF NOT EXISTS public."AreaTypes" (
    "ID"           serial PRIMARY KEY,
    "Name"         text,
    "Definition"   text,
    "CreatedBy"    text,
    "CreatedDate"  timestamp,
    "DeletedBy"    text,
    "DeletedDate"  timestamp,
    "ModifiedBy"   text,
    "ModifiedDate" timestamp
);

CREATE TABLE IF NOT EXISTS public."Areas" (
    "ID"           serial PRIMARY KEY,
    "Name"         text,
    "Width"        double precision NOT NULL,
    "Height"       double precision NOT NULL,
    "AreaID"       integer REFERENCES public."Areas" ("ID"),
    "AreaTypeID"   integer REFERENCES public."AreaTypes" ("ID"),
    "CreatedBy"    text,
    "CreatedDate"  timestamp,
    "DeletedBy"    text,
    "DeletedDate"  timestamp,
    "ModifiedBy"   text,
    "ModifiedDate" timestamp
);

CREATE TABLE IF NOT EXISTS public."Items" (
    "ID"           serial PRIMARY KEY,
    "Name"         text,
    "Availablity"  boolean NOT NULL,
    "IpV4"         text,
    "IpV6"         text,
    "ItemType"     text,
    "AreaOfItemID" integer NOT NULL REFERENCES public."Areas" ("ID") ON DELETE CASCADE,
    "CreatedBy"    text,
    "CreatedDate"  timestamp,
    "DeletedBy"    text,
    "DeletedDate"  timestamp,
    "ModifiedBy"   text,
    "ModifiedDate" timestamp
);

CREATE TABLE IF NOT EXISTS public."Memories" (
    "ID"           serial PRIMARY KEY,
    "AreaID"       integer REFERENCES public."Areas" ("ID"),
    "ItemID"       integer REFERENCES public."Items" ("ID"),
    "Date"         timestamp NOT NULL,
    "CreatedBy"    text,
    "CreatedDate"  timestamp,
    "DeletedBy"    text,
    "DeletedDate"  timestamp,
    "ModifiedBy"   text,
    "ModifiedDate" timestamp,
    "Definition"   text,
    "ActionName"   text,
    "ActionValue"  text
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Areas_Name"     ON public."Areas" ("Name");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_AreaTypes_Name" ON public."AreaTypes" ("Name");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Items_Name"     ON public."Items" ("Name");

-- =========================================================================
-- SimulationDB context: Devices / Actors / Operations / graph mappings
-- =========================================================================

CREATE TABLE IF NOT EXISTS public."Actor" (
    "ID"   serial PRIMARY KEY,
    "Name" text
);

CREATE TABLE IF NOT EXISTS public."Habits" (
    "HabitID" serial PRIMARY KEY,
    "Name"    text
);

CREATE TABLE IF NOT EXISTS public."AreaBases" (
    "ID"   serial PRIMARY KEY,
    "Name" text
);

CREATE TABLE IF NOT EXISTS public."Operations" (
    "ID"        serial PRIMARY KEY,
    "Name"      text,
    "StartTime" timestamp NOT NULL,
    "Duration"  interval NOT NULL
);

CREATE TABLE IF NOT EXISTS public."Scenarios" (
    "ID"   serial PRIMARY KEY,
    "Name" text
);

CREATE TABLE IF NOT EXISTS public."GraphObjects" (
    "ID"          serial PRIMARY KEY,
    "Name"        text,
    "MatrixValue" text
);

CREATE TABLE IF NOT EXISTS public."DeviceBases" (
    "ID"            serial PRIMARY KEY,
    "Name"          text,
    "ip"            text,
    "AreaID"        integer NOT NULL REFERENCES public."AreaBases" ("ID") ON DELETE CASCADE,
    "state"         boolean,
    "Discriminator" varchar(128) NOT NULL
);

CREATE TABLE IF NOT EXISTS public."OperationHabitMappings" (
    "ID"          serial PRIMARY KEY,
    "MaxDuration" integer NOT NULL,
    "MinDuration" integer NOT NULL,
    "OperationID" integer REFERENCES public."Operations" ("ID"),
    "HabitID"     integer REFERENCES public."Habits" ("HabitID")
);

CREATE TABLE IF NOT EXISTS public."OperationDevices" (
    "ID"           serial PRIMARY KEY,
    "Sira"         integer NOT NULL,
    "ActionName"   text,
    "OperationID"  integer NOT NULL REFERENCES public."Operations" ("ID") ON DELETE CASCADE,
    "DeviceBaseID" integer NOT NULL REFERENCES public."DeviceBases" ("ID") ON DELETE CASCADE,
    "AreaID"       integer REFERENCES public."AreaBases" ("ID")
);

CREATE TABLE IF NOT EXISTS public."GraphNodeDeviceMappings" (
    "ID"       serial PRIMARY KEY,
    "NodeName" text,
    "DeviceID" integer NOT NULL REFERENCES public."DeviceBases" ("ID") ON DELETE CASCADE,
    "GraphID"  integer NOT NULL REFERENCES public."GraphObjects" ("ID") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS public."HabitActors" (
    "Habit_HabitID" integer NOT NULL REFERENCES public."Habits" ("HabitID") ON DELETE CASCADE,
    "Actor_ID"      integer NOT NULL REFERENCES public."Actor" ("ID") ON DELETE CASCADE,
    PRIMARY KEY ("Habit_HabitID", "Actor_ID")
);

CREATE TABLE IF NOT EXISTS public."ScenarioActors" (
    "Scenario_ID" integer NOT NULL REFERENCES public."Scenarios" ("ID") ON DELETE CASCADE,
    "Actor_ID"    integer NOT NULL REFERENCES public."Actor" ("ID") ON DELETE CASCADE,
    PRIMARY KEY ("Scenario_ID", "Actor_ID")
);

CREATE TABLE IF NOT EXISTS public."ScenarioAreaBases" (
    "Scenario_ID"  integer NOT NULL REFERENCES public."Scenarios" ("ID") ON DELETE CASCADE,
    "AreaBase_ID"  integer NOT NULL REFERENCES public."AreaBases" ("ID") ON DELETE CASCADE,
    PRIMARY KEY ("Scenario_ID", "AreaBase_ID")
);
