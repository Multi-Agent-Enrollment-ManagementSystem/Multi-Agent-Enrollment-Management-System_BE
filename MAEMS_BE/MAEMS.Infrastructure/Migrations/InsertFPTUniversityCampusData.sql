-- Migration: Insert FPT University Campus Data
-- Date: 2024
-- Description: Updates FPT University campus information with email and phone number

BEGIN;

-- Update Ha Noi Campus
UPDATE campus SET email = 'tuyensinhhanoi@fpt.edu.vn', phone_number = '(024)73005588'
WHERE name LIKE '%Ha Noi%' OR name LIKE '%Hanoi%' OR campus_id = 1;

-- Update Ho Chi Minh City Campus
UPDATE campus SET email = 'tuyensinhhcm@fpt.edu.vn', phone_number = '(028)73005588'
WHERE name LIKE '%Ho Chi Minh%' OR name LIKE '%HCMC%' OR campus_id = 2;

-- Update Da Nang Campus
UPDATE campus SET email = 'tuyensinhdanang@fpt.edu.vn', phone_number = '(0236)7300999'
WHERE name LIKE '%Da Nang%' OR campus_id = 3;

-- Update Can Tho Campus
UPDATE campus SET email = 'tuyensinhcantho@fpt.edu.vn', phone_number = '(0292)7303636'
WHERE name LIKE '%Can Tho%' OR campus_id = 4;

-- Update Quy Nhon - Gia Lai Campus
UPDATE campus SET email = 'tuyensinhquynhon@fpt.edu.vn', phone_number = '(0256)7300999'
WHERE name LIKE '%Quy Nhon%' OR name LIKE '%Gia Lai%' OR campus_id = 5;

COMMIT;
