-- Generated once on 2026-11-23 23:39 UTC from the lounge.mkcentral.com leaderboards (the generator script was not kept).
-- Local dev seed: top 50 players per game from lounge.mkcentral.com.
-- DO NOT run against production.
--
-- Safe to run more than once: new players are inserted, existing ones get their country and MMR updated.
-- Seeded players have MKCId >= 900000 (fake) and RegistryId NULL.
--
-- Game ids (GameMode):                0 = mk8dx, 2 = mkworld12p, 3 = mkworld24p
-- Registration ids (RegistrationGameMode): 0 = mk8dx, 1 = mkworld (covers 12p and 24p)
-- Local seasons used: mk8dx 15, mkworld12p 2, mkworld24p 2

USE Lounge;

DROP TABLE IF EXISTS #seed;

CREATE TABLE #seed (
    Name           nvarchar(200) NOT NULL,
    NormalizedName nvarchar(200) NOT NULL,
    CountryCode    nvarchar(10)  NULL,
    MKCId          int           NOT NULL,
    Mmr8dx int NULL, MaxMmr8dx int NULL,
    Mmr12p int NULL, MaxMmr12p int NULL,
    Mmr24p int NULL, MaxMmr24p int NULL
);

INSERT INTO #seed (Name, NormalizedName, CountryCode, MKCId, Mmr8dx, MaxMmr8dx, Mmr12p, MaxMmr12p, Mmr24p, MaxMmr24p) VALUES
    (N'KIMI GA HERO', N'KIMIGAHERO', N'FR', 914998, 17071, 17394, NULL, NULL, NULL, NULL),
    (N'Meruem....', N'MERUEM....', N'JP', 949914, 16155, 16721, NULL, NULL, NULL, NULL),
    (N'Henri Mayu', N'HENRIMAYU', N'JP', 917116, 15945, 16346, NULL, NULL, NULL, NULL),
    (N'KOJI TADOKORO', N'KOJITADOKORO', N'JP', 947144, 15064, 15735, NULL, NULL, NULL, NULL),
    (N'Egoistic Flowers', N'EGOISTICFLOWERS', N'JP', 930979, 15061, 15267, NULL, NULL, NULL, NULL),
    (N'NAGASUGIRU INU', N'NAGASUGIRUINU', N'JP', 934621, 14748, 15546, NULL, NULL, NULL, NULL),
    (N'D1 Roflcopter', N'D1ROFLCOPTER', N'HK', 944295, 14618, 14803, NULL, NULL, NULL, NULL),
    (N'Ouroboros', N'OUROBOROS', N'US', 948988, 14549, 15261, NULL, NULL, NULL, NULL),
    (N'Postscript', N'POSTSCRIPT', N'JP', 914743, 14475, 14475, NULL, NULL, NULL, NULL),
    (N'TADAKIMIKIHARE', N'TADAKIMIKIHARE', N'JP', 950745, 14417, 17227, NULL, NULL, NULL, NULL),
    (N'Starlow', N'STARLOW', N'FR', 915583, 14373, 14373, 12116, 12849, 12143, 12278),
    (N'front memory', N'FRONTMEMORY', N'JP', 931587, 14259, 14259, NULL, NULL, NULL, NULL),
    (N'Eren Yeager', N'ERENYEAGER', N'DE', 917706, 14094, 15130, NULL, NULL, NULL, NULL),
    (N'atatata', N'ATATATA', N'JP', 917784, 14058, 14520, NULL, NULL, NULL, NULL),
    (N'Necrotoxin', N'NECROTOXIN', N'JP', 909700, 14032, 14186, NULL, NULL, NULL, NULL),
    (N'kuriin', N'KURIIN', N'JP', 962330, 13947, 14817, NULL, NULL, NULL, NULL),
    (N'blacky', N'BLACKY', N'JP', 909410, 13616, 13940, NULL, NULL, NULL, NULL),
    (N'unConScious.514', N'UNCONSCIOUS.514', N'JP', 941391, 13570, 13772, NULL, NULL, NULL, NULL),
    (N'wagimk', N'WAGIMK', N'JP', 926009, 13513, 13513, NULL, NULL, NULL, NULL),
    (N'Chaozu', N'CHAOZU', N'FR', 931805, 13474, 13525, NULL, NULL, NULL, NULL),
    (N'magabou no dream', N'MAGABOUNODREAM', N'JP', 919887, 13386, 13521, NULL, NULL, NULL, NULL),
    (N'Mr peeky', N'MRPEEKY', N'GB', 920708, 13339, 13778, NULL, NULL, NULL, NULL),
    (N'Crystal Waltz', N'CRYSTALWALTZ', N'JP', 929509, 13301, 14761, NULL, NULL, NULL, NULL),
    (N'SUGISAWA', N'SUGISAWA', N'JP', 913421, 13279, 13279, NULL, NULL, NULL, NULL),
    (N'Rejoice', N'REJOICE', N'JP', 921239, 13170, 13170, NULL, NULL, NULL, NULL),
    (N'Kanata', N'KANATA', N'JP', 919979, 13079, 13079, NULL, NULL, 13598, 14217),
    (N'real feeling', N'REALFEELING', N'US', 916630, 13071, 13321, NULL, NULL, NULL, NULL),
    (N'asuchang.', N'ASUCHANG.', N'JP', 908943, 12966, 13092, NULL, NULL, NULL, NULL),
    (N'l o v e  s i c k', N'LOVESICK', N'US', 947085, 12956, 13554, NULL, NULL, NULL, NULL),
    (N'Toad', N'TOAD', N'FR', 916060, 12954, 13075, NULL, NULL, NULL, NULL),
    (N'2005prime Hayley', N'2005PRIMEHAYLEY', N'JP', 923387, 12950, 12950, NULL, NULL, NULL, NULL),
    (N'lovegirlcuteeyes', N'LOVEGIRLCUTEEYES', N'FR', 953999, 12893, 13429, NULL, NULL, NULL, NULL),
    (N'hase', N'HASE', N'JP', 932188, 12849, 14266, NULL, NULL, NULL, NULL),
    (N'SecretTalk', N'SECRETTALK', N'JP', 940869, 12681, 13899, NULL, NULL, NULL, NULL),
    (N'RYAN', N'RYAN', N'US', 925704, 12668, 13222, NULL, NULL, NULL, NULL),
    (N'ULTRANAOMI67m1x', N'ULTRANAOMI67M1X', N'JP', 930717, 12660, 14123, NULL, NULL, NULL, NULL),
    (N'Cynda', N'CYNDA', N'US', 909903, 12577, 12606, NULL, NULL, NULL, NULL),
    (N'Yomo Fantastic', N'YOMOFANTASTIC', N'JP', 916767, 12541, 12839, NULL, NULL, NULL, NULL),
    (N'Demon', N'DEMON', N'GB', 936138, 12521, 13650, NULL, NULL, NULL, NULL),
    (N'Shakira', N'SHAKIRA', N'US', 919019, 12470, 14751, NULL, NULL, NULL, NULL),
    (N'Nagumo Yoichi', N'NAGUMOYOICHI', N'FR', 910717, 12456, 12456, NULL, NULL, NULL, NULL),
    (N'Razor Crest', N'RAZORCREST', N'JP', 953918, 12450, 12970, NULL, NULL, NULL, NULL),
    (N'Kumugi', N'KUMUGI', N'JP', 912998, 12411, 13343, NULL, NULL, NULL, NULL),
    (N'Monica Everett', N'MONICAEVERETT', N'JP', 922490, 12312, 12522, NULL, NULL, NULL, NULL),
    (N'sakumaru', N'SAKUMARU', N'JP', 952689, 12299, 13042, NULL, NULL, NULL, NULL),
    (N'Your Name.', N'YOURNAME.', N'JP', 958965, 12293, 12734, NULL, NULL, NULL, NULL),
    (N'temi', N'TEMI', N'JP', 935431, 12275, 12897, NULL, NULL, NULL, NULL),
    (N'Flamme Martini', N'FLAMMEMARTINI', N'GB', 914617, 12266, 12619, NULL, NULL, NULL, NULL),
    (N'Panda send wow', N'PANDASENDWOW', N'JP', 943240, 12264, 13349, NULL, NULL, NULL, NULL),
    (N'M O I S T O B A', N'MOISTOBA', N'JP', 922693, 12219, 12399, NULL, NULL, NULL, NULL),
    (N'The Orange Dino', N'THEORANGEDINO', N'US', 952431, NULL, NULL, 16913, 16926, NULL, NULL),
    (N'Kevo', N'KEVO', N'MX', 911961, NULL, NULL, 15894, 15894, NULL, NULL),
    (N'Saber', N'SABER', N'US', 919859, NULL, NULL, 15392, 16227, NULL, NULL),
    (N'rq', N'RQ', N'JP', 958009, NULL, NULL, 15309, 15771, 14399, 14911),
    (N'Irene', N'IRENE', N'JP', 950446, NULL, NULL, 14891, 15334, 13757, 13851),
    (N'Bungee Gum', N'BUNGEEGUM', N'FR', 928055, NULL, NULL, 14806, 14886, 13251, 13866),
    (N'Love Letter', N'LOVELETTER', N'AU', 948447, NULL, NULL, 14707, 15285, 13006, 13307),
    (N'Peyz', N'PEYZ', N'FR', 927785, NULL, NULL, 14406, 14406, NULL, NULL),
    (N'Jojo77', N'JOJO77', N'DE', 954791, NULL, NULL, 14310, 14750, 12260, 12857),
    (N'Abyss', N'ABYSS', N'NL', 953698, NULL, NULL, 14149, 14360, NULL, NULL),
    (N'Dianb', N'DIANB', N'JP', 956683, NULL, NULL, 14042, 14042, 15634, 15634),
    (N'L1am', N'L1AM', N'GB', 912324, NULL, NULL, 13909, 14090, 14847, 15055),
    (N'yoshira', N'YOSHIRA', N'US', 916799, NULL, NULL, 13906, 15141, NULL, NULL),
    (N'magikarpkarpkarp', N'MAGIKARPKARPKARP', N'US', 918972, NULL, NULL, 13658, 13893, NULL, NULL),
    (N'Gecko', N'GECKO', N'US', 910776, NULL, NULL, 13612, 13612, NULL, NULL),
    (N'Star Anise', N'STARANISE', N'JP', 974768, NULL, NULL, 13556, 14572, 16050, 16050),
    (N'LOLmdr', N'LOLMDR', N'FR', 932338, NULL, NULL, 13521, 13801, 12208, 12208),
    (N'shurinho', N'SHURINHO', N'FR', 948951, NULL, NULL, 13339, 13494, NULL, NULL),
    (N'thunthunthunthun', N'THUNTHUNTHUNTHUN', N'CA', 947682, NULL, NULL, 13277, 13587, NULL, NULL),
    (N'Zae', N'ZAE', N'US', 932775, NULL, NULL, 13236, 13477, NULL, NULL),
    (N'Magia', N'MAGIA', N'JP', 912194, NULL, NULL, 13087, 13087, 15089, 15438),
    (N'ZaviewPeak', N'ZAVIEWPEAK', N'US', 977966, NULL, NULL, 13084, 13573, NULL, NULL),
    (N'Fisherman', N'FISHERMAN', N'ES', 917825, NULL, NULL, 13017, 13504, NULL, NULL),
    (N'OsDras', N'OSDRAS', N'FR', 918759, NULL, NULL, 13011, 13791, NULL, NULL),
    (N'pepe8610', N'PEPE8610', N'US', 919663, NULL, NULL, 12973, 13386, NULL, NULL),
    (N'Kana Arima', N'KANAARIMA', N'FR', 911233, NULL, NULL, 12917, 12999, NULL, NULL),
    (N'al maghrib', N'ALMAGHRIB', N'FR', 909319, NULL, NULL, 12899, 12899, 12760, 13053),
    (N'All Smiles', N'ALLSMILES', N'CA', 911648, NULL, NULL, 12891, 13609, NULL, NULL),
    (N'Vladilena', N'VLADILENA', N'FR', 910734, NULL, NULL, 12859, 12859, NULL, NULL),
    (N'Loberia', N'LOBERIA', N'JP', 912510, NULL, NULL, 12828, 12931, 14760, 15368),
    (N'fih', N'FIH', N'FR', 921626, NULL, NULL, 12790, 13163, NULL, NULL),
    (N'Intel', N'INTEL', N'US', 957639, NULL, NULL, 12740, 14216, NULL, NULL),
    (N'Mhlia Blanco', N'MHLIABLANCO', N'JP', 912052, NULL, NULL, 12718, 14230, 12615, 14240),
    (N'Dua Lipa', N'DUALIPA', N'JP', 962369, NULL, NULL, 12717, 13646, NULL, NULL),
    (N'u 1st', N'U1ST', N'JP', 914358, NULL, NULL, 12657, 12657, 15358, 15367),
    (N'Edward Elric', N'EDWARDELRIC', N'FR', 967048, NULL, NULL, 12609, 12706, NULL, NULL),
    (N'sabatora', N'SABATORA', N'JP', 923497, NULL, NULL, 12584, 13673, NULL, NULL),
    (N'twis acorn', N'TWISACORN', N'CA', 933660, NULL, NULL, 12584, 13337, NULL, NULL),
    (N'Poochy', N'POOCHY', N'ES', 909641, NULL, NULL, 12580, 13125, NULL, NULL),
    (N'kurumi2', N'KURUMI2', N'JP', 951077, NULL, NULL, 12568, 12568, NULL, NULL),
    (N'Rayzopp', N'RAYZOPP', N'FR', 914747, NULL, NULL, 12564, 12564, NULL, NULL),
    (N'Carbuncle24', N'CARBUNCLE24', N'JP', 971027, NULL, NULL, 12538, 12727, 14522, 14565),
    (N'Gold Ship', N'GOLDSHIP', N'CR', 916383, NULL, NULL, 12533, 12846, NULL, NULL),
    (N'omg swoop hiii', N'OMGSWOOPHIII', N'US', 914713, NULL, NULL, 12526, 12625, NULL, NULL),
    (N'Lost Lace', N'LOSTLACE', N'US', 940774, NULL, NULL, 12294, 13301, NULL, NULL),
    (N'Ninji', N'NINJI', N'ES', 939697, NULL, NULL, 12282, 12685, NULL, NULL),
    (N'Narco', N'NARCO', N'JP', 937379, NULL, NULL, 12136, 13026, 13001, 13705),
    (N'Nico Robin', N'NICOROBIN', N'VE', 931630, NULL, NULL, 12129, 12129, NULL, NULL),
    (N'Alexio', N'ALEXIO', N'GB', 960747, NULL, NULL, 12118, 12464, NULL, NULL),
    (N'Crude play', N'CRUDEPLAY', N'JP', 935586, NULL, NULL, NULL, NULL, 15444, 15784),
    (N'Li4z', N'LI4Z', N'JP', 926176, NULL, NULL, NULL, NULL, 15192, 15627),
    (N'Akagane', N'AKAGANE', N'JP', 908750, NULL, NULL, NULL, NULL, 14958, 14958),
    (N'okashi', N'OKASHI', N'JP', 941771, NULL, NULL, NULL, NULL, 14930, 15606),
    (N'Bakuten', N'BAKUTEN', N'JP', 943914, NULL, NULL, NULL, NULL, 14719, 15133),
    (N'nerune', N'NERUNE', N'JP', 975412, NULL, NULL, NULL, NULL, 14487, 14754),
    (N'Mus', N'MUS', N'JP', 924081, NULL, NULL, NULL, NULL, 13863, 14378),
    (N'Masato Hayashi', N'MASATOHAYASHI', N'JP', 913177, NULL, NULL, NULL, NULL, 13658, 14259),
    (N'Me2', N'ME2', N'JP', 953868, NULL, NULL, NULL, NULL, 13646, 13676),
    (N'kirina', N'KIRINA', N'JP', 950778, NULL, NULL, NULL, NULL, 13589, 14133),
    (N'SKaR', N'SKAR', N'GB', 917577, NULL, NULL, NULL, NULL, 13566, 13938),
    (N'Kusaan', N'KUSAAN', N'JP', 912278, NULL, NULL, NULL, NULL, 13484, 14085),
    (N'Mda', N'MDA', N'JP', 923495, NULL, NULL, NULL, NULL, 13419, 14026),
    (N'yamayamaDX', N'YAMAYAMADX', N'JP', 909304, NULL, NULL, NULL, NULL, 13321, 13321),
    (N'Clock9', N'CLOCK9', N'JP', 977394, NULL, NULL, NULL, NULL, 13266, 13615),
    (N'AK2KI', N'AK2KI', N'JP', 959964, NULL, NULL, NULL, NULL, 13196, 15109),
    (N'K4I', N'K4I', N'JP', 911715, NULL, NULL, NULL, NULL, 13179, 13208),
    (N'4I7', N'4I7', N'JP', 918937, NULL, NULL, NULL, NULL, 13092, 13216),
    (N'distortionFuture', N'DISTORTIONFUTURE', N'JP', 976500, NULL, NULL, NULL, NULL, 12956, 13069),
    (N'Red', N'RED', N'JP', 977014, NULL, NULL, NULL, NULL, 12893, 14561),
    (N'karinto', N'KARINTO', N'JP', 931249, NULL, NULL, NULL, NULL, 12814, 13085),
    (N'krvr', N'KRVR', N'JP', 975544, NULL, NULL, NULL, NULL, 12737, 12946),
    (N'Lucia', N'LUCIA', N'JP', 930129, NULL, NULL, NULL, NULL, 12690, 13164),
    (N'BippaXD', N'BIPPAXD', N'JP', 908533, NULL, NULL, NULL, NULL, 12607, 13905),
    (N'chianb38', N'CHIANB38', N'JP', 984443, NULL, NULL, NULL, NULL, 12483, 13998),
    (N'Golden Lovrich', N'GOLDENLOVRICH', N'JP', 976804, NULL, NULL, NULL, NULL, 12315, 12634),
    (N'Fireworks', N'FIREWORKS', N'JP', 961730, NULL, NULL, NULL, NULL, 12268, 12997),
    (N'raisins', N'RAISINS', N'JP', 925639, NULL, NULL, NULL, NULL, 12239, 12779),
    (N'Nia Liston', N'NIALISTON', N'JP', 924233, NULL, NULL, NULL, NULL, 12206, 12518),
    (N'blue_mkw', N'BLUE_MKW', N'JP', 941586, NULL, NULL, NULL, NULL, 12197, 12887),
    (N'Nacht', N'NACHT', N'JP', 931878, NULL, NULL, NULL, NULL, 12105, 12455),
    (N'ritsu', N'RITSU', N'JP', 940057, NULL, NULL, NULL, NULL, 12080, 12686);

