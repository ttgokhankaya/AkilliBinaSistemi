-- Minimal fixture for CI integration tests (Adle.Api.Test), mirroring the shape
-- of the desktop app's SeedData. Loaded after db/schema.sql into a fresh DB.
-- IDs come out as 1..6 because the tables are empty, so AreaType 1 = LivingRoom
-- and 2 = Bedroom (asserted by the area CRUD test) and area 1 = Ev is a valid
-- parent. The serial sequences advance past these, so API inserts get IDs > 6.

INSERT INTO public."AreaTypes" ("Name") VALUES
  ('LivingRoom'), ('Bedroom'), ('Kitchen'), ('Bathroom'), ('Hall'), ('HouseArea');

-- Ev first so it exists (id 1) before the child rows reference it as parent.
INSERT INTO public."Areas" ("Name", "Width", "Height", "AreaID", "AreaTypeID")
  VALUES ('Ev', 120, 80, NULL, 6);

INSERT INTO public."Areas" ("Name", "Width", "Height", "AreaID", "AreaTypeID") VALUES
  ('Oturma Odası', 30, 20, 1, 1),
  ('Yatak Odası 1', 20, 15, 1, 2),
  ('Yatak Odası 2', 15, 12, 1, 2),
  ('Mutfak', 18, 14, 1, 3),
  ('Banyo', 10, 8, 1, 4);
