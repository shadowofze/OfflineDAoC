-- Shannon Estuary beach-rat camp. Run inside the caller's SQLite transaction.
-- The one existing rat is level 1; ten fixed-ID level-1/2 rats are cloned
-- nearby so rerunning the patch cannot duplicate the camp.
UPDATE Mob
SET Level = 1
WHERE Mob_ID = '11caef62-3c17-4e0a-8399-fccb46fa3fa6'
  AND Region = 200 AND Name = 'beach rat' AND Level <> 1;

WITH additions(Mob_ID, X, Y, Z, Heading, Level) AS (
    VALUES
    ('6a8bc1ef-a38a-5e21-8fae-15582d7a7e2f', 306640, 627370, 6585, 3660, 2),
    ('724a518e-63ff-570b-a515-40fa571c0ab3', 306700, 626650, 6271,  340, 2),
    ('92adfe9a-8589-53f3-99b8-9d1b4d8ce2d5', 306920, 627700, 6745, 3220, 1),
    ('8150bea8-72e3-5e53-9134-9277bf006477', 307050, 626640, 6344,  781, 1),
    ('07fa91d3-48c3-5e0e-a319-c5d32a6df550', 307180, 627250, 6656, 4050, 1),
    ('584b2efb-e21a-54ec-810e-340d9071058d', 307300, 627760, 6807, 2860, 2),
    ('cc14e26e-676f-54ba-bb99-9cb84a7c9d26', 307420, 626720, 6457, 1280, 2),
    ('656a30e1-a1eb-548a-bf71-bdcc8398d05a', 307680, 627580, 6817, 2460, 1),
    ('a9385037-18cf-5d3a-a6b2-597b17dbf5cd', 307720, 626900, 6605, 1710, 1),
    ('0315e7a7-fc4b-5346-bb07-6a114e2ecf1f', 307890, 627250, 6753, 2070, 2)
)
INSERT OR IGNORE INTO Mob (
    ClassType, TranslationId, Name, Suffix, Guild, ExamineArticle,
    MessageArticle, X, Y, Z, Speed, Heading, Region, Model, Size,
    Strength, Constitution, Dexterity, Quickness, Intelligence, Piety,
    Empathy, Charisma, Level, Realm, EquipmentTemplateID,
    ItemsListTemplateID, NPCTemplateID, Race, Flags, AggroLevel,
    AggroRange, MeleeDamageType, RespawnInterval, FactionID, BodyType,
    HouseNumber, Brain, PathID, OwnerID, RoamingRange, IsCloakHoodUp,
    Gender, PackageID, VisibleWeaponSlots, LastTimeRowUpdated, Mob_ID
)
SELECT
    seed.ClassType, seed.TranslationId, seed.Name, seed.Suffix, seed.Guild,
    seed.ExamineArticle, seed.MessageArticle, extra.X, extra.Y, extra.Z,
    seed.Speed, extra.Heading, seed.Region, seed.Model, seed.Size,
    seed.Strength, seed.Constitution, seed.Dexterity, seed.Quickness,
    seed.Intelligence, seed.Piety, seed.Empathy, seed.Charisma,
    extra.Level, seed.Realm, seed.EquipmentTemplateID,
    seed.ItemsListTemplateID, seed.NPCTemplateID, seed.Race, seed.Flags,
    seed.AggroLevel, seed.AggroRange, seed.MeleeDamageType,
    seed.RespawnInterval, seed.FactionID, seed.BodyType, seed.HouseNumber,
    seed.Brain, seed.PathID, seed.OwnerID, seed.RoamingRange,
    seed.IsCloakHoodUp, seed.Gender, seed.PackageID,
    seed.VisibleWeaponSlots, seed.LastTimeRowUpdated, extra.Mob_ID
FROM Mob AS seed
CROSS JOIN additions AS extra
WHERE seed.Mob_ID = '11caef62-3c17-4e0a-8399-fccb46fa3fa6'
  AND seed.Region = 200 AND seed.Name = 'beach rat';