-- Players: insert new ones, refresh country on existing ones
INSERT INTO Players (Name, NormalizedName, MKCId, RegistryId, CountryCode, IsHidden)
SELECT s.Name, s.NormalizedName, s.MKCId, NULL, s.CountryCode, 0
FROM #seed s
WHERE NOT EXISTS (SELECT 1 FROM Players p WHERE p.NormalizedName = s.NormalizedName);

UPDATE p SET p.CountryCode = s.CountryCode
FROM Players p
JOIN #seed s ON s.NormalizedName = p.NormalizedName;

-- Name history (the API creates one entry per new player)
INSERT INTO NameChanges (PlayerId, Name, NormalizedName, ChangedOn)
SELECT p.Id, p.Name, p.NormalizedName, SYSUTCDATETIME()
FROM #seed s
JOIN Players p ON p.NormalizedName = s.NormalizedName
WHERE NOT EXISTS (SELECT 1 FROM NameChanges n WHERE n.PlayerId = p.Id);

-- Game registrations
INSERT INTO PlayerGameRegistrations (PlayerId, Game, RegisteredOn)
SELECT p.Id, r.Game, SYSUTCDATETIME()
FROM #seed s
JOIN Players p ON p.NormalizedName = s.NormalizedName
CROSS APPLY (VALUES
    (0, CASE WHEN s.Mmr8dx IS NOT NULL THEN 1 ELSE 0 END),
    (1, CASE WHEN s.Mmr12p IS NOT NULL OR s.Mmr24p IS NOT NULL THEN 1 ELSE 0 END)
) r(Game, Plays)
WHERE r.Plays = 1
  AND NOT EXISTS (SELECT 1 FROM PlayerGameRegistrations g WHERE g.PlayerId = p.Id AND g.Game = r.Game);

-- Season MMR: update existing rows, insert missing ones
DROP TABLE IF EXISTS #mmr;

SELECT p.Id AS PlayerId, m.Game, m.Season, m.Mmr, m.MaxMmr
INTO #mmr
FROM #seed s
JOIN Players p ON p.NormalizedName = s.NormalizedName
CROSS APPLY (VALUES
    (0, 15,  s.Mmr8dx, s.MaxMmr8dx),
    (2, 2, s.Mmr12p, s.MaxMmr12p),
    (3, 2, s.Mmr24p, s.MaxMmr24p)
) m(Game, Season, Mmr, MaxMmr)
WHERE m.Mmr IS NOT NULL;

UPDATE d SET d.Mmr = m.Mmr, d.MaxMmr = m.MaxMmr
FROM PlayerSeasonData d
JOIN #mmr m ON m.PlayerId = d.PlayerId AND m.Game = d.Game AND m.Season = d.Season;

INSERT INTO PlayerSeasonData (PlayerId, Game, Season, Mmr, MaxMmr)
SELECT m.PlayerId, m.Game, m.Season, m.Mmr, m.MaxMmr
FROM #mmr m
WHERE NOT EXISTS (SELECT 1 FROM PlayerSeasonData d WHERE d.PlayerId = m.PlayerId AND d.Game = m.Game AND d.Season = m.Season);

-- Placements (the API records a starting MMR as a placement; only added once)
INSERT INTO Placements (PlayerId, Game, Season, Mmr, PrevMmr, AwardedOn)
SELECT m.PlayerId, m.Game, m.Season, m.Mmr, NULL, SYSUTCDATETIME()
FROM #mmr m
WHERE NOT EXISTS (SELECT 1 FROM Placements pl WHERE pl.PlayerId = m.PlayerId AND pl.Game = m.Game AND pl.Season = m.Season);

DROP TABLE #mmr;
DROP TABLE #seed;

-- Quick check
SELECT d.Game, d.Season, COUNT(*) AS Players, MAX(d.Mmr) AS TopMmr
FROM Players p
JOIN PlayerSeasonData d ON d.PlayerId = p.Id
WHERE p.MKCId >= 900000
GROUP BY d.Game, d.Season
ORDER BY d.Game;
